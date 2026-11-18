using UnityEngine;

namespace DarknessRises.Game.Visibility
{
    /// <summary>
    /// Where light falls at one moment: a sphere around a point light, or the whole Room (a Lightning flash).
    /// </summary>
    public readonly struct LitSpot
    {
        private LitSpot(Vector3 center, float range, bool isWholeRoom)
        {
            Center = center;
            Range = range;
            IsWholeRoom = isWholeRoom;
        }

        public Vector3 Center { get; }
        public float Range { get; }
        public bool IsWholeRoom { get; }

        public static LitSpot Around(Vector3 center, float range)
        {
            return new LitSpot(center, range, false);
        }

        public static LitSpot WholeRoom()
        {
            return new LitSpot(default, 0f, true);
        }

        public bool Lights(Vector3 point)
        {
            return IsWholeRoom || (point - Center).sqrMagnitude <= Range * Range;
        }
    }
}
