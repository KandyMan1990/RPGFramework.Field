using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    /// <summary>A request that waits asks once: a busy slot means carrying on, not asking again.</summary>
    internal sealed class WhenAWaitToStartFindsItsSlotBusy
    {
        private const byte   CALLER   = 1;
        private const byte   TARGET   = 2;
        private const int    HELD     = 9999;
        private const ushort AFTER    = 200;

        private TestField          m_Field;
        private FieldEntityRuntime m_Caller;
        private FieldEntityRuntime m_Target;

        [SetUp]
        public void SetUp()
        {
            m_Field  = new TestField();
            m_Target = m_Field.AddEntity(TARGET, new byte[] { 0, 3 }, m_Field.Add(new Script().Return()), m_Field.Add(new Script().Return()));

            m_Target.TryRequestScript(HELD, 3);

            int caller = m_Field.Add(new Script().Request(FieldScriptOpCode.RunAnotherEntityScriptWaitUntilStarted, TARGET, 1).Count(AFTER, 40).Return());

            m_Caller = m_Field.AddEntity(CALLER, new byte[1], caller);
            m_Field.Tick(m_Caller);
        }

        [Test]
        public void ThenWhatHoldsTheSlotStays()
        {
            Assert.IsTrue(m_Target.HoldsScript(HELD, 3));
        }

        [Test]
        public void ThenTheCallerCarriesOnTheNextFrame()
        {
            m_Field.Tick(m_Caller);

            Assert.AreEqual(FieldVM.INSTRUCTIONS_PER_ENTITY_PER_FRAME, m_Field.CountAt(AFTER));
        }
    }
}
