﻿// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicaCloth
{
    /// <summary>
    /// クロス基本クラス
    /// </summary>
    public abstract partial class BaseCloth : PhysicsTeam
    {
        /// <summary>
        /// パラメータ設定
        /// </summary>
        [SerializeField]
        protected ClothParams clothParams = new ClothParams();

        /// <summary>
        /// クロスデータ
        /// </summary>
        [SerializeField]
        private ClothData clothData = null;

        [SerializeField]
        protected int clothDataHash;
        [SerializeField]
        protected int clothDataVersion;

        /// <summary>
        /// 頂点選択データ
        /// </summary>
        [SerializeField]
        private SelectionData clothSelection = null;

        [SerializeField]
        private int clothSelectionHash;
        [SerializeField]
        private int clothSelectionVersion;

        /// <summary>
        /// ランタイムクロス設定
        /// </summary>
        protected ClothSetup setup = new ClothSetup();


        //=========================================================================================
        private float oldBlendRatio = 0.0f;


        //=========================================================================================
        /// <summary>
        /// データハッシュを求める
        /// </summary>
        /// <returns></returns>
        public override int GetDataHash()
        {
            int hash = base.GetDataHash();
            if (ClothData != null)
                hash += ClothData.GetDataHash();
            if (ClothSelection != null)
                hash += ClothSelection.GetDataHash();

            return hash;
        }

        //=========================================================================================
        public ClothParams Params
        {
            get
            {
                return clothParams;
            }
        }

        public ClothData ClothData
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    return clothData;
                else
                {
                    // unity2019.3で参照がnullとなる不具合の対処（臨時）
                    var so = new SerializedObject(this);
                    return so.FindProperty("clothData").objectReferenceValue as ClothData;
                }
#else
                return clothData;
#endif
            }
            set
            {
                clothData = value;
            }
        }

        public SelectionData ClothSelection
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    return clothSelection;
                else
                {
                    // unity2019.3で参照がnullとなる不具合の対処（臨時）
                    var so = new SerializedObject(this);
                    return so.FindProperty("clothSelection").objectReferenceValue as SelectionData;
                }
#else
                return clothSelection;
