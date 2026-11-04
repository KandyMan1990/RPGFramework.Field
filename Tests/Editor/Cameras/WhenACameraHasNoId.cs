using NUnit.Framework;

namespace RPGFramework.Field.Tests.Cameras
{
    /// <summary>A Cinemachine camera with no field camera beside it, which nothing could choose.</summary>
    internal sealed class WhenACameraHasNoId
    {
        private CameraField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new CameraField();
            m_Field.AddCamera("Hall", 0);
            m_Field.AddCameraWithoutId("Stairs");
        }

        [TearDown]
        public void TearDown()
        {
            m_Field.Destroy();
        }

        [Test]
        public void ThenExportRefusesIt()
        {
            string[] problems = m_Field.Export().ToArray();

            Assert.AreEqual(1, problems.Length);
            StringAssert.Contains("'Stairs' has no FieldCamera", problems[0]);
        }
    }
}
