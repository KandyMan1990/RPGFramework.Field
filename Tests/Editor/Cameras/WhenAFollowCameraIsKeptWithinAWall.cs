using NUnit.Framework;

namespace RPGFramework.Field.Tests.Cameras
{
    /// <summary>Bounds that are not a trigger are also something solid the characters walk into.</summary>
    internal sealed class WhenAFollowCameraIsKeptWithinAWall
    {
        private CameraField m_Field2D;
        private CameraField m_Field3D;

        [SetUp]
        public void SetUp()
        {
            m_Field2D = new CameraField();
            m_Field2D.AddFollowCamera2D(false);

            m_Field3D = new CameraField();
            m_Field3D.AddFollowCamera3D(false);
        }

        [TearDown]
        public void TearDown()
        {
            m_Field2D.Destroy();
            m_Field3D.Destroy();
        }

        [Test]
        public void ThenExportRefuses2D()
        {
            Assert.AreEqual(1, m_Field2D.Export().Count);
        }

        [Test]
        public void ThenExportRefuses3D()
        {
            Assert.AreEqual(1, m_Field3D.Export().Count);
        }
    }
}
