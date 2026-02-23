using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field.Utils
{
    internal static class RotationUtility
    {
        internal static Quaternion AdjustDirection(Quaternion start, Quaternion target, RotationDirection direction)
        {
            if (direction == RotationDirection.Closest)
            {
                return target;
            }

            Vector3 s = start.eulerAngles;
            Vector3 t = target.eulerAngles;

            float delta = Mathf.DeltaAngle(s.y, t.y);

            if (direction == RotationDirection.Clockwise && delta > 0)
            {
                t.y -= 360f;
            }

            if (direction == RotationDirection.CounterClockwise && delta < 0)
            {
                t.y += 360f;
            }

            return Quaternion.Euler(t);
        }

        internal static float ApplyInterpolation(float t, RotationInterpolation type)
        {
            if (type == RotationInterpolation.Smooth)
            {
                return Mathf.SmoothStep(0, 1, t);
            }

            return t;
        }
    }
}