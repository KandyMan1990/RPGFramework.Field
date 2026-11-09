using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>A script choosing an entity's footsteps, or every entity's.</summary>
    internal sealed class WhenAScriptSetsFootsteps
    {
        private const ulong SOUND = 77;

        private TestField m_Field;
        private int       m_Entity;
        private ulong     m_Sound;
        private bool      m_Enabled;
        private bool      m_AllEntities;

        [SetUp]
        public void SetUp()
        {
            m_Field       = new TestField();
            m_Entity      = -1;
            m_Sound       = 0;
            m_Enabled     = false;
            m_AllEntities = false;

            m_Field.Vm.RequestSetFootstepSound    += OnSound;
            m_Field.Vm.RequestSetFootstepsEnabled += OnEnabled;
        }

        [Test]
        public void ThenTheSoundIsPassedOn()
        {
            FieldEntityRuntime entity = SetSound(false);

            Assert.AreEqual(entity.EntityId, m_Entity);
            Assert.AreEqual(SOUND, m_Sound);
            Assert.IsFalse(m_AllEntities);
        }

        [Test]
        public void ThenTheSoundCanBeEveryEntitys()
        {
            SetSound(true);

            Assert.IsTrue(m_AllEntities);
        }

        [Test]
        public void ThenWhetherTheyAreOnIsPassedOn()
        {
            FieldEntityRuntime entity = SetEnabled(true, false);

            Assert.AreEqual(entity.EntityId, m_Entity);
            Assert.IsTrue(m_Enabled);
            Assert.IsFalse(m_AllEntities);
        }

        [Test]
        public void ThenTheyCanBeTurnedOffForEveryEntity()
        {
            m_Enabled = true;
            SetEnabled(false, true);

            Assert.IsFalse(m_Enabled);
            Assert.IsTrue(m_AllEntities);
        }

        private FieldEntityRuntime SetSound(bool allEntities)
        {
            return m_Field.Run(new Script().Op(FieldScriptOpCode.SetFootstepSound).ULong(SOUND)
                                           .Sources(Source.Immediate, Source.Immediate).Bool(allEntities).Return());
        }

        private FieldEntityRuntime SetEnabled(bool enabled, bool allEntities)
        {
            return m_Field.Run(new Script().Op(FieldScriptOpCode.SetFootstepsEnabled)
                                           .Sources(Source.Immediate, Source.Immediate).Bool(enabled).Bool(allEntities).Return());
        }

        private void OnSound(int entityId, ulong sound, bool allEntities)
        {
            m_Entity      = entityId;
            m_Sound       = sound;
            m_AllEntities = allEntities;
        }

        private void OnEnabled(int entityId, bool enabled, bool allEntities)
        {
            m_Entity      = entityId;
            m_Enabled     = enabled;
            m_AllEntities = allEntities;
        }
    }
}
