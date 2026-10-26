using NUnit.Framework;

namespace RPGFramework.Field.Tests.Cameras
{
    /// <summary>Nothing chooses between a field's cameras yet.</summary>
    internal sealed class WhenAFieldHasTwoCameras
    {
        private CameraField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new CameraField();
            m_Field.AddCamera("Hall");
            m_Field.AddCamera("Stairs");
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
