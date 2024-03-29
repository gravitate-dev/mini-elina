﻿// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MagicaCloth
{
    /// <summary>
    /// 回転調整ワーカー
    /// </summary>
    public class AdjustRotationWorker : PhysicsManagerWorker
    {
        // 回転調整モード
        //const int AdjustMode_RotationLine = 0;
        const int AdjustMode_None = 0;
        const int AdjustMode_XYMove = 1;
        const int AdjustMode_XZMove = 2;
        const int AdjustMode_YZMove = 3;
        //const int AdjustMode_Lock = 4; // 回転はBaseRot固定

        /// <summary>
        /// 拘束データ
        /// このデータは調整モードがRotationLineの場合のみ必要
        /// </summary>
        [System.Serializable]
        public struct AdjustRotationData
        {
            /// <summary>
            /// キー頂点インデックス
            /// </summary>
            public int keyIndex;

            /// <summary>
            /// ターゲット頂点インデックス
            /// ターゲット頂点インデックスがプラスの場合は子をターゲット、マイナスの場合は親をターゲットとする
            /// マイナスの場合は０を表現するためさらに(-1)されているので注意！
            /// </summary>
            public int targetIndex;

            /// <summary>
            /// ターゲットへのローカル位置（単位ベクトル）
            /// </summary>
            public float3 localPos;

            /// <summary>
            /// データが有効か判定する
            /// </summary>
            /// <returns></returns>
            public bool IsValid()
            {
                return keyIndex != 0 || targetIndex != 0;
            }
        }
        FixedChunkNativeArray<AdjustRotationData> dataList;

        /// <summary>
        /// グループごとの拘束データ
        /// </summary>
        public struct GroupData
        {
            public int teamId;
            public int active;

            /// <summary>
            /// 調整方法
            /// </summary>
            public int adjustMode;

            /// <summary>
            /// AdjustModeがXY/XZ/YZMoveのときの各軸ごとの回転力
            /// </summary>
            public float3 axisRotationPower;

            public ChunkData chunk;
        }
        public FixedNativeList<GroupData> groupList;

        /// <summary>
        /// パーティクルごとの拘束データ
        /// </summary>
        ExNativeMultiHashMap<int, int> particleMap;

        //=========================================================================================
        public override void Create()
        {
            dataList = new FixedChunkNativeArray<AdjustRotationData>();
            groupList = new FixedNativeList<GroupData>();
            particleMap = new ExNativeMultiHashMap<int, int>();
        }

        public override void Release()
        {
            dataList.Dispose();
            groupList.Dispose();
            particleMap.Dispose();
        }

        public int AddGroup(int teamId, bool active, int adjustMode, float3 axisRotationPower, AdjustRotationData[] dataArray)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];

            var gdata = new GroupData();
            gdata.teamId = teamId;
            gdata.active = active ? 1 : 0;
            gdata.adjustMode = adjustMode;
            gdata.axisRotationPower = axisRotationPower;
            if (dataArray != null && dataArray.Length > 0)
            {
                // モードがRotationLineのみデータがある
                var c = this.dataList.AddChunk(dataArray.Length);
                gdata.chunk = c;

                // チャンクデータコピー
                dataList.ToJobArray().CopyFromFast(c.startIndex, dataArray);

                // パーティクルごとのデータリンク
                int pstart = teamData.particleChunk.startIndex;
                for (int i = 0; i < dataArray.Length; i++)
                {
                    var data = dataArray[i];
                    int dindex = c.startIndex + i;
                    particleMap.Add(pstart + data.keyIndex, dindex);
                }
            }

            int group = groupList.Add(gdata);
            return group;
        }

        public override void RemoveGroup(int teamId)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.adjustRotationGroupIndex;
            if (group < 0)
                return;

            var cdata = groupList[group];

            // パーティクルごとのデータリンク解除
            if (cdata.chunk.dataLength > 0)
            {
                int dstart = cdata.chunk.startIndex;
                int pstart = teamData.particleChunk.startIndex;
                for (int i = 0; i < cdata.chunk.dataLength; i++)
                {
                    int dindex = dstart + i;
                    var data = dataList[dindex];
                    particleMap.Remove(pstart + data.keyIndex, dindex);
                }

                // チャンクデータ削除
                dataList.RemoveChunk(cdata.chunk);
            }

            // データ削除
            groupList.Remove(group);
        }

        public void ChangeParam(int teamId, bool active, int adjustMode, float3 axisRotationPower)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.adjustRotationGroupIndex;
            if (group < 0)
                return;

            var gdata = groupList[group];
            gdata.active = active ? 1 : 0;
            gdata.adjustMode = adjustMode;
            gdata.axisRotationPower = axisRotationPower;
            groupList[group] = gdata;
        }

        public int ActiveCount
        {
            get
            {
                int cnt = 0;
                for (int i = 0; i < groupList.Length; i++)
                    if (groupList[i].active == 1)
                        cnt++;
                return cnt;
            }
        }

        //=========================================================================================
        /// <summary>
        /// トランスフォームリード中に実行する処理
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public override void Warmup()
        {
        }

        public override void CompleteWarmup()
        {
        }

        //=========================================================================================
        /// <summary>
        /// 物理更新前処理
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public override JobHandle PreUpdate(JobHandle jobHandle)
        {
            return jobHandle;
        }

        //=========================================================================================
        /// <summary>
        /// 物理更新後処理
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public override JobHandle PostUpdate(JobHandle jobHandle)
        {
            if (ActiveCount == 0)
                return jobHandle;

            // 回転調整拘束（パーティクルごとに実行する）
            var job1 = new AdjustRotationJob()
            {
                dataList = dataList.ToJobArray(),
                groupList = groupList.ToJobArray(),
                particleMap = particleMap.Map,

                teamDataList = Manager.Team.teamDataList.ToJobArray(),
                teamIdList = Manager.Particle.teamIdList.ToJobArray(),

                flagList = Manager.Particle.flagList.ToJobArray(),
                basePosList = Manager.Particle.basePosList.ToJobArray(),
                baseRotList = Manager.Particle.baseRotList.ToJobArray(),
                posList = Manager.Particle.posList.ToJobArray(),

                rotList = Manager.Particle.rotList.ToJobArray(),
            };
            jobHandle = job1.Schedule(Manager.Particle.Length, 64, jobHandle);

            return jobHandle;
        }

        /// <summary>
        /// 回転調整ジョブ
        /// パーティクルごとに計算
        /// </summary>
        [BurstCompile]
        struct AdjustRotationJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<AdjustRotationData> dataList;
            [ReadOnly]
            public NativeArray<GroupData> groupList;
            [ReadOnly]
            public NativeMultiHashMap<int, int> particleMap;

            [ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;
            [ReadOnly]
            public NativeArray<int> teamIdList;

            [ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [ReadOnly]
            public NativeArray<float3> basePosList;
            [ReadOnly]
            public NativeArray<quaternion> baseRotList;
            [ReadOnly]
            public NativeArray<float3> posList;

            [WriteOnly]
            public NativeArray<quaternion> rotList;

            /// <summary>
            /// パーティクルごと
            /// </summary>
            /// <param name="index"></param>
            public void Execute(int index)
            {
                var flag = flagList[index];
                if (flag.IsValid() == false)
                    return;

                // チーム
                var team = teamDataList[teamIdList[index]];
                if (team.IsActive() == false || team.adjustRotationGroupIndex < 0)
                    return;
                int start = team.particleChunk.startIndex;

                // グループデータ
                var gdata = groupList[team.adjustRotationGroupIndex];
                if (gdata.active == 0)
                    return;

                // 情報
                quaternion baserot = baseRotList[index]; // 常に本来の回転から算出する
                var nextrot = baserot;

                // 回転調整
                var nextpos = posList[index];

                // モードがRotationLineかそれ以外で処理分岐
                //if (gdata.adjustMode == AdjustMode_RotationLine)
                //{
                // ★LineWorkerへ移動
#if false
                    // 子の回転補間数を数える
                    int rotcnt = 0;
                    int dataIndex;
                    NativeMultiHashMapIterator<int> iterator;
                    if (particleMap.TryGetFirstValue(index, out dataIndex, out iterator))
                    {
                        do
                        {
                            rotcnt++;
                        }
                        while (particleMap.TryGetNextValue(out dataIndex, ref iterator));
                    }
                    if (rotcnt == 0)
                        return;
                    float t = 1.0f / rotcnt;

                    if (particleMap.TryGetFirstValue(index, out dataIndex, out iterator))
                    {
                        do
                        {
                            var data = dataList[dataIndex];
                            if (data.IsValid() == false)
                                continue;

                            float3 tvec = data.localPos;

                            // 回転ラインベース
                            float3 v2 = new float3(0, 0, 1);
                            float3 tv = v2;

                            if (data.targetIndex < 0)
                            {
                                // 親がターゲット
                                int tindex = start + -data.targetIndex - 1; // さらに(-1)する
                                float3 ppos = posList[tindex];

                                // 現在のベクトル
                                v2 = nextpos - ppos;

                                // 本来あるべきベクトル
                                tv = math.mul(baseRotList[tindex], tvec);
                            }
                            else
                            {
                                // 子がターゲット
                                int tindex = start + data.targetIndex;
                                float3 cpos = posList[tindex];

                                // 現在のベクトル
                                v2 = cpos - nextpos;

                                // 本来あるべきベクトル
                                tv = math.mul(baserot, tvec);
                            }

                            // 補正回転
                            // ターゲットが複数ある場合は均等に回転補間を行う
                            quaternion q = MathUtility.FromToRotation(tv, v2);
                            quaternion rot = math.slerp(quaternion.identity, q, t);

                            // 最終回転
                            nextrot = math.mul(rot, nextrot);
                        }
                        while (particleMap.TryGetNextValue(out dataIndex, ref iterator));
                    }
#endif
                //}
                //else if (gdata.adjustMode == AdjustMode_Lock)
                //{
                //    // BaseRot固定
                //}
                //else
                {
                    // 移動ベクトルベース
                    // 移動ローカルベクトル
                    var lpos = nextpos - basePosList[index];
                    lpos = math.mul(math.inverse(baserot), lpos);

                    // 軸ごとの回転力
                    lpos *= gdata.axisRotationPower;

                    // ローカル回転
                    quaternion lq = quaternion.identity;
                    if (gdata.adjustMode == AdjustMode_XYMove)
                    {
                        lq = quaternion.EulerZXY(-lpos.y, lpos.x, 0);
                    }
                    else if (gdata.adjustMode == AdjustMode_XZMove)
                    {
                        lq = quaternion.EulerZXY(lpos.z, 0, -lpos.x);
                    }
                    else if (gdata.adjustMode == AdjustMode_YZMove)
                    {
                        lq = quaternion.EulerZXY(0, lpos.z, -lpos.y);
                    }

                    // 最終回転
                    nextrot = math.mul(nextrot, lq);
                }

                // 書き出し
                nextrot = math.normalize(nextrot); // 正規化しないとエラーになる場合がある
                rotList[index] = nextrot;
            }
        }
    }
}
