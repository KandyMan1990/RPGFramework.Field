using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Shaking
{
    /// <summary>An offset of a tenth of the width and height of a 16:9 screen, so 0.2 of the view's -1 to 1.</summary>
    internal sealed class WhenAShakeMovesThePicture
    {
        private const float TOLERANCE = 0.0001f;
        private const float ASPECT    = 16f / 9f;

        private static readonly Vector2 m_Offset = new Vector2(192f, 108f);

        [Test]
        public void ThenAPerspectivePictureMovesTheSameAtAnyDepth()
        {
            Matrix4x4 projection = Matrix4x4.Perspective(60f, ASPECT, 0.1f, 100f);

            AssertMoved(projection, new Vector3(1f, 1f, -5f));
            AssertMoved(projection, new Vector3(-3f, 2f, -50f));
        }

        [Test]
        public void ThenAnOrthographicPictureMovesTheSame()
        {
            AssertMoved(Matrix4x4.Ortho(-8f, 8f, -4.5f, 4.5f, 0.1f, 100f), new Vector3(1f, 1f, -5f));
        }

        private static void AssertMoved(Matrix4x4 projection, Vector3 point)
        {
            Vector3 before = projection.MultiplyPoint(point);
            Vector3 after  = ScreenShake.Shifted(projection, m_Offset, ASPECT).MultiplyPoint(point);

            Assert.AreEqual(0.2f, after.x - before.x, TOLERANCE);
            Assert.AreEqual(0.2f, after.y - before.y, TOLERANCE);
            Assert.AreEqual(0f, after.z - before.z, TOLERANCE);
        }
    }
}
