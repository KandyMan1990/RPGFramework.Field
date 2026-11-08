using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>An entity takes the place and facing of entity 7.</summary>
    internal sealed class WhenAnEntityCopiesAnothersPose
    {
        private const byte SOURCE = 7;

        private TestField          m_Field;
        private FieldEntityRuntime m_Entity;
        private int                m_Copier;
        private int                m_Source;

        [SetUp]
        public void SetUp()
        {
            m_Field  = new TestField();
            m_Copier = -1;
            m_Source = -1;

            m_Field.Vm.RequestCopyEntityPose += OnCopy;

            m_Entity = m_Field.Run(new Script().Op(FieldScriptOpCode.CopyEntityPose).Sources(Source.Immediate, Source.Immediate).Byte(SOURCE).Return());
        }

        [Test]
        public void ThenItIsThisEntityThatMoves()
        {
            Assert.AreEqual(m_Entity.EntityId, m_Copier);
        }

        [Test]
        public void ThenItCopiesTheEntityNamed()
        {
            Assert.AreEqual(SOURCE, m_Source);
        }

        private void OnCopy(int entityId, int sourceEntityId)
        {
            m_Copier = entityId;
            m_Source = sourceEntityId;
        }
    }
}
