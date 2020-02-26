// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// チームデータ
    /// チーム０はグローバルとして扱う
    /// </summary>
    public class PhysicsManagerTeamData : PhysicsManagerAccess
    {
        /// <summary>
        /// チームフラグビット
        /// </summary>
        public const uint Flag_Enable = 0x00000001; // 有効フラグ
        public const uint Flag_Interpolate = 0x00000002; // 補間処理適用
        //public const uint Flag_Update = 0x00000004; // 更新フラグ
        public const uint Flag_Reset_WorldInfluence = 0x00010000; // ワールド影響をリセットする
        public const uint Flag_Reset_Position = 0x00020000; // クロスパーティクル姿勢をリセット
        public const uint Flag_Collision_KeepShape = 0x00040000; // 当たり判定時の初期姿勢をキープ

        /// <summary>
        /// 速度変更モード
        /// </summary>
        public enum ForceMode
        {
            None,

            VelocityAdd,                    // 速度に加算（質量の影響を受ける）
            VelocityChange,                 // 速度を変更（質量の影響を受ける）

            VelocityAddWithoutMass = 10,    // 速度に加算（質量無視）
            VelocityChangeWithoutMass,      // 速度を変更（質量無視）
        }

        /// <summary>
        /// チーム状態
        /// </summary>
        public struct TeamData
        {
            /// <summary>
            /// チームが生成したパーティクル（コライダーパーティクルは除くので注意）
            /// </summary>
            public ChunkData particleChunk;

            /// <summary>
            /// フラグビットデータ
            /// </summary>
            public uint flag;

            /// <summary>
            /// 摩擦係数(0.0-1.0)
            /// </summary>
            public float friction;

            /// <summary>
            /// セルフコリジョンの影響範囲
            /// </summary>
            public float selfCollisionRange;

            /// <summary>
            /// 自身のボーンインデックス
            /// </summary>
            public int boneIndex;

            /// <summary>
            /// チームタイムスケール(0.0-1.0)
            /// </summary>
            public float timeScale;

            /// <summary>
            /// チーム内更新時間
            /// </summary>
            public float nowTime;

            /// <summary>
            /// チーム更新回数
            /// </summary>
            public int updateCount;

            public int runCount;

            /// <summary>
            /// ブレンド率(0.0-1.0)
            /// </summary>
            public float blendRatio;

            /// <summary>
            /// 外力
            /// </summary>
            public ForceMode forceMode;
            public float3 impactForce;

            /// <summary>
            /// 距離拘束データへのインデックス
            /// </summary>
            public int restoreDistanceGroupIndex;
            public int triangleBendGroupIndex;
            public int clampDistanceGroupIndex;
            public int clampPositionGroupIndex;
            public int clampRotationGroupIndex;
            public int restoreRotationGroupIndex;
            public int adjustRotationGroupIndex;
            public int springGroupIndex;
            public int volumeGroupIndex;
            public int airLineGroupIndex;
            public int lineWorkerGroupIndex;
            public int selfCollisionGroupIndex;
            public int edgeCollisionGroupIndex;

            /// <summary>
            /// データが有効か判定する
            /// </summary>
            /// <returns></returns>
            public bool IsActive()
            {
                return (flag & Flag_Enable) != 0;
            }

            /// <summary>
            /// 更新すべきか判定する
            /// </summary>
            /// <returns></returns>
            public bool IsUpdate()
            {
                return runCount < updateCount;
            }

            public bool IsRunning()
            {
                return updateCount > 0;
            }

            /// <summary>
            /// 補間を行うか判定する
            /// </summary>
            /// <returns></returns>
            public bool IsInterpolate()
            {
                //return timeScale < 0.99f;
                return (flag & Flag_Interpolate) != 0;
            }

            /// <summary>
            /// フラグ判定
            /// </summary>
            /// <param name="flag"></param>
            /// <returns></returns>
            public bool IsFlag(uint flag)
            {
                return (this.flag & flag) != 0;
            }

            /// <summary>
            /// フラグ設定
            /// </summary>
            /// <param name="flag"></param>
            /// <param name="sw"></param>
            public void SetFlag(uint flag, bool sw)
            {
                if (sw)
                    this.flag |= flag;
                else
                    this.flag &= ~flag;
            }
        }

        /// <summary>
        /// チームデータリスト
        /// </summary>
        public FixedNativeList<TeamData> teamDataList;

        public FixedNativeList<CurveParam> teamMassList;
        public FixedNativeList<CurveParam> teamGravityList;
        public FixedNativeList<CurveParam> teamDragList;
        public FixedNativeList<CurveParam> teamMaxVelocityList;

        /// <summary>
        /// チームのワールド移動回転影響
        /// </summary>
        public struct WorldInfluence
        {
            /// <summary>
            /// 影響力(0.0-1.0)
            /// </summary>
            public CurveParam moveInfluence;
            public CurveParam rotInfluence;

            /// <summary>
            /// ワールド移動量
            /// </summary>
            public float3 nowPosition;
            public float3 oldPosition;
            public float3 moveOffset;

            /// <summary>
            /// ワールド回転量
            /// </summary>
            public quaternion nowRotation;
            public quaternion oldRotation;
            public quaternion rotationOffset;

            /// <summary>
            /// テレポート
            /// </summary>
            public int resetTeleport;
            public float teleportDistance;
            public float teleportRotation;
        }
        public FixedNativeList<WorldInfluence> teamWorldInfluenceList;

        /// <summary>
        /// チームごとの判定コライダー(キー:チームID, データ:コライダーパーティクルID)
        /// </summary>
        public ExNativeMultiHashMap<int, int> colliderMap;

        /// <summary>
        /// チームごとのチームコンポーネント参照への辞書（キー：チームID）
        /// nullはグローバルチーム
        /// </summary>
        private Dictionary<int, PhysicsTeam> teamComponentDict = new Dictionary<int, PhysicsTeam>();

        /// <summary>
        /// 稼働中のチーム数
        /// </summary>
        int activeTeamCount;

        //=========================================================================================
        /// <summary>
        /// 初期設定
        /// </summary>
        public override void Create()
        {
            teamDataList = new FixedNativeList<TeamData>();
            teamMassList = new FixedNativeList<CurveParam>();
            teamGravityList = new FixedNativeList<CurveParam>();
            teamDragList = new FixedNativeList<CurveParam>();
            teamMaxVelocityList = new FixedNativeList<CurveParam>();
            teamWorldInfluenceList = new FixedNativeList<WorldInfluence>();
            colliderMap = new ExNativeMultiHashMap<int, int>();

            // グローバルチーム[0]を作成し常に有効にしておく
            CreateTeam(null, 0);
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public override void Dispose()
        {
            if (teamDataList == null)
                return;

            colliderMap.Dispose();
            teamMassList.Dispose();
            teamGravityList.Dispose();
            teamDragList.Dispose();
            teamMaxVelocityList.Dispose();
            teamWorldInfluenceList.Dispose();
            teamDataList.Dispose();
        }

        //=========================================================================================
        /// <summary>
        /// 登録チーム数を返す
        /// [0]はグローバルチームなので-1する
        /// </summary>
        public int TeamCount
        {
            get
            {
                return teamDataList.Count - 1;
            }
        }

        /// <summary>
        /// チーム配列数を返す
        /// </summary>
        public int TeamLength
        {
            get
            {
                return teamDataList.Length;
            }
        }

        /// <summary>
        /// 現在活動中のチーム数を返す
        /// これが0の場合はチームが無いか、すべて停止中となっている
        /// </summary>
        public int ActiveTeamCount
        {
            get
            {
                return activeTeamCount;
            }
        }

        /// <summary>
        /// コライダーの数を返す
        /// </summary>
        public int ColliderCount
        {
            get
            {
                if (colliderMap == null)
                    return 0;

                return colliderMap.Count;
            }
        }

        //=========================================================================================
        /// <summary>
        /// チームを作成する
        /// </summary>
        /// <returns></returns>
        public int CreateTeam(PhysicsTeam team, uint flag)
        {
            var data = new TeamData();
            flag |= Flag_Enable;
            flag |= Flag_Reset_WorldInfluence; // 移動影響リセット
            data.flag = flag;

            data.friction = 0;
            data.boneIndex = team != null ? 0 : -1; // グローバルチームはボーン無し
            data.timeScale = 1.0f;
            data.blendRatio = 1.0f;

            // 拘束チームインデックス
            data.restoreDistanceGroupIndex = -1;
            data.triangleBendGroupIndex = -1;
            data.clampDistanceGroupIndex = -1;
            data.clampPositionGroupIndex = -1;
            data.clampRotationGroupIndex = -1;
            data.restoreRotationGroupIndex = -1;
            data.adjustRotationGroupIndex = -1;
            data.springGroupIndex = -1;
            data.volumeGroupIndex = -1;
            data.airLineGroupIndex = -1;
            data.lineWorkerGroupIndex = -1;
            data.selfCollisionGroupIndex = -1;
            data.edgeCollisionGroupIndex = -1;

            int teamId = teamDataList.Add(data);
            teamMassList.Add(new CurveParam(1.0f));
            teamGravityList.Add(new CurveParam());
            teamDragList.Add(new CurveParam());
            teamMaxVelocityList.Add(new CurveParam());

            teamWorldInfluenceList.Add(new WorldInfluence());

            teamComponentDict.Add(teamId, team);

            if (team != null)
                activeTeamCount++;

            return teamId;
        }

        /// <summary>
        /// チームを削除する
        /// </summary>
        /// <param name="teamId"></param>
        public void RemoveTeam(int teamId)
        {
            if (teamId >= 0)
            {
                teamDataList.Remove(teamId);
                teamMassList.Remove(teamId);
                teamGravityList.Remove(teamId);
                teamDragList.Remove(teamId);
                teamMaxVelocityList.Remove(teamId);
                teamWorldInfluenceList.Remove(teamId);
                teamComponentDict.Remove(teamId);
            }
        }

        /// <summary>
        /// チームの有効フラグ切り替え
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="sw"></param>
        public void SetEnable(int teamId, bool sw)
        {
            if (teamId >= 0)
            {
                SetFlag(teamId, Flag_Enable, sw);
                SetFlag(teamId, Flag_Reset_Position, sw); // 位置リセット
            }
        }

        /// <summary>
        /// チームが存在するか判定する
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public bool IsValid(int teamId)
        {
            return teamId >= 0;
        }

        /// <summary>
        /// チームが有効状態か判定する
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public bool IsActive(int teamId)
        {
            if (teamId >= 0)
                return teamDataList[teamId].IsActive();
            else
                return false;
        }

        /// <summary>
        /// チームの状態フラグ設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="flag"></param>
        /// <param name="sw"></param>
        public void SetFlag(int teamId, uint flag, bool sw)
        {
            if (teamId < 0)
                return;
            TeamData data = teamDataList[teamId];
            bool oldvalid = data.IsActive();
            data.SetFlag(flag, sw);
            bool newvalid = data.IsActive();
            if (oldvalid != newvalid)
            {
                // アクティブチーム数カウント
                activeTeamCount += newvalid ? 1 : -1;
            }
            teamDataList[teamId] = data;
        }

        public void SetParticleChunk(int teamId, ChunkData chunk)
        {
            TeamData data = teamDataList[teamId];
            data.particleChunk = chunk;
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームの摩擦係数設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="friction"></param>
        public void SetFriction(int teamId, float friction)
        {
            TeamData data = teamDataList[teamId];
            data.friction = friction;
            teamDataList[teamId] = data;
        }

        public void SetMass(int teamId, BezierParam mass)
        {
            teamMassList[teamId] = new CurveParam(mass);
        }

        public void SetGravity(int teamId, BezierParam gravity)
        {
            teamGravityList[teamId] = new CurveParam(gravity);
        }

        public void SetDrag(int teamId, BezierParam drag)
        {
            teamDragList[teamId] = new CurveParam(drag);
        }

        public void SetMaxVelocity(int teamId, BezierParam maxVelocity)
        {
            teamMaxVelocityList[teamId] = new CurveParam(maxVelocity);
        }

        /// <summary>
        /// ワールド移動影響設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="worldMoveInfluence"></param>
        public void SetWorldInfluence(int teamId, BezierParam moveInfluence, BezierParam rotInfluence, bool resetTeleport, float teleportDistance, float teleportRotation)
        {
            var data = teamWorldInfluenceList[teamId];
            data.moveInfluence = new CurveParam(moveInfluence);
            data.rotInfluence = new CurveParam(rotInfluence);
            data.resetTeleport = resetTeleport ? 1 : 0;
            data.teleportDistance = teleportDistance;
            data.teleportRotation = teleportRotation;
            teamWorldInfluenceList[teamId] = data;
        }

        /// <summary>
        /// セルフコリジョンの影響範囲設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="range"></param>
        public void SetSelfCollisionRange(int teamId, float range)
        {
            TeamData data = teamDataList[teamId];
            data.selfCollisionRange = range;
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームのボーンインデックスを設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="boneIndex"></param>
        public void SetBoneIndex(int teamId, int boneIndex)
        {
            TeamData data = teamDataList[teamId];
            data.boneIndex = boneIndex;
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームにコライダーを追加
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="particleIndex"></param>
        public void AddCollider(int teamId, int particleIndex)
        {
            colliderMap.Add(teamId, particleIndex);
        }

        /// <summary>
        /// チームからコライダーを削除
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="particleIndex"></param>
        public void RemoveCollider(int teamId, int particleIndex)
        {
            colliderMap.Remove(teamId, particleIndex);
        }

        /// <summary>
        /// チームのコライダーをすべて削除
        /// </summary>
        /// <param name="teamId"></param>
        public void RemoveCollider(int teamId)
        {
            colliderMap.Remove(teamId);
        }

        /// <summary>
        /// チームのタイムスケールを設定する
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="timeScale">0.0-1.0</param>
        public void SetTimeScale(int teamId, float timeScale)
        {
            TeamData data = teamDataList[teamId];
            data.timeScale = Mathf.Clamp01(timeScale);
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームのタイムスケールを取得する
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public float GetTimeScale(int teamId)
        {
            return teamDataList[teamId].timeScale;
        }

        /// <summary>
        /// チームのブレンド率を設定する
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="blendRatio"></param>
        public void SetBlendRatio(int teamId, float blendRatio)
        {
            TeamData data = teamDataList[teamId];
            data.blendRatio = Mathf.Clamp01(blendRatio);
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームのブレンド率を取得する
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public float GetBlendRatio(int teamId)
        {
            return teamDataList[teamId].blendRatio;
        }

        /// <summary>
        /// 外力を与える
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="force">１秒あたりの外力</param>
        public void SetImpactForce(int teamId, float3 force, ForceMode mode)
        {
            TeamData data = teamDataList[teamId];
            data.impactForce = force;
            data.forceMode = mode;
            teamDataList[teamId] = data;
        }

        //=========================================================================================
        /// <summary>
        /// アクティブ状態に限らず行うチーム更新
        /// </summary>
        public void PreUpdateTeamAlways()
        {
            var mainCamera = Camera.main.transform;

            foreach (var team in Team.teamComponentDict.Values)
            {
                var baseCloth = team as BaseCloth;
                if (baseCloth == null)
                    continue;

                // 距離無効化／有効化
                float blend = 1.0f;
                if (baseCloth.Params.UseDistanceDisable)
                {
                    var refObject = baseCloth.Params.DisableReferenceObject;
                    if (refObject == null)
                        refObject = mainCamera;

                    float dist = Vector3.Distance(team.transform.position, refObject.position);
                    float disableDist = baseCloth.Params.DisableDistance;
                    float fadeDist = Mathf.Max(disableDist - baseCloth.Params.DisableFadeDistance, 0.0f);

                    blend = Mathf.InverseLerp(disableDist, fadeDist, dist);
                }

                // 距離ブレンド率設定
                if (baseCloth.Setup.DistanceBlendRatio != blend)
                {
                    //Debug.Log("dist:" + dist + " blend:" + blend);
                    baseCloth.Setup.DistanceBlendRatio = blend;
                    baseCloth.UpdateBlend();
                }
            }
        }


        /// <summary>
        /// チームデータ前処理
        /// ワープ関連を考慮した最大アップデート回数の集計を行うためメインスレッドで実行する
        /// </summary>
        /// <param name="dtime"></param>
        /// <param name="updateDeltaTime"></param>
        /// <param name="ups"></param>
        /// <returns></returns>
        public int PreUpdateTeamData(float dtime, float updateDeltaTime, int ups)
        {
            bool unscaledUpdate = manager.UpdateTime.GetUpdateMode() == UpdateTimeManager.UpdateMode.UnscaledTime;

            int maxUpdateCount = 0;
            float globalTimeScale = manager.GetGlobalTimeScale();

            // 固定更新では１回の更新時間をupdateDeltaTimeに設定する
            if (unscaledUpdate == false)
                dtime = updateDeltaTime;

            // スロー再生の有無
            bool slow = false;

            for (int i = 0, cnt = Team.teamDataList.Length; i < cnt; i++)
            {
                int teamId = i;
                if (Team.teamComponentDict.ContainsKey(teamId) == false)
                    continue;
                var team = Team.teamComponentDict[teamId];
                if (team == null)
                    continue;

                var tdata = Team.teamDataList[teamId];
                if (tdata.IsActive() == false)
                {
                    tdata.updateCount = 0;
                    tdata.runCount = 0;
                    Team.teamDataList[teamId] = tdata;
                    continue;
                }

                // 速度影響／ワープ
                if (team.InfluenceTarget != null)
                {
                    // ワールド移動影響
                    WorldInfluence wdata = Team.teamWorldInfluenceList[teamId];
                    float3 bpos = team.InfluenceTarget.position;
                    quaternion brot = team.InfluenceTarget.rotation;

                    // 移動量算出
                    float3 movePos = bpos - wdata.oldPosition;
                    quaternion moveRot = MathUtility.FromToRotation(wdata.oldRotation, brot);
                    wdata.moveOffset = movePos;
                    wdata.rotationOffset = moveRot;

                    // テレポート判定
                    if (wdata.resetTeleport == 1)
                    {
                        if (math.length(movePos) >= wdata.teleportDistance || math.degrees(MathUtility.Angle(moveRot)) >= wdata.teleportRotation)
                        {
                            tdata.SetFlag(Flag_Reset_WorldInfluence, true);
                            tdata.SetFlag(Flag_Reset_Position, true);
                        }
                    }

                    if (tdata.IsFlag(Flag_Reset_WorldInfluence) || tdata.IsFlag(Flag_Reset_Position))
                    {
                        // リセット
                        wdata.moveOffset = 0;
                        wdata.rotationOffset = quaternion.identity;
                        wdata.oldPosition = bpos;
                        wdata.oldRotation = brot;

                        // チームタイムリセット（強制更新）
                        tdata.nowTime = updateDeltaTime;
                    }
                    wdata.nowPosition = bpos;
                    wdata.nowRotation = brot;

                    // 下記戻し
                    Team.teamWorldInfluenceList[teamId] = wdata;
                }

                // 更新フラグ（タイムスケール対応）
                tdata.updateCount = 0;
                tdata.runCount = 0;
                float timeScale = tdata.timeScale * globalTimeScale;
                float nowTime = tdata.nowTime + dtime * timeScale;
                // 時間ステップ
                while (nowTime >= updateDeltaTime)
                {
                    nowTime -= updateDeltaTime;
                    tdata.updateCount++;
                }

                // 最大実効回数（30で割る、90upsなら１フレーム最大3回の更新）
                tdata.updateCount = math.min(tdata.updateCount, ups / 30);

                maxUpdateCount = Mathf.Max(maxUpdateCount, tdata.updateCount);

                tdata.nowTime = nowTime;

                // 補間再生判定
                if (timeScale < 0.99f || Time.timeScale < 0.99f)
                {
                    tdata.SetFlag(Flag_Interpolate, true);
                    slow = true;
                }
                else
                {
                    tdata.SetFlag(Flag_Interpolate, false);
                }

                // リセットフラグOFF
                tdata.SetFlag(Flag_Reset_WorldInfluence, false);

                // 書き戻し
                Team.teamDataList[teamId] = tdata;
            }

            // グローバルスロー判定
            if (slow)
                maxUpdateCount = Mathf.Max(maxUpdateCount, 1); // スロー時は最低１回更新

            // 今回フレームの更新回数を返す
            return maxUpdateCount;
        }

        //=========================================================================================
#if false
        public void PreUpdateTeamData(float dtime, float updateDeltaTime)
        {
            // チームデータ前処理
            var job = new PreProcessTeamDataJob()
            {
                dtime = dtime,
                updateDeltaTime = updateDeltaTime,
                globalTimeScale = manager.UpdateTime.TimeScale,
                unscaledUpdate = manager.UpdateTime.GetUpdateMode() == UpdateTimeManager.UpdateMode.FixedTimeStep,
                ups = manager.UpdateTime.UpdatePerSecond,

                teamData = Team.teamDataList.ToJobArray(),
                teamWorldInfluenceList = Team.teamWorldInfluenceList.ToJobArray(),

                bonePosList = Bone.bonePosList.ToJobArray(),
                boneRotList = Bone.boneRotList.ToJobArray(),
            };
            Compute.MasterJob = job.Schedule(Team.teamDataList.Length, 8, Compute.MasterJob);
        }

        [BurstCompile]
        struct PreProcessTeamDataJob : IJobParallelFor
        {
            public float dtime;
            public float updateDeltaTime;
            public float globalTimeScale;
            public bool unscaledUpdate;
            public int ups;

            public NativeArray<TeamData> teamData;
            public NativeArray<WorldInfluence> teamWorldInfluenceList;

            [ReadOnly]
            public NativeArray<float3> bonePosList;
            [ReadOnly]
            public NativeArray<quaternion> boneRotList;

            // チームデータごと
            public void Execute(int index)
            {
                var tdata = teamData[index];
                if (tdata.IsActive() == false || tdata.boneIndex < 0)
                {
                    tdata.updateCount = 0;
                    tdata.runCount = 0;
                    teamData[index] = tdata;
                    return;
                }

                // ワールド移動影響
                WorldInfluence wdata = teamWorldInfluenceList[index];
                var bpos = bonePosList[tdata.boneIndex];
                var brot = boneRotList[tdata.boneIndex];

                // 移動量算出
                float3 movePos = bpos - wdata.oldPosition;
                quaternion moveRot = MathUtility.FromToRotation(wdata.oldRotation, brot);
                wdata.moveOffset = movePos;
                wdata.rotationOffset = moveRot;

                // テレポート判定
                if (wdata.resetTeleport)
                {
                    if (math.length(movePos) >= wdata.teleportDistance || math.degrees(MathUtility.Angle(moveRot)) >= wdata.teleportRotation)
                    {
                        tdata.SetFlag(Flag_Reset_WorldInfluence, true);
                        tdata.SetFlag(Flag_Reset_Position, true);
                    }
                }

                if (tdata.IsFlag(Flag_Reset_WorldInfluence) || tdata.IsFlag(Flag_Reset_Position))
                {
                    // リセット
                    wdata.moveOffset = 0;
                    wdata.rotationOffset = quaternion.identity;
                    wdata.oldPosition = bpos;
                    wdata.oldRotation = brot;

                    // チームタイムリセット（強制更新）
                    //tdata.nowTime = updateDeltaTime;
                }
                wdata.nowPosition = bpos;
                wdata.nowRotation = brot;

                // 更新フラグ（タイムスケール対応）
                tdata.runCount = 0;
                //tdata.updateCount = 0;
                //float nowTime = tdata.nowTime + dtime * tdata.timeScale * globalTimeScale;
                //if (unscaledUpdate)
                //{
                //    // 固定時間ステップ
                //    while (nowTime >= updateDeltaTime)
                //    {
                //        nowTime -= updateDeltaTime;
                //        tdata.updateCount++;
                //    }

                //    // 最大実効回数（30で割る、90upsなら１フレーム最大3回の更新）
                //    tdata.updateCount = math.min(tdata.updateCount, ups / 30);
                //}
                //else
                //{
                //    // １フレームに１回（もしくはリセット）
                //    tdata.updateCount = 1;
                //    nowTime = 0;
                //}
                //tdata.nowTime = nowTime;

                // リセットフラグOFF
                tdata.SetFlag(Flag_Reset_WorldInfluence, false);

                // 書き戻し
                teamData[index] = tdata;
                teamWorldInfluenceList[index] = wdata;
            }
        }
#endif

        //=========================================================================================
        public void PostUpdateTeamData()
        {
            // チームデータ後処理
            var job = new PostProcessTeamDataJob()
            {
                teamData = Team.teamDataList.ToJobArray(),
                teamWorldInfluenceList = Team.teamWorldInfluenceList.ToJobArray(),
            };
            Compute.MasterJob = job.Schedule(Team.teamDataList.Length, 8, Compute.MasterJob);
        }

        [BurstCompile]
        struct PostProcessTeamDataJob : IJobParallelFor
        {
            public NativeArray<TeamData> teamData;
            public NativeArray<WorldInfluence> teamWorldInfluenceList;

            // チームデータごと
            public void Execute(int index)
            {
                var tdata = teamData[index];
                if (tdata.IsActive() == false)
                    return;

                var wdata = teamWorldInfluenceList[index];

                wdata.oldPosition = wdata.nowPosition;
                wdata.oldRotation = wdata.nowRotation;

                if (tdata.IsRunning())
                {
                    // 外部フォースをリセット
                    tdata.impactForce = 0;
                    tdata.forceMode = ForceMode.None;
                }

                // 姿勢リセットフラグリセット
                tdata.SetFlag(Flag_Reset_Position, false);

                // 書き戻し
                teamData[index] = tdata;
                teamWorldInfluenceList[index] = wdata;
            }
        }

        //=========================================================================================
        public void UpdateTeamUpdateCount()
        {
            // チームデータ後処理
            var job = new UpdateTeamUpdateCountJob()
            {
                teamData = Team.teamDataList.ToJobArray(),
            };
            Compute.MasterJob = job.Schedule(Team.teamDataList.Length, 8, Compute.MasterJob);
        }

        [BurstCompile]
        struct UpdateTeamUpdateCountJob : IJobParallelFor
        {
            public NativeArray<TeamData> teamData;

            // チームデータごと
            public void Execute(int index)
            {
                var tdata = teamData[index];
                if (tdata.IsActive() == false)
                    return;

                //tdata.updateCount = math.max(tdata.updateCount - 1, 0);
                tdata.runCount++;

                // 書き戻し
                teamData[index] = tdata;
            }
        }
    }
}
