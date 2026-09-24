using UnityEngine;

namespace RPGFramework.Field.FieldVmArgs
{
    /// <summary>
    /// How an entity carries itself while a script moves it.
    /// </summary>
    internal enum MoveStyle
    {
        /// <summary>Turns to face the way it goes, and its base animation walks.</summary>
        Walk = 0,

        /// <summary>Turns to face the way it goes, without walking — a vehicle, something floating.</summary>
        Glide = 1,

        /// <summary>Neither turns nor walks — a box being pushed, a platform.</summary>
        Slide = 2
    }

    /// <summary>
    /// A script sending an entity somewhere: to a point, or to another entity, followed as it moves.
    /// </summary>
    internal readonly struct MoveEntityArgs
    {
        internal readonly Vector3   Target;
        internal readonly int       TargetEntityId;
        internal readonly float     StopDistance;
        internal readonly MoveStyle Style;

        internal MoveEntityArgs(Vector3 target, int targetEntityId, float stopDistance, MoveStyle style)
        {
            Target         = target;
            TargetEntityId = targetEntityId;
            StopDistance   = stopDistance;
            Style          = style;
        }

        internal bool FollowsEntity => TargetEntityId != FieldEntity.NO_ENTITY;
    }
}
