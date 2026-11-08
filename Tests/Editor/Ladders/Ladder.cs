using NUnit.Framework;
using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field.Tests.Ladders
{
    /// <summary>
    /// A ladder from height 1 to height 5, a step on at the bottom from 0 and a step off at the top to 6, climbed at 2
    /// units a second, every animation a second long.
    /// </summary>
    internal static class Ladder
    {
        internal const float SECOND = 1f;
        internal const float ON     = 1f;

        private const float SPEED     = 2f;
        private const float TOLERANCE = 0.001f;

        internal static Vector3 At(float height)
        {
            return new Vector3(0f, height, 0f);
        }

        internal static LadderClimb Up()
        {
            return new LadderClimb(At(0f), new LadderClimbArgs(true, At(1f), At(5f), At(6f)), SPEED, SECOND, SECOND, SECOND);
        }

        internal static LadderClimb Down()
        {
            return new LadderClimb(At(6f), new LadderClimbArgs(false, At(5f), At(1f), At(0f)), SPEED, SECOND, SECOND, SECOND);
        }

        internal static void AssertIsAt(float height, LadderClimb climb)
        {
            Assert.AreEqual(height, climb.Position.y, TOLERANCE, $"at {climb.Position}");
        }

        internal static void AssertShows(LadderAnimation animation, float normalisedTime, LadderClimb climb)
        {
            Assert.AreEqual(animation, climb.Pose.Animation);
            Assert.AreEqual(normalisedTime, climb.Pose.NormalisedTime, TOLERANCE);
        }
    }
}
