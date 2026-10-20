using NUnit.Framework;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    internal sealed class WhenAScriptHandsItsSlotToAnother
    {
        private const ushort OLD = 70;
        private const ushort NEW = 200;

        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();

            int handing = m_Field.Add(new Script().ReturnTo(1).SetByte(OLD, 1).Return());
            int handed  = m_Field.Add(new Script().Count(NEW, 40).Return());

            m_Field.Tick(m_Field.AddEntity(1, new byte[] { 0, 0 }, handing, handed));
        }

        [Test]
        public void ThenNothingAfterItRunsInTheOldScript()
        {
            Assert.AreEqual(0, m_Field.Memory.ReadByte(MemoryBank.Persistent, OLD));
        }

        [Test]
        public void ThenTheNewScriptRunsOnWhatIsLeftOfTheFrame()
        {
            Assert.AreEqual(FieldVM.INSTRUCTIONS_PER_ENTITY_PER_FRAME - 1, m_Field.CountAt(NEW));
        }
    }
}
