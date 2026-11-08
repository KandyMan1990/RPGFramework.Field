using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    /// <summary>
    /// Main has been interrupted by a script in slot 1, and that by one in slot 7, which ends by returning to a slot
    /// below it.
    /// </summary>
    internal sealed class WhenAScriptReturnsToAPriority
    {
        private const ushort MAIN    = 40;
        private const ushort MIDDLE  = 44;
        private const ushort URGENT  = 48;
        private const ushort AFTER   = 52;
        private const byte   URGENCY = 7;

        private TestField          m_Field;
        private FieldEntityRuntime m_Entity;
        private int                m_Urgent;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
        }

        [Test]
        public void ThenThatPriorityCarriesOn()
        {
            ReturnTo(0);

            Assert.AreEqual(1, m_Field.CountAt(MAIN));
        }

        [Test]
        public void ThenTheScriptsBetweenAreThrownAway()
        {
            ReturnTo(0);

            Assert.AreEqual(0, m_Field.CountAt(MIDDLE));
            Assert.IsFalse(m_Entity.IsSlotOccupied(1));
        }

        [Test]
        public void ThenItsOwnSlotIsFree()
        {
            ReturnTo(0);

            Assert.IsFalse(m_Entity.IsSlotOccupied(URGENCY));
        }

        [Test]
        public void ThenTheRestOfTheScriptDoesNotRun()
        {
            ReturnTo(0);

            Assert.AreEqual(0, m_Field.CountAt(AFTER));
        }

        [Test]
        public void ThenAnEmptySlotFallsToTheNextOccupiedBelowIt()
        {
            ReturnTo(3);

            Assert.AreEqual(1, m_Field.CountAt(MIDDLE));
        }

        [Test]
        public void ThenASlotAtOrAboveItsOwnJustEndsIt()
        {
            ReturnTo(URGENCY);

            Assert.AreEqual(0, m_Field.CountAt(AFTER));
            Assert.AreEqual(1, m_Field.CountAt(MIDDLE));
        }

        private void ReturnTo(byte priority)
        {
            int main   = m_Field.Add(new Script().Yield().Count(MAIN, 1).Return());
            int middle = m_Field.Add(new Script().Yield().Count(MIDDLE, 1).Return());

            m_Urgent = m_Field.Add(new Script().Count(URGENT, 1).ReturnToPriority(priority).Count(AFTER, 1).Return());
            m_Entity = m_Field.AddEntity(1, new byte[] { 0, 1, URGENCY }, main, middle, m_Urgent);

            m_Field.Tick(m_Entity);
            m_Entity.TryRequestScript(middle, 1);
            m_Field.Tick(m_Entity);
            m_Entity.TryRequestScript(m_Urgent, URGENCY);
            m_Field.Tick(m_Entity);
        }
    }
}
