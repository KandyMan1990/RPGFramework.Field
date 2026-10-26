using NUnit.Framework;

namespace RPGFramework.Field.Tests.Cameras
{
    /// <summary>A field with no Cinemachine camera would be seen from wherever the field before it left the view.</summary>
    internal sealed class WhenAFieldHasNoCamera
    {
        private CameraField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new CameraField();
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
