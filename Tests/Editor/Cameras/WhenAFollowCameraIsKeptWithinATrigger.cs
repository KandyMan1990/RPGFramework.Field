using NUnit.Framework;

namespace RPGFramework.Field.Tests.Cameras
{
    internal sealed class WhenAFollowCameraIsKeptWithinATrigger
    {
        private CameraField m_Field2D;
        private CameraField m_Field3D;

        [SetUp]
        public void SetUp()
        {
            m_Field2D = new CameraField();
            m_Field2D.AddFollowCamera2D(true);

            m_Field3D = new CameraField();
            m_Field3D.AddFollowCamera3D(true);
        }

        [TearDown]
        public void TearDown()
        {
            m_Field2D.Destroy();
            m_Field3D.Destroy();
        }

        [Test]
        public void ThenExportAccepts2D()
        {
            CollectionAssert.IsEmpty(m_Field2D.Export());
        }

        [Test]
        public void ThenExportAccepts3D()
        {
            CollectionAssert.IsEmpty(m_Field3D.Export());
        }
    }
}
