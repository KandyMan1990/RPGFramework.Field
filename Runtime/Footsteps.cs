using Unity.Mathematics;

namespace RPGFramework.Field
{
    /// <summary>When a foot has landed, and how a step sounds from where it is on the screen.</summary>
    internal static class Footsteps
    {
        // A step at the screen's edge is panned this far; the rest of the way is off the screen.
        private const float SCREEN_EDGE_PAN = 0.3125f;

        private const float NEAREST_VOLUME  = 1f;
        private const float FARTHEST_VOLUME = 0.13f;

        /// <summary>
        /// Whether a cycle going forward, 0 to 1 and round again, has reached a point since it was last looked at. Run
        /// it the other way round for a cycle going backward.
        /// </summary>
        internal static bool Crossed(float previous, float current, float point)
        {
            if (previous <= current)
            {
                return previous < point && point <= current;
            }

            return previous < point || point <= current;
        }

        /// <param name="viewportX">Where the entity is across the screen, 0 the left edge and 1 the right.</param>
        internal static float Pan(float viewportX)
        {
            return math.clamp((viewportX - 0.5f) * 2f * SCREEN_EDGE_PAN, -1f, 1f);
        }

        /// <param name="distance">How far the entity is from the camera.</param>
        /// <param name="farDistance">The distance at which a step is at its quietest; 0 or less for none.</param>
        internal static float Volume(float distance, float farDistance)
        {
            return farDistance > 0f ? math.lerp(NEAREST_VOLUME, FARTHEST_VOLUME, math.saturate(distance / farDistance)) : NEAREST_VOLUME;
        }
    }
}
