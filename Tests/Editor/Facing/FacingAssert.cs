using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Facing
{
    internal static class FacingAssert
    {
        private const float TOLERANCE = 0.001f;

        internal static void Faces(Vector3 expected, Vector3 actual)
        {
            Vector3 normalised = expected.normalized;

            Assert.AreEqual(normalised.x, actual.x, TOLERANCE, $"facing {actual}");
            Assert.AreEqual(normalised.y, actual.y, TOLERANCE, $"facing {actual}");
            Assert.AreEqual(normalised.z, actual.z, TOLERANCE, $"facing {actual}");
        }
    }
}
