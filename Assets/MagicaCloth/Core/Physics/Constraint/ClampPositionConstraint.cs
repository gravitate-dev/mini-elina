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
    /// 移動範囲制限距離拘束
    /// nextposおよびposを原点からの距離制限する
    /// </summary>
    public class ClampPositionConstraint : PhysicsManagerConstraint
    {
        /// <summary>
        /// グループごとの拘束データ
        /// </summary>
        public struct GroupData
        {
            public int teamId;
            public int active;

            public CurveParam limitLength;

            /// <summary>
            /// 軸ごとの移動制限割合(0.0-1.0)
            /// </summary>
            public float3 axisRatio;

            /// <summary>
            /// 速度影響
            /// </summary>
            public float velocityInfluence;

            /// <summary>
            /// データが軸ごとの制限を行うか判定する
            /// </summary>
            /// <returns></returns>
            public bool IsAxisCheck()
            {
                return axisRatio.x < 0.999f || axisRatio.y < 0.999f || axisRatio.z < 0.999f;
            }
        }
        public FixedNativeList<GroupData> groupList;

        //=========================================================================================
        public override void Create()
        {
            groupList = new FixedNativeList<GroupData>();
        }

        public override void Release()
        {
            groupList.Dispose();
        }

        //=========================================================================================
        public int AddGroup(int teamId, bool active, BezierParam limitLength, float3 axisRatio, float velocityInfluence)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];

            var gdata = new GroupData();
            gdata.teamId = teamId;
            gdata.active = active ? 1 : 0;
            gdata.limitLength.Setup(limitLength);
            gdata.axisRatio = axisRatio;
            gdata.velocityInfluence = velocityInfluence;

            int group = groupList.Add(gdata);
            return group;
        }

        public override void RemoveTeam(int teamId)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.clampPositionGroupIndex;
            if (group < 0)
                return;

            // データ削除
            groupList.Remove(group);
        }

        public void ChangeParam(int teamId, bool active, BezierParam limitLength, float3 axisRatio, float velocityInfluence)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.clampPositionGroupIndex;
            if (group < 0)
                return;

            var gdata = groupList[group];
            gdata.active = active ? 1 : 0;
            gdata.limitLength.Setup(limitLength);
            gdata.axisRatio = axisRatio;
            gdata.velocityInfluence = velocityInfluence;
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
        /// 拘束の解決
        /// </summary>
        /// <param name="dtime"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public override JobHandle SolverConstraint(float dtime, float updatePower, int iteration, JobHandle jobHandle)
        {
            if (ActiveCount == 0)
                return jobHandle;

            // 移動範囲制限拘束（パーティクルごとに実行する）
            var job1 = new ClampPositionJob()
            {
                clampPositionGroupList = groupList.ToJobArray(),

                teamDataList = Manager.Team.teamDataList.ToJobArray(),
                teamIdList = Manager.Particle.teamIdList.ToJobArray(),

                flagList = Manager.Particle.flagList.ToJobArray(),
                depthList = Manager.Particle.depthList.ToJobArray(),
                basePosList = Manager.Particle.basePosList.ToJobArray(),
                baseRotList = Manager.Particle.baseRotList.ToJobArray(),

                nextPosList = Manager.Particle.InNextPosList.ToJobArray(),
                posList = Manager.Particle.posList.ToJobArray(),
            };
            jobHandle = job1.Schedule(Manager.Particle.Length, 64, jobHandle);

            return jobHandle;
        }

        /// <summary>
        /// 移動範囲制限拘束ジョブ
        /// パーティクルごとに計算
        /// </summary>
        [BurstCompile]
        struct ClampPositionJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<GroupData> clampPositionGroupList;

            [ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;
            [ReadOnly]
            public NativeArray<int> teamIdList;

            [ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [ReadOnly]
            public NativeArray<float> depthList;
            [ReadOnly]
            public NativeArray<float3> basePosList;
            [ReadOnly]
            public NativeArray<quaternion> baseRotList;

            public NativeArray<float3> nextPosList;
            public NativeArray<float3> posList;

            // パーティクルごと
            public void Execute(int index)
            {
                var flag = flagList[index];
                if (flag.IsValid() == false || flag.IsFixed())
                    return;

                var team = teamDataList[teamIdList[index]];
                if (team.IsActive() == false)
                    return;
                if (team.clampPositionGroupIndex < 0)
                    return;
                // 更新確認
                if (team.IsUpdate() == false)
                    return;

                // グループデータ
                var gdata = clampPositionGroupList[team.clampPositionGroupIndex];
                if (gdata.active == 0)
                    return;

                var nextpos = nextPosList[index];
                var depth = depthList[index];
                var limitLength = gdata.limitLength.Evaluate(depth);

                // baseposからの最大移動距離制限
                var basepos = basePosList[index];
                var v = nextpos - basepos; // nextpos

                if (gdata.IsAxisCheck())
                {
                    // 楕円体判定
                    float3 axisRatio = gdata.axisRatio;
                    // 基準軸のワールド回転
                    quaternion rot = baseRotList[index];
                    // 基準軸のローカルベクトルへ変換
                    quaternion irot = math.inverse(rot);
                    float3 lv = math.mul(irot, v);

                    // Boxクランプ
                    float3 axisRatio1 = axisRatio * limitLength;
                    lv = math.clamp(lv, -axisRatio1, axisRatio1);

                    // 基準軸のワールドベクトルへ変換
                    // 最終的に(v)が楕円体でクランプされた移動制限ベクトルとなる
                    v = math.mul(rot, lv);
                }

                // nextposの制限
                v = MathUtility.ClampVector(v, 0.0f, limitLength);

                // 書き戻し
                var opos = nextpos;
                nextpos = basepos + v;
                nextPosList[index] = nextpos;

                // 速度影響
                var av = (nextpos - opos) * (1.0f - gdata.velocityInfluence);
                posList[index] = posList[index] + av;
            }
        }
    }
}
