using Unity.Mathematics;

namespace RPGFramework.Field.Utils
{
    internal static class MathUtils
    {
        internal static float LerpAngle(float a, float b, float t)
        {
            t = math.clamp(t, 0f, 1f);

            float delta = math.degrees(math.atan2(math.sin(math.radians(b - a)), math.cos(math.radians(b - a))));

            return a + delta * t;
        }
    }
}