// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp

using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// クロス基本クラスAPI
    /// </summary>
    public abstract partial class BaseCloth : PhysicsTeam
    {
        /// <summary>
        /// クロスの物理シミュレーションをリセットします
        /// </summary>
        public void ResetCloth()
        {
            if (IsValid())
            {
                MagicaPhysicsManager.Instance.Team.SetFlag(teamId, PhysicsManagerTeamData.Flag_Reset_WorldInfluence, true);
                MagicaPhysicsManager.Instance.Team.SetFlag(teamId, PhysicsManagerTeamData.Flag_Reset_Position, true);
            }
        }

        /// <summary>
        /// タイムスケールを変更します
        /// </summary>
        /// <param name="timeScale">0.0-1.0</param>
        public void SetTimeScale(float timeScale)
        {
            if (IsValid())
                MagicaPhysicsManager.Instance.Team.SetTimeScale(teamId, Mathf.Clamp01(timeScale));
        }

        /// <summary>
        /// タイムスケールを取得します
        /// </summary>
        /// <returns></returns>
        public float GetTimeScale()
        {
            if (IsValid())
                return MagicaPhysicsManager.Instance.Team.GetTimeScale(teamId);
            else
                return 1.0f;
        }

        /// <summary>
        /// 外力を与えます
        /// </summary>
        /// <param name="force"></param>
        public void AddForce(Vector3 force, PhysicsManagerTeamData.ForceMode mode)
        {
            if (IsValid() && IsActive())
                MagicaPhysicsManager.Instance.Team.SetImpactForce(teamId, force, mode);
        }

        /// <summary>
        /// 元の姿勢とシミュレーション結果とのブレンド率
        /// (0.0 = 0%, 1.0 = 100%)
        /// </summary>
        public float BlendWeight
        {
            get
            {
                return TeamData.UserBlendRatio;
            }
            set
            {
                TeamData.UserBlendRatio = value;
                UpdateBlend();
            }
        }
    }
}
