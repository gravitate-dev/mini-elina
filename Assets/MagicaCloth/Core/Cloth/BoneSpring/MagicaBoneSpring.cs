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
    /// ボーンスプリング
    /// </summary>
    [HelpURL("https://magicasoft.jp/magica-cloth-bone-spring/")]
    [AddComponentMenu("MagicaCloth/MagicaBoneSpring")]
    public class MagicaBoneSpring : BaseCloth
    {
        /// <summary>
        /// データバージョン
        /// </summary>
        private const int DATA_VERSION = 2;

        /// <summary>
        /// メッシュデータ
        /// </summary>
        [SerializeField]
        private MeshData meshData = null;

        [SerializeField]
        private int meshDataHash;
        [SerializeField]
        private int meshDataVersion;

        /// <summary>
        /// 使用ルートトランスフォーム情報
        /// </summary>
        [SerializeField]
        private BoneClothTarget clothTarget = new BoneClothTarget();

        /// <summary>
        /// 最終的に使用されるすべてのトランスフォーム情報
        /// </summary>
        [SerializeField]
        private List<Transform> useTransformList = new List<Transform>();
        [SerializeField]
        private List<Vector3> useTransformPositionList = new List<Vector3>();
        [SerializeField]
        private List<Quaternion> useTransformRotationList = new List<Quaternion>();
        [SerializeField]
        private List<Vector3> useTransformScaleList = new List<Vector3>();

        //=========================================================================================
        /// <summary>
        /// データハッシュを求める
        /// </summary>
        /// <returns></returns>
        public override int GetDataHash()
        {
            int hash = base.GetDataHash();
            hash += MeshData.GetDataHash();
            hash += clothTarget.GetDataHash();
            hash += useTransformList.GetDataHash();
            hash += useTransformPositionList.GetDataHash();
            hash += useTransformRotationList.GetDataHash();
            hash += useTransformScaleList.GetDataHash();
            return hash;
        }

        //=========================================================================================
        public BoneClothTarget ClothTarget
        {
            get
            {
                return clothTarget;
            }
        }

        public MeshData MeshData
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    return meshData;
                else
                {
                    // unity2019.3で参照がnullとなる不具合の対処（臨時）
                    var so = new SerializedObject(this);
                    return so.FindProperty("meshData").objectReferenceValue as MeshData;
                }
#else
                return meshData;
#endif
            }
        }

        //=========================================================================================
        protected override void Reset()
        {
            base.Reset();
            ResetParams();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
        }


        /// <summary>
        /// クロス初期化
        /// </summary>
        protected override void ClothInit()
        {
            // 初期化前に使用トランスフォーム姿勢をリセットする
            //for (int i = 0; i < useTransformList.Count; i++)
            //{
            //    var t = useTransformList[i];
            //    t.localPosition = useTransformPositionList[i];
            //    t.localRotation = useTransformRotationList[i];
            //    t.localScale = useTransformScaleList[i];
            //}

            base.ClothInit();
        }

        /// <summary>
        /// 頂点ごとのパーティクルフラグ設定
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected override uint UserFlag(int index)
        {
            uint flag = 0;

            bool isFix = ClothData.IsFixedVertex(index);

            //flag |= clothTarget.IsAnimationBone ? 0 : PhysicsManagerParticleData.Flag_Transform_Restore; // 実行前にlocalPos/localRot復元
            flag |= PhysicsManagerParticleData.Flag_Transform_Restore; // 実行前にlocalPos/localRot復元
            //flag |= clothTarget.IsAnimationPosition ? 0 : PhysicsManagerParticleData.Flag_Transform_Restore_Pos; // 実行前にlocalPos復元
            //flag |= clothTarget.IsAnimationRotation ? 0 : PhysicsManagerParticleData.Flag_Transform_Restore_Rot; // 実行前にlocalRot復元
            flag |= isFix ? (PhysicsManagerParticleData.Flag_Transform_Read_Pos | PhysicsManagerParticleData.Flag_Transform_Read_Rot) : 0; // トランスフォームをpos/rotに読み込み（固定のみ）
            flag |= PhysicsManagerParticleData.Flag_Transform_Read_Base; // トランスフォームをbasePos/baseRotに読み込み
            flag |= PhysicsManagerParticleData.Flag_Transform_Write; // 最後にトランスフォームへ座標書き込み

            return flag;
        }

        /// <summary>
        /// 頂点ごとの連動トランスフォーム設定
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected override Transform UserTransform(int index)
        {
            return GetUseTransform(index);
        }

        /// <summary>
        /// 頂点ごとの連動トランスフォームのLocalPositionを返す（不要な場合は0）
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected override float3 UserTransformLocalPosition(int vindex)
        {
            int index = ClothData.useVertexList[vindex];
            return useTransformPositionList[index];
        }

        /// <summary>
        /// 頂点ごとの連動トランスフォームのLocalRotationを返す（不要な場合はquaternion.identity)
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected override quaternion UserTransformLocalRotation(int vindex)
        {
            int index = ClothData.useVertexList[vindex];
            return useTransformRotationList[index];
        }

        /// <summary>
        /// デフォーマーの数を返す
        /// </summary>
        /// <returns></returns>
        public override int GetDeformerCount()
        {
            // BoneClothには不要
            return 0;
        }

        /// <summary>
        /// デフォーマーを返す
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override BaseMeshDeformer GetDeformer(int index)
        {
            // BoneClothには不要
            return null;
        }

        /// <summary>
        /// クロス初期化時に必要なMeshDataを返す（不要ならnull）
        /// </summary>
        /// <returns></returns>
        protected override MeshData GetMeshData()
        {
            return MeshData;
        }

        /// <summary>
        /// クロス初期化の主にワーカーへの登録
        /// </summary>
        protected override void WorkerInit()
        {
            // BoneClothには不要
        }

        /// <summary>
        /// デフォーマーごとの使用頂点設定
        /// 使用頂点に対して AddUseVertex() / RemoveUseVertex() を実行する
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="deformer"></param>
        /// <param name="deformerIndex"></param>
        protected override void SetDeformerUseVertex(bool sw, BaseMeshDeformer deformer, int deformerIndex)
        {
            // BoneClothには不要
        }

        //=========================================================================================
        /// <summary>
        /// 使用するトランスフォームをリストにして返す
        /// </summary>
        /// <returns></returns>
        public List<Transform> GetTransformList()
        {
            List<Transform> transformList = new List<Transform>();
            int cnt = clothTarget.RootCount;
            for (int i = 0; i < cnt; i++)
            {
                var root = clothTarget.GetRoot(i);
                if (root != null)
                {
                    transformList.Add(root);
                }
            }
            return transformList;
        }

        Transform GetUseTransform(int index)
        {
            int vindex = ClothData.useVertexList[index];
            return useTransformList[vindex];
        }

        int UseTransformCount
        {
            get
            {
                return useTransformList.Count;
            }
        }

        //=========================================================================================
        public override int GetVersion()
        {
            return DATA_VERSION;
        }

        /// <summary>
        /// データを検証して結果を格納する
        /// </summary>
        /// <returns></returns>
        public override void CreateVerifyData()
        {
            base.CreateVerifyData();
            meshDataHash = MeshData.SaveDataHash;
            meshDataVersion = MeshData.SaveDataVersion;
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

            var mdata = MeshData;
            if (mdata == null)
                return Define.Error.MeshDataNull;
            var mdataError = mdata.VerifyData();
            if (mdataError != Define.Error.None)
                return mdataError;
            if (meshDataHash != mdata.SaveDataHash)
                return Define.Error.MeshDataHashMismatch;
            if (meshDataVersion != mdata.SaveDataVersion)
                return Define.Error.MeshDataVersionMismatch;

            if (useTransformList.Count == 0)
                return Define.Error.UseTransformCountZero;
            if (UseTransformCount != mdata.VertexCount)
                return Define.Error.UseTransformCountMismatch;
            if (clothTarget.RootCount != mdata.VertexCount)
                return Define.Error.ClothTargetRootCountMismatch;
            if (useTransformPositionList.Count != useTransformList.Count)
                return Define.Error.UseTransformCountMismatch;
            if (useTransformRotationList.Count != useTransformList.Count)
                return Define.Error.UseTransformCountMismatch;
            if (useTransformScaleList.Count != useTransformList.Count)
                return Define.Error.UseTransformCountMismatch;

            foreach (var t in useTransformList)
                if (t == null)
                    return Define.Error.UseTransformNull;

            return Define.Error.None;
        }

        /// <summary>
        /// データ検証の結果テキストを取得する
        /// </summary>
        /// <returns></returns>
        public override string GetInformation()
        {
            StaticStringBuilder.Clear();

            var err = VerifyData();
            if (err == Define.Error.None)
            {
                // OK
                var cdata = ClothData;
                StaticStringBuilder.AppendLine("Transform: ", MeshData.VertexCount);
                StaticStringBuilder.AppendLine("Clamp Position: ", clothParams.UseClampPositionLength ? cdata.VertexUseCount : 0);
                StaticStringBuilder.AppendLine("Spring: ", clothParams.UseSpring ? cdata.VertexUseCount : 0);
                StaticStringBuilder.Append("Adjust Rotation: ", clothParams.UseAdjustRotation ? cdata.VertexUseCount : 0);
            }
            else
            {
                // エラー
                StaticStringBuilder.AppendLine("This bone spring is in a state error!");
                if (Application.isPlaying)
                {
                    StaticStringBuilder.AppendLine("Execution stopped.");
                }
                else
                {
                    StaticStringBuilder.AppendLine("Please recreate the bone spring data.");
                }
                StaticStringBuilder.Append(Define.GetErrorMessage(err));
            }

            return StaticStringBuilder.ToString();
        }

        //=========================================================================================
        /// <summary>
        /// メッシュのワールド座標/法線/接線を返す（エディタ用）
        /// </summary>
        /// <param name="wposList"></param>
        /// <param name="wnorList"></param>
        /// <param name="wtanList"></param>
        /// <returns>頂点数</returns>
        public override int GetEditorPositionNormalTangent(out List<Vector3> wposList, out List<Vector3> wnorList, out List<Vector3> wtanList)
        {
            wposList = new List<Vector3>();
            wnorList = new List<Vector3>();
            wtanList = new List<Vector3>();

            var tlist = GetTransformList();
            foreach (var t in tlist)
            {
                wposList.Add(t.position);
                wnorList.Add(t.TransformDirection(Vector3.forward));
                var up = t.TransformDirection(Vector3.up);
                wtanList.Add(up);
            }

            return wposList.Count;
        }

        /// <summary>
        /// メッシュのトライアングルリストを返す（エディタ用）
        /// </summary>
        /// <returns></returns>
        public override List<int> GetEditorTriangleList()
        {
            List<int> triangles = new List<int>();
            var mdata = MeshData;
            if (mdata != null && mdata.triangleList != null)
                triangles = new List<int>(mdata.triangleList);
            return triangles;
        }

        /// <summary>
        /// メッシュのラインリストを返す（エディタ用）
        /// </summary>
        /// <returns></returns>
        public override List<int> GetEditorLineList()
        {
            List<int> lines = new List<int>();
            var mdata = MeshData;
            if (mdata != null && mdata.lineList != null)
                lines = new List<int>(mdata.lineList);
            return lines;
        }

        //=========================================================================================
        /// <summary>
        /// 頂点の選択状態をリストにして返す（エディタ用）
        /// 選択状態は ClothSelection.Invalid / ClothSelection.Fixed / ClothSelection.Move
        /// すべてがInvalidならばnullを返す
        /// </summary>
        /// <returns></returns>
        public override List<int> GetSelectionList()
        {
            if (ClothSelection != null && MeshData != null)
                return ClothSelection.GetSelectionData(MeshData, null);
            else
                return null;
        }

        /// <summary>
        /// 頂点の使用状態をリストにして返す（エディタ用）
        /// 数値が１以上ならば使用中とみなす
        /// すべて使用状態ならばnullを返す
        /// </summary>
        /// <returns></returns>
        public override List<int> GetUseList()
        {
            if (Application.isPlaying)
            {
                if (ClothData != null)
                {
                    var useList = new List<int>();
                    foreach (var sel in ClothData.selectionData)
                        useList.Add(sel != SelectionData.Invalid ? 1 : 0);
                    return useList;
                }
            }

            return null;
        }

        //=========================================================================================
        /// <summary>
        /// 共有データオブジェクト収集
        /// </summary>
        /// <returns></returns>
        public override List<ShareDataObject> GetAllShareDataObject()
        {
            var sdata = base.GetAllShareDataObject();
            sdata.Add(MeshData);
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
            var sdata = base.DuplicateShareDataObject(source);
            if (sdata != null)
                return sdata;

            if (MeshData == source)
            {
                //meshData = Instantiate(MeshData);
                meshData = ShareDataObject.Clone(MeshData);
                return meshData;
            }

            return null;
        }

        //=========================================================================================
        /// <summary>
        /// パラメータ初期化
        /// </summary>
        void ResetParams()
        {
            clothParams.SetRadius(0.02f, 0.02f);
            clothParams.SetMass(1.0f, 1.0f, false);
            clothParams.SetGravity(false, -9.8f, -9.8f);
            clothParams.SetDrag(true, 0.03f, 0.03f);
            clothParams.SetMaxVelocity(true, 3.0f, 3.0f);
            clothParams.SetWorldInfluence(0.5f, 0.5f);
            clothParams.SetTeleport(true);
            clothParams.SetClampDistanceRatio(false);
            clothParams.SetClampPositionLength(true, 0.2f, 0.2f, 1.0f, 1.0f, 1.0f, 1.0f);
            clothParams.SetClampRotationAngle(false);
            clothParams.SetRestoreDistance(1.0f);
            clothParams.SetRestoreRotation(false);
            clothParams.SetSpring(true, 0.02f, 0.1f, 1.0f, 1.0f, 1.0f, 1.0f);
            clothParams.SetAdjustRotation(false, ClothParams.AdjustMode.None, 3.0f);
            clothParams.SetTriangleBend(false);
            clothParams.SetVolume(false);
            clothParams.SetCollision(false, 0.2f);
        }
    }
}
