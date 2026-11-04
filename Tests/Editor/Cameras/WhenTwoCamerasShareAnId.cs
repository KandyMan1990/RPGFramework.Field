using NUnit.Framework;

namespace RPGFramework.Field.Tests.Cameras
{
    internal sealed class WhenTwoCamerasShareAnId
    {
        private CameraField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new CameraField();
            m_Field.AddCamera("Hall", 1);
            m_Field.AddCamera("Stairs", 1);
        }

        [TearDown]
        public void TearDown()
        {
            m_Field.Destroy();
        }

        [Test]
        public void ThenExportRefusesTheSecond()
        {
            string[] problems = m_Field.Export().ToArray();

            Assert.AreEqual(1, problems.Length);
            StringAssert.Contains("'Stairs' reuses camera id [1]", problems[0]);
        }
    }
}
