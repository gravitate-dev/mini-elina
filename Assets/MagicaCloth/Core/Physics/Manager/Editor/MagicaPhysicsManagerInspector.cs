// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using UnityEditor;

namespace MagicaCloth
{
    /// <summary>
    /// 物理マネージャのエディタ拡張
    /// </summary>
    [CustomEditor(typeof(MagicaPhysicsManager))]
    public class MagicaPhysicsManagerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            //DrawDefaultInspector();

            MagicaPhysicsManager scr = target as MagicaPhysicsManager;

            serializedObject.Update();
            Undo.RecordObject(scr, "PhysicsManager");

            MainInspector();

            serializedObject.ApplyModifiedProperties();
        }

        void MainInspector()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Update", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                var prop = serializedObject.FindProperty("updateTime.updatePerSeccond");
                EditorGUILayout.PropertyField(prop);

                var prop2 = serializedObject.FindProperty("updateTime.updateMode");
                EditorGUILayout.PropertyField(prop2);

                //var prop2 = serializedObject.FindProperty("updateTime.timeScale");
                //EditorGUILayout.PropertyField(prop2);
            }
        }
    }
}