// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// MagicaCloth物理マネージャ
    /// </summary>
    [HelpURL("https://magicasoft.jp/magica-cloth-physics-manager/")]
    public partial class MagicaPhysicsManager : CreateSingleton<MagicaPhysicsManager>
    {
        /// <summary>
        /// 更新管理
        /// </summary>
        [SerializeField]
        UpdateTimeManager updateTime = new UpdateTimeManager();

        /// <summary>
        /// パーティクルデータ
        /// </summary>
        PhysicsManagerParticleData particle = new PhysicsManagerParticleData();

        /// <summary>
        /// トランスフォームデータ
        /// </summary>
        PhysicsManagerBoneData bone = new PhysicsManagerBoneData();

        /// <summary>
        /// メッシュデータ
        /// </summary>
        PhysicsManagerMeshData mesh = new PhysicsManagerMeshData();

        /// <summary>
        /// チームデータ
        /// </summary>
        PhysicsManagerTeamData team = new PhysicsManagerTeamData();

        /// <summary>
        /// 物理計算処理
        /// </summary>
        PhysicsManagerCompute compute = new PhysicsManagerCompute();

        //=========================================================================================
        public UpdateTimeManager UpdateTime
        {
            get
            {
                return updateTime;
            }
        }

        public PhysicsManagerParticleData Particle
        {
            get
            {
                particle.SetParent(this);
                return particle;
            }
        }

        public PhysicsManagerBoneData Bone
        {
            get
            {
                bone.SetParent(this);
                return bone;
            }
        }

        public PhysicsManagerMeshData Mesh
        {
            get
            {
                mesh.SetParent(this);
                return mesh;
            }
        }

        public PhysicsManagerTeamData Team
        {
            get
            {
                team.SetParent(this);
                return team;
            }
        }

        public PhysicsManagerCompute Compute
        {
            get
            {
                compute.SetParent(this);
                return compute;
            }
        }

        //=========================================================================================
        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// 初期化
        /// </summary>
        protected override void InitSingleton()
        {
            Particle.Create();
            Bone.Create();
            Mesh.Create();
            Team.Create();
            Compute.Create();
        }

        /// <summary>
        /// ２つ目の破棄されるマネージャの通知
        /// </summary>
        /// <param name="duplicate"></param>
        protected override void DuplicateDetection(MagicaPhysicsManager duplicate)
        {
            // 設定をコピーする
            UpdateMode = duplicate.UpdateMode;
            UpdatePerSeccond = duplicate.UpdatePerSeccond;
        }

        /// <summary>
        /// 破棄
        /// </summary>
        private void OnDestroy()
        {
            Compute.Dispose();
            Team.Dispose();
            Mesh.Dispose();
            Bone.Dispose();
            Particle.Dispose();
        }

        /// <summary>
        /// 更新（アニメーション前）
        /// </summary>
        private void Update()
        {
            Compute.Update();
        }

        /// <summary>
        /// 更新（アニメーション後）
        /// </summary>
        private void LateUpdate()
        {
            Compute.LateUpdate(updateTime);
        }
    }
}