#endif
            }
        }

        public ClothSetup Setup
        {
            get
            {
                return setup;
            }
        }

        //=========================================================================================
        protected virtual void Reset()
        {
        }

        protected virtual void OnValidate()
        {
            if (Application.isPlaying == false)
                return;

            // クロスパラメータのラインタイム変更
            setup.ChangeData(this, clothParams);

            // ブレンド率更新
            UpdateBlend();
        }

        //=========================================================================================
        protected override void OnInit()
        {
            base.OnInit();
            BaseClothInit();
        }

        protected override void OnActive()
        {
            base.OnActive();
            // パーティクル有効化
            EnableParticle(UserTransform, UserTransformLocalPosition, UserTransformLocalRotation);
            SetUseMesh(true);
            ClothActive();
            UpdateBlend();
        }

        protected override void OnInactive()
        {
            base.OnInactive();
            // パーティクル無効化
            DisableParticle(UserTransform, UserTransformLocalPosition, UserTransformLocalRotation);
            SetUseMesh(false);
            ClothInactive();
        }

        protected override void OnDispose()
        {
            BaseClothDispose();
            base.OnDispose();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
        }

        //=========================================================================================
        void BaseClothInit()
        {
            // デフォーマー初期化
            int dcount = GetDeformerCount();
            for (int i = 0; i < dcount; i++)
            {
                var deformer = GetDeformer(i);
                if (deformer == null)
                {
                    Status.SetInitError();
                    return;
                }
                deformer.Init();
                if (deformer.Status.IsInitError)
                {
                    Status.SetInitError();
                    return;
                }
            }

            if (VerifyData() != Define.Error.None)
            {
                Status.SetInitError();
                return;
            }

            // クロス初期化
            ClothInit();

            // クロス初期化後の主にワーカーへの登録
            WorkerInit();

            // 頂点有効化
            SetUseVertex(true);
        }

        /// <summary>
        /// クロス初期化
        /// </summary>
        protected virtual void ClothInit()
        {
            setup.ClothInit(this, GetMeshData(), ClothData, clothParams, UserFlag);
        }

        protected virtual void ClothActive()
        {
            setup.ClothActive(this, clothParams);
        }

        protected virtual void ClothInactive()
        {
            setup.ClothInactive(this);
        }

        /// <summary>
        /// 頂点ごとのパーティクルフラグ設定（不要な場合は０）
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected abstract uint UserFlag(int vindex);

        /// <summary>
        /// 頂点ごとの連動トランスフォーム設定（不要な場合はnull）
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected abstract Transform UserTransform(int vindex);

        /// <summary>
        /// 頂点ごとの連動トランスフォームのLocalPositionを返す（不要な場合は0）
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected abstract float3 UserTransformLocalPosition(int vindex);

        /// <summary>
        /// 頂点ごとの連動トランスフォームのLocalRotationを返す（不要な場合はquaternion.identity)
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected abstract quaternion UserTransformLocalRotation(int vindex);

        /// <summary>
        /// デフォーマーの数を返す
        /// </summary>
        /// <returns></returns>
        public abstract int GetDeformerCount();

        /// <summary>
        /// デフォーマーを返す
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public abstract BaseMeshDeformer GetDeformer(int index);

        /// <summary>
        /// クロス初期化時に必要なMeshDataを返す（不要ならnull）
        /// </summary>
        /// <returns></returns>
        protected abstract MeshData GetMeshData();

        /// <summary>
        /// クロス初期化後の主にワーカーへの登録
        /// </summary>
        protected abstract void WorkerInit();


        //=========================================================================================
        void BaseClothDispose()
        {
            if (MagicaPhysicsManager.IsInstance() == false)
                return;

            if (Status.IsInitSuccess)
            {
                // 頂点無効化
                SetUseVertex(false);

                // クロス破棄
                // この中ですべてのコンストレイントとワーカーからチームのデータが自動削除される
                setup.ClothDispose(this);
            }
        }

        //=========================================================================================
        /// <summary>
        /// 使用デフォーマー設定
        /// </summary>
        /// <param name="sw"></param>
        void SetUseMesh(bool sw)
        {
            if (MagicaPhysicsManager.IsInstance() == false)
                return;

            if (Status.IsInitSuccess == false)
                return;

            int dcount = GetDeformerCount();
            for (int i = 0; i < dcount; i++)
            {
                var deformer = GetDeformer(i);
                if (deformer != null)
                {
                    if (sw)
                        deformer.AddUseMesh(this);
                    else
                        deformer.RemoveUseMesh(this);
                }
            }
        }

        /// <summary>
        /// 使用頂点設定
        /// </summary>
        /// <param name="sw"></param>
        void SetUseVertex(bool sw)
        {
            if (MagicaPhysicsManager.IsInstance() == false)
                return;

            int dcount = GetDeformerCount();
            for (int i = 0; i < dcount; i++)
            {
                var deformer = GetDeformer(i);
                if (deformer != null)
                {
                    SetDeformerUseVertex(sw, deformer, i);
                }
            }
        }

        /// <summary>
        /// デフォーマーごとの使用頂点設定
        /// 使用頂点に対して AddUseVertex() / RemoveUseVertex() を実行する
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="deformer"></param>
        /// <param name="deformerIndex"></param>
        protected abstract void SetDeformerUseVertex(bool sw, BaseMeshDeformer deformer, int deformerIndex);

        //=========================================================================================
        /// <summary>
        /// ブレンド率更新
        /// </summary>
        public void UpdateBlend()
        {
            if (MagicaPhysicsManager.IsInstance() == false)
                return;
            if (teamId <= 0)
                return;

            // ユーザーブレンド率
            float blend = TeamData.UserBlendRatio;

            // 距離ブレンド率
            blend *= setup.DistanceBlendRatio;

            // 変更チェック
            blend = Mathf.Clamp01(blend);
            if (blend != oldBlendRatio)
            {
                // チームデータへ反映
                MagicaPhysicsManager.Instance.Team.SetBlendRatio(teamId, blend);

                // コンポーネント有効化判定
                SetUserEnable(blend > 0.01f);

                oldBlendRatio = blend;
            }
        }

        //=========================================================================================
        /// <summary>
        /// データを検証して結果を格納する
        /// </summary>
        /// <returns></returns>
        public override void CreateVerifyData()
        {
            base.CreateVerifyData();
            clothDataHash = ClothData != null ? ClothData.SaveDataHash : 0;
            clothDataVersion = ClothData != null ? ClothData.SaveDataVersion : 0;
            clothSelectionHash = ClothSelection != null ? ClothSelection.SaveDataHash : 0;
            clothSelectionVersion = ClothSelection != null ? ClothSelection.SaveDataVersion : 0;
        }

        /// <summary>
        /// 現在のデータが正常（実行できる状態）か返す
        /// </summary>
        /// <returns></returns>
        public override Define.Error VerifyData()
        {
            var baseError = base.VerifyData();
            if (baseError != Define.Error.None)
                return baseError;

            // clothDataはオプション
            if (ClothData != null)
            {
                var clothDataError = ClothData.VerifyData();
                if (clothDataError != Define.Error.None)
                    return clothDataError;
                if (clothDataHash != ClothData.SaveDataHash)
                    return Define.Error.ClothDataHashMismatch;
                if (clothDataVersion != ClothData.SaveDataVersion)
                    return Define.Error.ClothDataVersionMismatch;
            }

            // clothSelectionはオプション
            if (ClothSelection != null)
            {
                var clothSelectionError = ClothSelection.VerifyData();
                if (clothSelectionError != Define.Error.None)
                    return clothSelectionError;
                if (clothSelectionHash != ClothSelection.SaveDataHash)
                    return Define.Error.ClothSelectionHashMismatch;
                if (clothSelectionVersion != ClothSelection.SaveDataVersion)
                    return Define.Error.ClothSelectionVersionMismatch;
            }

            return Define.Error.None;
        }

        //=========================================================================================
        /// <summary>
        /// 共有データオブジェクト収集
        /// </summary>
        /// <returns></returns>
        public override List<ShareDataObject> GetAllShareDataObject()
        {
            var sdata = base.GetAllShareDataObject();
            sdata.Add(ClothData);
            sdata.Add(ClothSelection);
            return sdata;
        }

        /// <summary>
        /// sourceの共有データを複製して再セットする
        /// 再セットした共有データを返す
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public override ShareDataObject DuplicateShareDataObject(ShareDataObject source)
        {
            if (ClothData == source)
            {
                //clothData = Instantiate(ClothData);
                clothData = ShareDataObject.Clone(ClothData);
                return clothData;
            }

            if (ClothSelection == source)
            {
                //clothSelection = Instantiate(ClothSelection);
                clothSelection = ShareDataObject.Clone(ClothSelection);
                return clothSelection;
            }

            return null;
        }

    }
}
