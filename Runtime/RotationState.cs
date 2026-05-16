using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field
{
    internal struct RotationState
    {
        internal bool                  Active;
        internal Quaternion            Start;
        internal Quaternion            Target;
        internal float                 Elapsed;
        internal float                 Duration;
        internal RotationInterpolation Interpolation;
        internal RotationDirection     Direction;
    }
}