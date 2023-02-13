using Components.Markers;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(PlayerSpawnMarker))]
    public class PlayerSpawnMarkerEditor : UnityEditor.Editor
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Pickable | GizmoType.NonSelected)]
        public static void RenderCustomGizmo(PlayerSpawnMarker spawnMarker, GizmoType gizmo)
        {
            Gizmos.color = Color.red;

            var markerPosition = spawnMarker.transform.position;

            foreach (var playerMesh in spawnMarker.PlayerMeshes)
            {
                Gizmos.DrawMesh(playerMesh, markerPosition);
            }
        }
    }
}