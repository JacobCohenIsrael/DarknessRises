using System;
using UnityEngine;

namespace DarknessRises.Game.Waves
{
    /// <summary>
    /// A Room's Spawn points: the authored places where Enemies enter it, such as dark doorways, gaps and vents.
    /// Each must sit on the Room's NavMesh.
    /// </summary>
    public sealed class RoomSpawnPoints : MonoBehaviour
    {
        private const float GizmoRadius = 0.5f;
        private const float GizmoArrowLength = 1f;

        [SerializeField, Tooltip("Where Enemies enter this Room. Each faces into the Room.")]
        private Transform[] points = Array.Empty<Transform>();

        public int Count => points.Length;

        public Vector3 PositionOf(int index) => points[index].position;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.8f, 0.2f, 0.9f);
            foreach (var point in points)
            {
                if (point == null)
                {
                    continue;
                }

                Gizmos.DrawWireSphere(point.position, GizmoRadius);
                Gizmos.DrawLine(point.position, point.position + point.forward * GizmoArrowLength);
            }
        }
    }
}
