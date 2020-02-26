// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace MagicaCloth
{
    /// <summary>
    /// 計算処理
    /// </summary>
    public class PhysicsManagerCompute : PhysicsManagerAccess
    {
        /// <summary>
        /// 拘束判定繰り返し回数
        /// </summary>
        //[Header("拘束全体の反復回数")]
        //[Range(1, 8)]
        //public int solverIteration = 2;
        private int solverIteration = 1;

        /// <summary>
        /// 拘束条件
        /// </summary>
        List<PhysicsManagerConstraint> constraints = new List<PhysicsManagerConstraint>();

        public ClampPositionConstraint ClampPosition { get; private set; }
        public ClampDistanceConstraint ClampDistance { get; private set; }
        public ClampRotationConstraint ClampRotation { get; private set; }
        public SpringConstraint Spring { get; private set; }
        public RestoreDistanceConstraint RestoreDistance { get; private set; }
        public RestoreRotationConstraint RestoreRotation { get; private set; }
        public TriangleBendConstraint TriangleBend { get; private set; }
        public VolumeConstraint Volume { get; private set; }
        public ColliderCollisionConstraint Collision { get; private set; }
        //public EdgeCollisionConstraint EdgeCollision { get; private set; }

        /// <summary>
        /// ワーカーリスト
        /// </summary>
        List<PhysicsManagerWorker> workers = new List<PhysicsManagerWorker>();
        public RenderMeshWorker RenderMeshWorker { get; private set; }
        public VirtualMeshWorker VirtualMeshWorker { get; private set; }
        public MeshParticleWorker MeshParticleWorker { get; private set; }
        public SpringMeshWorker SpringMeshWorker { get; private set; }
        public AdjustRotationWorker AdjustRotationWorker { get; private set; }
        public LineWorker LineWorker { get; private set; }

        /// <summary>
        /// マスタージョブハンドル
        /// すべてのジョブはこのハンドルに連結される
        /// </summary>
        JobHandle jobHandle;

        //=========================================================================================
        /// <summary>
        /// 初期設定
        /// </summary>
        public override void Create()
        {
            // 拘束の作成
            // ※この並び順が実行順番となります。

            // 移動制限
            ClampDistance = new ClampDistanceConstraint();
            constraints.Add(ClampDistance);
            ClampPosition = new ClampPositionConstraint();
            constraints.Add(ClampPosition);
            ClampRotation = new ClampRotationConstraint();
            constraints.Add(ClampRotation);

            // 主なクロスシミュレーション
            Spring = new SpringConstraint();
            constraints.Add(Spring);
            RestoreDistance = new RestoreDistanceConstraint();
            constraints.Add(RestoreDistance);
            RestoreRotation = new RestoreRotationConstraint();
            constraints.Add(RestoreRotation);

            // コリジョン
            //EdgeCollision = new EdgeCollisionConstraint();
            //constraints.Add(EdgeCollision);
            Collision = new ColliderCollisionConstraint();
            constraints.Add(Collision);

            // 形状維持
            TriangleBend = new TriangleBendConstraint();
            constraints.Add(TriangleBend);
            Volume = new VolumeConstraint();
            constraints.Add(Volume);

            foreach (var con in constraints)
                con.Init(manager);

            // ワーカーの作成
            // ※この並び順は変更してはいけません。
            RenderMeshWorker = new RenderMeshWorker();
            workers.Add(RenderMeshWorker);
            VirtualMeshWorker = new VirtualMeshWorker();
            workers.Add(VirtualMeshWorker);
            MeshParticleWorker = new MeshParticleWorker();
            workers.Add(MeshParticleWorker);
            SpringMeshWorker = new SpringMeshWorker();
            workers.Add(SpringMeshWorker);
            AdjustRotationWorker = new AdjustRotationWorker();
            workers.Add(AdjustRotationWorker);
            LineWorker = new LineWorker();
            workers.Add(LineWorker);
            foreach (var worker in workers)
                worker.Init(manager);
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public override void Dispose()
        {
            if (constraints != null)
            {
                foreach (var con in constraints)
                    con.Release();
            }
            if (workers != null)
            {
                foreach (var worker in workers)
                    worker.Release();
            }
        }

        /// <summary>
        /// 各コンストレイント／ワーカーから指定グループのデータを削除する
        /// </summary>
        /// <param name="teamId"></param>
        public void RemoveTeam(int teamId)
        {
            if (constraints != null)
            {
                foreach (var con in constraints)
                    con.RemoveTeam(teamId);
            }
            if (workers != null)
            {
                foreach (var worker in workers)
                    worker.RemoveGroup(teamId);
            }
        }

        //=========================================================================================
        /// <summary>
        /// アニメーション前の更新
        /// </summary>
        public void Update()
        {
            // 活動チームが１つ以上ある場合のみ更新
            if (Team.ActiveTeamCount > 0)
            {
                // マスター／ウォームアップジョブハンドル初期化
                InitJob();

                // トランスフォーム姿勢のリセット
                Bone.ResetBoneFromTransform();

                // マスタージョブ完了待機
                CompleteJob();
            }
        }

        //=========================================================================================
        /// <summary>
        /// アニメーション後の更新
        /// </summary>
        /// <param name="update"></param>
        public void LateUpdate(UpdateTimeManager update)
        {
            // 時間
            float dtime = Time.deltaTime;
            float updatePower = update.UpdatePower;
            float updateIntervalTime = update.UpdateIntervalTime;
            int ups = update.UpdatePerSecond;

            // 常に実行するチームデータ更新
            Team.PreUpdateTeamAlways();

            // 活動チームが１つ以上ある場合のみ更新
            if (Team.ActiveTeamCount > 0)
            {
                // チームデータ更新、ワールド移動影響、最大更新回数計算（これはメインスレッド）
                int updateCount = Team.PreUpdateTeamData(dtime, updateIntervalTime, ups);

                // マスター／ウォームアップジョブハンドル初期化
                InitJob();

                // トランスフォーム姿勢の読み込み
                Bone.ReadBoneFromTransform();

                // トランスフォーム読み込み中のワーカー処理
                WarmupWorker();

                // ボーン姿勢をパーティクルにコピーする
                Particle.UpdateBoneToParticle();

                // ウォームアップワーカーの実行と完了待ち（ボーン読み込みJobと並列で動作）
                CompleteWarmupWorker();

                // 物理更新前ワーカー処理
                // ・レンダーメッシュ座標読み込み（何もなし）
                // ・仮想メッシュ座標読み込み（仮想メッシュスキニング）
                // ・仮想メッシュ座標をパーティクルに反映させる
                // ・メッシュスプリング（何もなし）
                // ・回転調整（何もなし）
                // ・ライン（何もなし）
                MasterJob = RenderMeshWorker.PreUpdate(MasterJob);
                MasterJob = VirtualMeshWorker.PreUpdate(MasterJob);
                MasterJob = MeshParticleWorker.PreUpdate(MasterJob);
                MasterJob = SpringMeshWorker.PreUpdate(MasterJob);
                MasterJob = AdjustRotationWorker.PreUpdate(MasterJob);
                MasterJob = LineWorker.PreUpdate(MasterJob);

                // 物理更新
                for (int i = 0, cnt = updateCount; i < cnt; i++)
                {
                    UpdatePhysics(updateCount, i, updatePower, updateIntervalTime);
                }

                // 物理更新後ワーカー処理
                // ・ライン（ラインの回転調整）
                // ・回転調整
                // ・メッシュスプリング
                // ・パーティクル姿勢を仮想メッシュに書き出す
                // ・仮想メッシュ座標書き込み（仮想メッシュトライアングル法線計算）
                // ・レンダーメッシュ座標書き込み（仮想メッシュからレンダーメッシュ座標計算）
                MasterJob = LineWorker.PostUpdate(MasterJob);
                MasterJob = AdjustRotationWorker.PostUpdate(MasterJob);
                // パーティクル姿勢をボーン姿勢に書き戻す
                Particle.UpdateParticleToBone(); // ここに挟まないと駄目
                MasterJob = SpringMeshWorker.PostUpdate(MasterJob);
                MasterJob = MeshParticleWorker.PostUpdate(MasterJob);
                MasterJob = VirtualMeshWorker.PostUpdate(MasterJob);
                MasterJob = RenderMeshWorker.PostUpdate(MasterJob);

                // チームデータ後処理
                Team.PostUpdateTeamData();

                // ボーン姿勢をトランスフォームに書き出す
                Bone.WriteBoneToTransform();

                // マスタージョブ完了待機
                CompleteJob();
            }

            // 物理演算更新後のメッシュ後処理（主にメッシュへの頂点書き戻し）
            if (Mesh.VirtualMeshCount > 0)
                Mesh.FinishMesh();
        }

        //=========================================================================================
        public JobHandle MasterJob
        {
            get
            {
                return jobHandle;
            }
            set
            {
                jobHandle = value;
            }
        }

        /// <summary>
        /// マスタージョブハンドル初期化
        /// </summary>
        void InitJob()
        {
            jobHandle = default(JobHandle);
        }

        /// <summary>
        /// マスタージョブハンドル完了待機
        /// </summary>
        void CompleteJob()
        {
            jobHandle.Complete();
            jobHandle = default(JobHandle);
        }

        //=========================================================================================
        /// <summary>
        /// 物理エンジン更新ループ処理
        /// これは１フレームにステップ回数分呼び出される
        /// 場合によっては１回も呼ばれないフレームも発生するので注意！
        /// </summary>
        /// <param name="updateCount"></param>
        /// <param name="loopIndex"></param>
        /// <param name="dtime"></param>
        void UpdatePhysics(int updateCount, int loopIndex, float updatePower, float updateDeltaTime)
        {
            if (Particle.Count == 0)
                return;

            // フォース影響＋速度更新
            var job1 = new ForceAndVelocityJob()
            {
                updateDeltaTime = updateDeltaTime,
                updatePower = updatePower,
                step = math.saturate((float)(loopIndex + 1) / (float)updateCount),
                loopIndex = loopIndex,

                teamDataList = Team.teamDataList.ToJobArray(),
                teamMassList = Team.teamMassList.ToJobArray(),
                teamGravityList = Team.teamGravityList.ToJobArray(),
                teamDragList = Team.teamDragList.ToJobArray(),
                teamMaxVelocityList = Team.teamMaxVelocityList.ToJobArray(),

                flagList = Particle.flagList.ToJobArray(),
                teamIdList = Particle.teamIdList.ToJobArray(),
                depthList = Particle.depthList.ToJobArray(),
                basePosList = Particle.basePosList.ToJobArray(),
                baseRotList = Particle.baseRotList.ToJobArray(),

                nextPosList = Particle.InNextPosList.ToJobArray(),
                nextRotList = Particle.InNextRotList.ToJobArray(),
                oldPosList = Particle.oldPosList.ToJobArray(),
                oldRotList = Particle.oldRotList.ToJobArray(),
                frictionList = Particle.frictionList.ToJobArray(),
                oldSlowPosList = Particle.oldSlowPosList.ToJobArray(),

                posList = Particle.posList.ToJobArray(),
                rotList = Particle.rotList.ToJobArray(),
                velocityList = Particle.velocityList.ToJobArray()
            };
            jobHandle = job1.Schedule(Particle.Length, 64, jobHandle);

            // 拘束条件解決
            if (constraints != null)
            {
                // 拘束解決反復数分ループ
                for (int i = 0; i < solverIteration; i++)
                {
                    foreach (var con in constraints)
                    {
                        if (con != null /*&& con.enabled*/)
                        {
                            // 拘束ごとの反復回数
                            for (int j = 0; j < con.GetIterationCount(); j++)
                            {
                                jobHandle = con.SolverConstraint(updateDeltaTime, updatePower, j, jobHandle);
                            }
                        }
                    }
                }
            }

            // 座標確定
            var job2 = new FixPositionJob()
            {
                updatePower = updatePower,
                updateDeltaTime = updateDeltaTime,
                //globalTimeScale = manager.UpdateTime.TimeScale,

                teamDataList = Team.teamDataList.ToJobArray(),

                flagList = Particle.flagList.ToJobArray(),
                teamIdList = Particle.teamIdList.ToJobArray(),
                nextPosList = Particle.InNextPosList.ToJobArray(),
                nextRotList = Particle.InNextRotList.ToJobArray(),

                basePosList = Particle.basePosList.ToJobArray(),
                baseRotList = Particle.baseRotList.ToJobArray(),

                oldPosList = Particle.oldPosList.ToJobArray(),
                oldRotList = Particle.oldRotList.ToJobArray(),
                oldSlowPosList = Particle.oldSlowPosList.ToJobArray(),

                frictionList = Particle.frictionList.ToJobArray(),

                velocityList = Particle.velocityList.ToJobArray(),
                rotList = Particle.rotList.ToJobArray(),
                posList = Particle.posList.ToJobArray()
            };
            jobHandle = job2.Schedule(Particle.Length, 64, jobHandle);

            // チーム更新カウント減算
            Team.UpdateTeamUpdateCount();
        }

        [BurstCompile]
        struct ForceAndVelocityJob : IJobParallelFor
        {
            public float updateDeltaTime;
            public float updatePower;
            public float step;
            public int loopIndex;

            // チーム
            [ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;
            [ReadOnly]
            public NativeArray<CurveParam> teamMassList;
            [ReadOnly]
            public NativeArray<CurveParam> teamGravityList;
            [ReadOnly]
            public NativeArray<CurveParam> teamDragList;
            [ReadOnly]
            public NativeArray<CurveParam> teamMaxVelocityList;

            // パーティクル
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [ReadOnly]
            public NativeArray<int> teamIdList;
            [ReadOnly]
            public NativeArray<float> depthList;
            [ReadOnly]
            public NativeArray<float3> basePosList;
            [ReadOnly]
            public NativeArray<quaternion> baseRotList;

            [WriteOnly]
            public NativeArray<float3> nextPosList;
            [WriteOnly]
            public NativeArray<quaternion> nextRotList;

            //[WriteOnly]
            public NativeArray<float> frictionList;

            public NativeArray<float3> posList;
            public NativeArray<quaternion> rotList;

            public NativeArray<float3> velocityList;

            //[ReadOnly]
            public NativeArray<float3> oldPosList;
            //[ReadOnly]
            public NativeArray<quaternion> oldRotList;

            [WriteOnly]
            public NativeArray<float3> oldSlowPosList;

            // パーティクルごと
            public void Execute(int index)
            {
                var flag = flagList[index];
                if (flag.IsValid() == false)
                    return;

                // チームデータ
                int teamId = teamIdList[index];
                var teamData = teamDataList[teamId];

                var pos = oldPosList[index];
                var rot = oldRotList[index];
                float3 nextPos = pos;
                quaternion nextRot = rot;

                if (flag.IsFlag(PhysicsManagerParticleData.Flag_Reset_Position) || teamData.IsFlag(PhysicsManagerTeamData.Flag_Reset_Position))
                {
                    // 位置回転速度リセット
                    // ★Baseデータが無い場合はどうする？
                    nextPos = basePosList[index];
                    nextRot = baseRotList[index];
                    pos = nextPos;
                    rot = nextRot;

                    oldPosList[index] = nextPos;
                    oldRotList[index] = nextRot;

                    // スロー用位置リセット
                    oldSlowPosList[index] = nextPos;

                    // 速度リセット
                    velocityList[index] = 0;

                    // フラグクリア
                    flag.SetFlag(PhysicsManagerParticleData.Flag_Reset_Position, false);
                }
                else if (flag.IsFixed())
                {
                    // キネマティックパーティクル
                    if (flag.IsFlag(PhysicsManagerParticleData.Flag_Step_Update))
                    {
                        // OldPos/Rot から BasePos/Rot に step で補間して現在姿勢とする
                        nextPos = math.lerp(pos, basePosList[index], step);
                        nextRot = math.slerp(rot, baseRotList[index], step);
                    }
                }
                else if (teamData.IsUpdate())
                {
                    // 動的パーティクル
                    var depth = depthList[index];
                    var maxVelocity = teamMaxVelocityList[teamId].Evaluate(depth);
                    var drag = teamDragList[teamId].Evaluate(depth);
                    var gravity = teamGravityList[teamId].Evaluate(depth);
                    var mass = teamMassList[teamId].Evaluate(depth);
                    var velocity = velocityList[index];

                    // 最大速度
                    velocity = MathUtility.ClampVector(velocity, 0.0f, maxVelocity);

                    // 空気抵抗(90ups基準)
                    // 重力に影響させたくないので先に計算する（※通常はforce適用後に行うのが一般的）
                    velocity *= math.pow(1.0f - drag, updatePower);

                    // フォース
                    // フォースは空気抵抗を無視して加算する
                    float3 force = 0;

                    // 重力
                    // 重力は質量に関係なく一定
                    // (最後に質量で割るためここでは質量をかける）
                    force.y += gravity * mass;

                    // 外部フォース
                    if (loopIndex == 0)
                    {
                        switch (teamData.forceMode)
                        {
                            case PhysicsManagerTeamData.ForceMode.VelocityAdd:
                                force += teamData.impactForce;
                                break;
                            case PhysicsManagerTeamData.ForceMode.VelocityAddWithoutMass:
                                force += teamData.impactForce * mass;
                                break;
                            case PhysicsManagerTeamData.ForceMode.VelocityChange:
                                force += teamData.impactForce;
                                velocity = 0;
                                break;
                            case PhysicsManagerTeamData.ForceMode.VelocityChangeWithoutMass:
                                force += teamData.impactForce * mass;
                                velocity = 0;
                                break;
                        }
                    }

                    // 速度計算(質量で割る)
                    velocity += (force / mass) * updateDeltaTime;

                    // 速度を理想位置に反映させる
                    nextPos = pos + velocity * updateDeltaTime;
                }
                else
                {
                    // 補間モード（スロー）
                    // 何もしない
                    return;
                }

                // 予定座標更新 ==============================================================

                // 摩擦クリア
                //frictionList[index] = 0;

                // 摩擦減衰
                frictionList[index] = frictionList[index] * 0.5f; // 0.5?

                // 移動前の姿勢
                posList[index] = pos;
                rotList[index] = rot;

                // 予測位置
                nextPosList[index] = nextPos;
                nextRotList[index] = nextRot;

                flagList[index] = flag;
            }
        }

        [BurstCompile]
        struct FixPositionJob : IJobParallelFor
        {
            public float updatePower;
            public float updateDeltaTime;
            //public float globalTimeScale;

            // チーム
            [ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;

            // パーティクルごと
            [ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [ReadOnly]
            public NativeArray<int> teamIdList;
            [ReadOnly]
            public NativeArray<float3> nextPosList;
            [ReadOnly]
            public NativeArray<quaternion> nextRotList;
            [ReadOnly]
            public NativeArray<float> frictionList;
            [ReadOnly]
            public NativeArray<float3> basePosList;
            [ReadOnly]
            public NativeArray<quaternion> baseRotList;

            // パーティクルごと
            public NativeArray<float3> velocityList;
            [WriteOnly]
            public NativeArray<quaternion> rotList;

            public NativeArray<float3> oldPosList;
            public NativeArray<quaternion> oldRotList;
            public NativeArray<float3> oldSlowPosList;

            public NativeArray<float3> posList;

            // パーティクルごと
            public void Execute(int index)
            {
                var flag = flagList[index];
                if (flag.IsValid() == false)
                    return;

                // チームデータ
                int teamId = teamIdList[index];
                var teamData = teamDataList[teamId];

                var nextPos = nextPosList[index];
                var nextRot = nextRotList[index];
                nextRot = math.normalize(nextRot); // 回転蓄積で精度が落ちていくので正規化しておく

                // 表示位置
                var viewPos = nextPos;
                var viewRot = nextRot;

                // 速度更新(m/s)
                if (flag.IsFixed() == false)
                {
                    if (teamData.IsInterpolate())
                    {
                        // 補間モード（スロー）
                        // 未来予測
                        float ratio = teamData.IsUpdate() ? 1.0f : teamData.nowTime / updateDeltaTime;
                        var futurePos = oldPosList[index] + velocityList[index] * updateDeltaTime;
                        viewPos = math.lerp(oldSlowPosList[index], futurePos, ratio);
                    }

                    if (teamData.IsUpdate())
                    {
                        float3 velocity = 0;

                        // 移動パーティクルのみ速度を更新する
                        var pos = posList[index];

                        // 速度更新(m/s)
                        velocity = (nextPos - pos) / updateDeltaTime;

                        // 摩擦による速度減衰
                        float friction = frictionList[index];
                        velocity *= math.pow(1.0f - friction, updatePower);

                        // 書き戻し
                        velocityList[index] = velocity;
                    }
                }

                if (teamData.IsUpdate())
                {
                    oldPosList[index] = nextPos;
                    oldRotList[index] = nextRot;

                    // スロー用のスナップ位置を記録
                    oldSlowPosList[index] = viewPos;
                }

                // ブレンド
                if (teamData.blendRatio < 0.99f)
                {
                    viewPos = math.lerp(basePosList[index], viewPos, teamData.blendRatio);
                    viewRot = math.slerp(baseRotList[index], viewRot, teamData.blendRatio);
                }

                // 表示位置
                posList[index] = viewPos;
                rotList[index] = viewRot;
            }
        }

        //=========================================================================================
        /// <summary>
        /// トランスフォームリード中に実行するワーカーウォームアップ処理
        /// </summary>
        void WarmupWorker()
        {
            if (workers == null || workers.Count == 0)
                return;

            for (int i = 0; i < workers.Count; i++)
            {
                var worker = workers[i];
                worker.Warmup();
            }
        }

        /// <summary>
        /// ウォームアップ処理の完了待ち
        /// </summary>
        void CompleteWarmupWorker()
        {
            if (workers == null || workers.Count == 0)
                return;

            for (int i = 0; i < workers.Count; i++)
            {
                var worker = workers[i];
                worker.CompleteWarmup();
            }
        }
    }
}
