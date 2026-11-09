using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Heads
{
    /// <summary>A head at the origin, on a body facing along z unless a test turns it, sent somewhere at once.</summary>
    internal sealed class WhenAHeadIsAimed
    {
        private const float TOLERANCE = 0.01f;

        [Test]
        public void ThenStraightAheadIsNoTurn()
        {
            Assert.AreEqual(0f, Aimed(new Vector3(0f, 0f, 5f), Quaternion.identity).Angles.x, TOLERANCE);
        }

        [Test]
        public void ThenToTheRightIsAPositiveYaw()
        {
            Assert.AreEqual(45f, Aimed(new Vector3(5f, 0f, 5f), Quaternion.identity).Angles.x, TOLERANCE);
        }

        [Test]
        public void ThenAboveIsAPositivePitch()
        {
            Assert.AreEqual(30f, Aimed(new Vector3(0f, Mathf.Tan(30f * Mathf.Deg2Rad), 1f), Quaternion.identity).Angles.y, TOLERANCE);
        }

        [Test]
        public void ThenAYawIsCountedFromTheWayTheBodyFaces()
        {
            HeadLook head = Aimed(new Vector3(5f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));

            Assert.AreEqual(0f, head.Angles.x, TOLERANCE);
        }

        [Test]
        public void ThenWithNoLimitsSetAYawStopsAtNinety()
        {
            Assert.AreEqual(90f, Aimed(new Vector3(1f, 0f, -5f), Quaternion.identity).Angles.x, TOLERANCE);
        }

        [Test]
        public void ThenWithNoLimitsSetAPitchStopsAtFortyFive()
        {
            Assert.AreEqual(45f, Aimed(new Vector3(0f, 5f, 1f), Quaternion.identity).Angles.y, TOLERANCE);
        }

        [Test]
        public void ThenALimitAScriptSetIsKeptTo()
        {
            HeadLook head = default;

            head.SetLimits(20f, 10f);
            Send(ref head, new Vector3(5f, 5f, 5f), Quaternion.identity);

            Assert.AreEqual(20f, head.Angles.x, TOLERANCE);
            Assert.AreEqual(10f, head.Angles.y, TOLERANCE);
        }

        [Test]
        public void ThenALimitAppliesToTheOtherSideToo()
        {
            HeadLook head = default;

            head.SetLimits(20f, 10f);
            Send(ref head, new Vector3(-5f, -5f, 5f), Quaternion.identity);

            Assert.AreEqual(-20f, head.Angles.x, TOLERANCE);
            Assert.AreEqual(-10f, head.Angles.y, TOLERANCE);
        }

        private static HeadLook Aimed(Vector3 point, Quaternion body)
        {
            HeadLook head = default;

            Send(ref head, point, body);

            return head;
        }

        private static void Send(ref HeadLook head, Vector3 point, Quaternion body)
        {
            head.LookAt(point, FieldEntity.NO_ENTITY, 0f);
            head.Aim(body, Vector3.zero, point);
            head.Tick(0.01f);
        }
    }
}
