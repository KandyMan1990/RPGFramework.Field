using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>An init script says whether its entity is in the field this visit.</summary>
    internal sealed class WhenAScriptSaysWhetherItsEntityIsInTheField
    {
        private TestField m_Field;
        private int       m_Entity;
        private bool?     m_InField;

        [SetUp]
        public void SetUp()
        {
            m_Field   = new TestField();
            m_Entity  = -1;
            m_InField = null;

            m_Field.Vm.RequestSetEntityInField += OnInField;
        }

        [Test]
        public void ThenLeavingItOutIsPassedOn()
        {
            Say(false);

            Assert.AreEqual(false, m_InField);
        }

        [Test]
        public void ThenKeepingItInIsPassedOn()
        {
            Say(true);

            Assert.AreEqual(true, m_InField);
        }

        [Test]
        public void ThenItIsThisEntityItIsSaidOf()
        {
            FieldEntityRuntime entity = Say(false);

            Assert.AreEqual(entity.EntityId, m_Entity);
        }

        private FieldEntityRuntime Say(bool inField)
        {
            return m_Field.Run(new Script().Op(FieldScriptOpCode.EntityInField).Sources(Source.Immediate, Source.Immediate).Bool(inField).Return());
        }

        private void OnInField(int entityId, bool inField)
        {
            m_Entity  = entityId;
            m_InField = inField;
        }
    }
}
