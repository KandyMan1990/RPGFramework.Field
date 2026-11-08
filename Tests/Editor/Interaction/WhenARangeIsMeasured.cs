using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Interaction
{
    /// <summary>From the origin of a 3D field whose height limit is 2, with a range of 2.</summary>
    internal sealed class WhenARangeIsMeasured
    {
        private const float RANGE  = 2f;
        private const float HEIGHT = 2f;

        [Test]
        public void ThenWithinItIsInRange()
        {
            Assert.IsTrue(IsInRange(new Vector3(1f, 0f, 1f)));
        }

        [Test]
        public void ThenBeyondItIsOutOfRange()
        {
            Assert.IsFalse(IsInRange(new Vector3(0f, 0f, 3f)));
        }

        [Test]
        public void ThenTheVerySameSpotIsInRange()
        {
            Assert.IsTrue(IsInRange(Vector3.zero));
        }

        [Test]
        public void ThenAboveTheHeightLimitIsOutOfRange()
        {
            Assert.IsFalse(IsInRange(new Vector3(0f, 3f, 0f)));
        }

        [Test]
        public void ThenHeightDoesNotAddToTheDistance()
        {
            Assert.IsTrue(IsInRange(new Vector3(0f, 1.5f, 1.5f)));
        }

        private static bool IsInRange(Vector3 to)
        {
            return new InteractionSearch(Vector3.up, 180f, HEIGHT).IsInRange(Vector3.zero, to, RANGE);
        }
    }
}
