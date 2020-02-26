// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp

using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 物理マネージャAPI
    /// </summary>
    public partial class MagicaPhysicsManager : CreateSingleton<MagicaPhysicsManager>
    {
        /// <summary>
        /// １秒あたりの更新回数
        /// </summary>
        public UpdateTimeManager.UpdateCount UpdatePerSeccond
        {
            get
            {
                return (UpdateTimeManager.UpdateCount)UpdateTime.UpdatePerSecond;
            }
            set
            {
                UpdateTime.SetUpdatePerSecond(value);
            }
        }

        /// <summary>
        /// 更新モード
        /// </summary>
        public UpdateTimeManager.UpdateMode UpdateMode
        {
            get
            {
                return UpdateTime.GetUpdateMode();
            }
            set
            {
                UpdateTime.SetUpdateMode(value);
            }
        }


        /// <summary>
        /// グローバルタイムスケールを設定する
        /// </summary>
        /// <param name="timeScale">0.0-1.0</param>
        public void SetGlobalTimeScale(float timeScale)
        {
            UpdateTime.TimeScale = Mathf.Clamp01(timeScale);
        }

        /// <summary>
        /// グローバルタイムスケールを取得する
        /// </summary>
        /// <returns></returns>
        public float GetGlobalTimeScale()
        {
            return UpdateTime.TimeScale;
        }
    }
}
