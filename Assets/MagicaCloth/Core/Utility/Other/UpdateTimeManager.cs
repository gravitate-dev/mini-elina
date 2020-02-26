// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 時間管理クラス
    /// </summary>
    [System.Serializable]
    public class UpdateTimeManager
    {
        // アップデート回数
        public enum UpdateCount
        {
            _60 = 60,
            _90_Default = 90,
            _120 = 120,
            _150 = 150,
            _180 = 180,
        }

        // １秒間の更新回数
        [SerializeField]
        private UpdateCount updatePerSeccond = UpdateCount._90_Default;

        // 更新モード
        public enum UpdateMode
        {
            UnscaledTime = 0,   // 非同期更新
            OncePerFrame = 1,   // 固定更新（基本１フレームに１回）
        }
        [SerializeField]
        private UpdateMode updateMode = UpdateMode.UnscaledTime;

        // タイムスケール
        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float timeScale = 1.0f;

        /// <summary>
        /// アップデートモード取得
        /// </summary>
        /// <returns></returns>
        public UpdateMode GetUpdateMode()
        {
            return updateMode;
        }

        public void SetUpdateMode(UpdateMode mode)
        {
            updateMode = mode;
        }

        /// <summary>
        /// １秒間の更新回数
        /// </summary>
        public int UpdatePerSecond
        {
            get
            {
                return (int)updatePerSeccond;
            }
        }

        public void SetUpdatePerSecond(UpdateCount ucount)
        {
            updatePerSeccond = ucount;
        }

        /// <summary>
        /// 更新間隔時間
        /// </summary>
        public float UpdateIntervalTime
        {
            get
            {
                return 1.0f / UpdatePerSecond;
            }
        }

        /// <summary>
        /// 更新力（90upsを基準とした倍数）
        /// 60fps = 1.5 / 120fps = 0.75
        /// </summary>
        public float UpdatePower
        {
            get
            {
                float power = 90.0f / (float)UpdatePerSecond;
                //power = Mathf.Pow(power, 0.3f); // 調整
                return power;
            }
        }

        /// <summary>
        /// タイムスケール
        /// 1.0未満に設定することで全体のスロー再生が可能
        /// ただし完全なスローではないので注意
        /// </summary>
        public float TimeScale
        {
            get
            {
                return timeScale;
            }
            set
            {
                timeScale = Mathf.Clamp01(value);
            }
        }
    }
}
