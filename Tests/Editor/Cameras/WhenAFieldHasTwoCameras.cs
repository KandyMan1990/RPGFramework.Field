using NUnit.Framework;

namespace RPGFramework.Field.Tests.Cameras
{
    /// <summary>Two cameras, each named by an id of its own, for scripts to cut between.</summary>
    internal sealed class WhenAFieldHasTwoCameras
    {
        private CameraField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new CameraField();
            m_Field.AddCamera("Hall", 0);
            m_Field.AddCamera("Stairs", 1);
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

        [Test]
        public void ThenScriptsCanNameBoth()
        {
            m_Field.Export();

            CollectionAssert.AreEquivalent(new[] { 0, 1 }, m_Field.CameraIds);
        }
    }
}
