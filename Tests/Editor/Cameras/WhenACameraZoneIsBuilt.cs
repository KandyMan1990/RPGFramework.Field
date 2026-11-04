using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Cameras
{
    /// <summary>A camera zone in a field of three cameras, the highest id taken as the newest.</summary>
    internal sealed class WhenACameraZoneIsBuilt
    {
        private CameraField       m_Field;
        private FieldEntityRecord m_Zone;

        [SetUp]
        public void SetUp()
        {
            m_Field = new CameraField();
            m_Field.AddCamera("Hall", 0);
            m_Field.AddCamera("Stairs", 2);
            m_Field.AddCamera("Landing", 1);

            m_Zone = new FieldEntityRecord(5, "CameraZone5", null);
            m_Zone.Scripts.Add(new FieldScriptRecord(FieldScriptType.Init, string.Empty, FieldEntityBodyBuilder.NEW_SCRIPT_TEXT));

            FieldEntityBodyBuilder.WritePresetScripts(m_Zone, FieldBodyPreset.CameraZone, false, m_Field.Root);
        }

        [TearDown]
        public void TearDown()
        {
            m_Field.Destroy();
        }

        [Test]
        public void ThenItsEnterScriptCutsToTheHighestId()
        {
            FieldScriptRecord enter = m_Zone.Scripts.Find(script => script.Type == FieldScriptType.OnEnter);

            Assert.AreEqual("SET_CAMERA 2 0 0\nRETURN", enter.Text);
        }

        [Test]
        public void ThenItHasNoLeaveScript()
        {
            Assert.IsFalse(m_Zone.Scripts.Exists(script => script.Type == FieldScriptType.OnLeave));
        }
    }
}
