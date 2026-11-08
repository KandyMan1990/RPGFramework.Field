using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.Tests.Vm;

namespace RPGFramework.Field.Tests.Entities
{
    /// <summary>An entity holding a script in Main is left out of the field.</summary>
    internal sealed class WhenAnEntityIsLeftOutOfTheField
    {
        private const ushort MARK  = 1000;
        private const byte   URGENT = 5;

        private TestField          m_Field;
        private FieldEntityRuntime m_Entity;

        [SetUp]
        public void SetUp()
        {
            m_Field  = new TestField();
            m_Entity = m_Field.Start(new Script().SetByte(MARK, 1).Return());
            m_Entity.LeaveOutOfField();
        }

        [Test]
        public void ThenItRunsNothing()
        {
            m_Entity.Update(m_Field.Vm);

            Assert.AreEqual(0, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        [Test]
        public void ThenARequestIsRefused()
        {
            Assert.IsFalse(m_Entity.TryRequestScript(200, URGENT));
        }

        [Test]
        public void ThenARefusedRequestLeavesItsSlotEmpty()
        {
            m_Entity.TryRequestScript(200, URGENT);

            Assert.IsFalse(m_Entity.IsSlotOccupied(URGENT));
        }
    }
}
