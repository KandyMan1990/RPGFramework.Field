using UnityEngine;

namespace RPGFramework.Field.Tests.Interaction
{
    /// <summary>
    /// A player at the origin of a 3D field facing forward, searching as the field scene does by default — 90° either
    /// side, 2 above or below — and people placed around them, each reaching 2.
    /// </summary>
    internal static class Around
    {
        internal const int   PLAYER = 0;
        internal const float REACH  = 2f;

        internal static int Pick(params InteractionCandidate[] candidates)
        {
            return new InteractionSearch(Vector3.up, 180f, 2f).Pick(PLAYER, Vector3.zero, Vector3.forward, candidates);
        }

        /// <summary>Someone the given degrees to the player's right, so far away and so high, who talks from any side.</summary>
        internal static InteractionCandidate Person(int id, float degreesRight, float distance, float height = 0f)
        {
            return new InteractionCandidate(id, Place(degreesRight, distance, height), Vector3.back, REACH, 360f);
        }

        /// <summary>Someone straight ahead who talks only to a player within 90° of the way they face.</summary>
        internal static InteractionCandidate FacingOnly(int id, Vector3 facing)
        {
            return new InteractionCandidate(id, Place(0f, 1f, 0f), facing, REACH, 90f);
        }

        private static Vector3 Place(float degreesRight, float distance, float height)
        {
            return Quaternion.AngleAxis(degreesRight, Vector3.up) * Vector3.forward * distance + Vector3.up * height;
        }
    }
}
