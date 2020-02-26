// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// MagicaCapsuleColliderのギズモ表示
    /// </summary>
    public class MagicaCapsuleColliderGizmoDrawer
    {
        //[DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.InSelectionHierarchy | GizmoType.Active)]
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active)]
        static void DrawGizmo(MagicaCapsuleCollider scr, GizmoType gizmoType)
        {
            bool selected = (gizmoType & GizmoType.Selected) != 0 || (ClothMonitorMenu.Monitor != null && ClothMonitorMenu.Monitor.UI.AlwaysClothShow);

            DrawGizmo(scr, selected);
        }

        public static void DrawGizmo(MagicaCapsuleCollider scr, bool selected)
        {
            Gizmos.color = selected ? GizmoUtility.ColorCollider : GizmoUtility.ColorNonSelectedCollider;
            GizmoUtility.DrawWireCapsule(
                scr.transform.position,
                scr.transform.rotation,
                Vector3.one, // scr.transform.lossyScale, 現在スケールは見ていない
                scr.GetLocalDir(),
                scr.GetLocalUp(),
                scr.Length,
                scr.StartRadius,
                scr.EndRadius
                );
        }
    }
}
