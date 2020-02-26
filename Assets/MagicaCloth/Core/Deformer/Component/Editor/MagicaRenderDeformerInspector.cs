// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// レンダーデフォーマーのエディタ拡張
    /// </summary>
    [CustomEditor(typeof(MagicaRenderDeformer))]
    public class MagicaRenderDeformerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            MagicaRenderDeformer scr = target as MagicaRenderDeformer;

            serializedObject.Update();

            // データ検証
            if (EditorApplication.isPlaying == false)
                VerifyData();

            // 自動データ作成判定
            if (scr.DataReset)
            {
                serializedObject.FindProperty("dataReset").boolValue = false;
                serializedObject.ApplyModifiedProperties();

                Undo.RecordObject(scr, "CreateRenderMesh");
                CreateData(scr);
                serializedObject.ApplyModifiedProperties();
            }

            // データ状態
            EditorInspectorUtility.DispDataStatus(scr);

            Undo.RecordObject(scr, "CreateRenderMesh");

            // モニターボタン
            EditorInspectorUtility.MonitorButtonInspector();

            DrawRenderDeformerInspector();

            // データ作成
            if (EditorApplication.isPlaying == false)
            {
                EditorGUILayout.Space();
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Create"))
                {
                    Undo.RecordObject(scr, "CreateRenderMeshData");
                    CreateData(scr);
                }
                GUI.backgroundColor = Color.white;
                serializedObject.ApplyModifiedProperties();
            }
        }

        void DrawRenderDeformerInspector()
        {
            MagicaRenderDeformer scr = target as MagicaRenderDeformer;

            serializedObject.Update();

            EditorGUILayout.LabelField("Update Mode", EditorStyles.boldLabel);

            var property1 = serializedObject.FindProperty("deformer.normalAndTangentUpdateMode");
            var value1 = property1.boolValue;

            EditorGUILayout.PropertyField(property1);

            serializedObject.ApplyModifiedProperties();

            if (property1.boolValue != value1)
                scr.Deformer.IsChangeNormalTangent = true; // 再計算
        }

        //=========================================================================================
        /// <summary>
        /// データ検証
        /// </summary>
        private void VerifyData()
        {
            MagicaRenderDeformer scr = target as MagicaRenderDeformer;
            if (scr.VerifyData() != Define.Error.None)
            {
                // 検証エラー
                serializedObject.ApplyModifiedProperties();
            }
        }

        //=========================================================================================
        /// <summary>
        /// 事前データ作成
        /// </summary>
        public void CreateData(MagicaRenderDeformer scr)
        {
            Debug.Log("Started creating. [" + scr.name + "]");

            // ターゲットオブジェクト
            serializedObject.FindProperty("deformer.targetObject").objectReferenceValue = scr.gameObject;
            serializedObject.FindProperty("deformer.dataHash").intValue = 0;

            // 共有データ作成
            var meshData = ShareDataObject.CreateShareData<MeshData>("RenderMeshData_" + scr.name);

            // renderer
            var ren = scr.GetComponent<Renderer>();
            if (ren == null)
            {
                Debug.LogError("Creation failed. Renderer not found.");
                return;
            }

            Mesh sharedMesh = null;
            if (ren is SkinnedMeshRenderer)
            {
                meshData.isSkinning = true;
                var sren = ren as SkinnedMeshRenderer;
                sharedMesh = sren.sharedMesh;
            }
            else
            {
                meshData.isSkinning = false;
                var meshFilter = ren.GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    Debug.LogError("Creation failed. MeshFilter not found.");
                    return;
                }
                sharedMesh = meshFilter.sharedMesh;
            }

            // 頂点
            meshData.vertexCount = sharedMesh.vertexCount;

            // 頂点ハッシュ
            var vlist = sharedMesh.vertices;
            List<ulong> vertexHashList = new List<ulong>();
            for (int i = 0; i < vlist.Length; i++)
            {
                var vhash = DataHashExtensions.GetVectorDataHash(vlist[i]);
                //Debug.Log("[" + i + "] (" + (vlist[i] * 1000) + ") :" + vhash);
                vertexHashList.Add(vhash);
            }
            meshData.vertexHashList = vertexHashList.ToArray();

            // トライアングル
            meshData.triangleCount = sharedMesh.triangles.Length / 3;

            // レンダーデフォーマーのメッシュデータにはローカル座標、法線、接線、UV、トライアングルリストは保存しない
            // 不要なため

            // ボーン
            //List<Transform> useBones = new List<Transform>();
            //if (meshData.isSkinning)
            //{
            //    var sren = ren as SkinnedMeshRenderer;
            //    useBones = new List<Transform>(sren.bones);
            //    useBones.Add(ren.transform); // 最後にレンダラーのトランスフォームを追加する
            //}
            //else
            //{
            //    // 通常メッシュではレンダラーのトランスフォームをボーン[0]に設定する
            //    useBones.Add(ren.transform);
            //}
            int boneCount = meshData.isSkinning ? sharedMesh.bindposes.Length : 1;
            meshData.boneCount = boneCount;

            //AssetDatabase.StartAssetEditing();

            // メッシュデータの検証とハッシュ
            meshData.CreateVerifyData();

            serializedObject.FindProperty("deformer.sharedMesh").objectReferenceValue = sharedMesh;
            serializedObject.FindProperty("deformer.meshData").objectReferenceValue = meshData;
            serializedObject.FindProperty("deformer.meshOptimize").intValue = EditUtility.GetOptimizeMesh(sharedMesh);
            serializedObject.ApplyModifiedProperties();

            // デフォーマーデータの検証とハッシュ
            scr.Deformer.CreateVerifyData();
            serializedObject.ApplyModifiedProperties();

            // コアコンポーネントの検証とハッシュ
            scr.CreateVerifyData();
            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(meshData);

            //AssetDatabase.StopAssetEditing();

            // 変更後数
            Debug.Log("Creation completed. [" + scr.name + "]");
        }
    }
}