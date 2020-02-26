// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// ボーンクロスのターゲットトランスフォーム
    /// </summary>
    [System.Serializable]
    public class BoneClothTarget : IDataHash
    {
        //　ルートトランスフォーム
        [SerializeField]
        private List<Transform> rootList = new List<Transform>();

        // [Header("トランスフォームのスケールを更新するかどうか(高負荷)")]
        // [SerializeField]
        // private bool readTransformScale = false;

        //　各ボーンはアニメーション制御されるか?
        //[SerializeField]
        //private bool isAnimationBone = false;
        //[SerializeField]
        //private bool isAnimationRotation = false;
        //[SerializeField]
        //private bool isAnimationPosition = false;

        //=========================================================================================
        /// <summary>
        /// データを識別するハッシュコードを作成して返す
        /// </summary>
        /// <returns></returns>
        public int GetDataHash()
        {
            int hash = 0;
            hash += rootList.GetDataHash();
            return hash;
        }

        //=========================================================================================
        /// <summary>
        /// ルートトランスフォームの数
        /// </summary>
        public int RootCount
        {
            get
            {
                return rootList.Count;
            }
        }

        /// <summary>
        /// ルートトランスフォーム取得
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Transform GetRoot(int index)
        {
            if (index < rootList.Count)
                return rootList[index];

            return null;
        }

        /// <summary>
        /// ルートトランスフォームのインデックスを返す。無い場合は(-1)
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public int GetRootIndex(Transform root)
        {
            return rootList.IndexOf(root);
        }

        /// <summary>
        /// ボーンがアニメーション制御されるかどうかの判定フラグ
        /// </summary>
        //public bool IsAnimationBone
        //{
        //    get
        //    {
        //        return isAnimationBone;
        //    }
        //    set
        //    {
        //        isAnimationBone = value;
        //    }
        //}

        /// <summary>
        /// ボーンがアニメーション制御されるかどうかの判定フラグ
        /// </summary>
        //public bool IsAnimationRotation
        //{
        //    get
        //    {
        //        return isAnimationRotation;
        //    }
        //    set
        //    {
        //        isAnimationRotation = value;
        //    }
        //}
        //public bool IsAnimationPosition
        //{
        //    get
        //    {
        //        return isAnimationPosition;
        //    }
        //    set
        //    {
        //        isAnimationPosition = value;
        //    }
        //}

    }
}
