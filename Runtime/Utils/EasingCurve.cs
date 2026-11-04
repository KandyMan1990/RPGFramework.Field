using RPGFramework.Field.FieldVmArgs;
using Unity.Mathematics;

namespace RPGFramework.Field.Utils
{
    internal static class EasingCurve
    {
        /// <summary>How far along something timed is, <paramref name="t" /> of the way through its time.</summary>
        internal static float Apply(float t, Easing easing)
        {
            return easing == Easing.Smooth ? math.smoothstep(0f, 1f, t) : t;
        }
    }
}
