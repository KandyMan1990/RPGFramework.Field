using NUnit.Framework;

namespace RPGFramework.Field.Tests.Cameras
{
    internal sealed class WhenAFollowCameraHasNoBounds
    {
        private CameraField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new CameraField();
            m_Field.AddFollowCamera2D(true).BoundingShape2D = null;
        }

        [TearDown]
        public void TearDown()
        {
            m_Field.Destroy();
        }

        [Test]
        public void ThenExportRefusesIt()
        {
            Assert.AreEqual(1, m_Field.Export().Count);
        }
    }
}
