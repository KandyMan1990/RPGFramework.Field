using NUnit.Framework;

namespace RPGFramework.Field.Tests.Cameras
{
    internal sealed class WhenAFieldHasOneFixedCamera
    {
        private CameraField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new CameraField();
            m_Field.AddCamera("Fixed Camera");
        }

        [TearDown]
        public void TearDown()
        {
            m_Field.Destroy();
        }

        [Test]
        public void ThenExportAcceptsIt()
        {
            CollectionAssert.IsEmpty(m_Field.Export());
        }
    }
}
