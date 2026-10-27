using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// An entity confirm could talk to: where it stands and faces, how near the player must be, and how wide around its
    /// facing the player may stand, 360 for anywhere.
    /// </summary>
    internal readonly struct InteractionCandidate
    {
        internal readonly int     EntityId;
        internal readonly Vector3 Position;
        internal readonly Vector3 Facing;
        internal readonly float   Range;
        internal readonly float   Angle;

        internal InteractionCandidate(int entityId, Vector3 position, Vector3 facing, float range, float angle)
        {
            EntityId = entityId;
            Position = position;
            Facing   = facing;
            Range    = range;
            Angle    = angle;
        }
    }
}
