using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    internal sealed class WhenAScriptAsksForOneOnAnotherEntity
    {
        private const byte   CALLER = 1;
        private const byte   TARGET = 2;
        private const ushort AFTER  = 200;

        private TestField          m_Field;
        private FieldEntityRuntime m_Target;
        private int                m_Requested;

        [SetUp]
        public void SetUp()
        {
            m_Field     = new TestField();
            m_Requested = m_Field.Add(new Script().Return());
            m_Target    = m_Field.AddEntity(TARGET, new byte[] { 0, 1 }, m_Field.Add(new Script().Return()), m_Requested);

            int caller = m_Field.Add(new Script().Request(FieldScriptOpCode.RunAnotherEntityScriptUnlessBusy, TARGET, 1).Count(AFTER, 40).Return());

            m_Field.Tick(m_Field.AddEntity(CALLER, new byte[1], caller));
        }

        [Test]
        public void ThenTheCallersFrameEnds()
        {
            Assert.AreEqual(0, m_Field.CountAt(AFTER));
        }

        [Test]
        public void ThenTheOtherEntityHoldsTheScriptInItsSlot()
        {
            Assert.IsTrue(m_Target.HoldsScript(m_Requested, 1));
        }
    }
}
