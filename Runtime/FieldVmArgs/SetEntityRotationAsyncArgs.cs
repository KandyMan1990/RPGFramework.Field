using UnityEngine;

namespace RPGFramework.Field.FieldVmArgs
{
    internal enum RotationDirection
    {
        Clockwise,
        CounterClockwise,
        Closest
    }

    internal readonly struct SetEntityRotationAsyncArgs
    {
        internal readonly Quaternion        Rotation;
        internal readonly RotationDirection RotationDirection;
        internal readonly float             Duration;
        internal readonly Easing            RotationType;

        internal SetEntityRotationAsyncArgs(Quaternion rotation, RotationDirection rotationDirection, float duration, Easing rotationType)
        {
            Rotation          = rotation;
            RotationDirection = rotationDirection;
            Duration          = duration;
            RotationType      = rotationType;
        }
    }
}
