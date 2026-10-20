using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    internal sealed class WhenAWaitToStartIsQueuedBehindAMoreUrgentScript
    {
        private const byte   CALLER = 1;
        private const byte   TARGET = 2;
        private const ushort AFTER  = 200;

        private TestField          m_Field;
        private FieldEntityRuntime m_Caller;
        private FieldEntityRuntime m_Target;

        [SetUp]
        public void SetUp()
        {
            m_Field  = new TestField();
            m_Target = m_Field.AddEntity(TARGET, new byte[] { 0, 3 }, m_Field.Add(new Script().Return()), m_Field.Add(new Script().Return()));

            m_Target.TryRequestScript(9998, 7);

            int caller = m_Field.Add(new Script().Request(FieldScriptOpCode.RunAnotherEntityScriptWaitUntilStarted, TARGET, 1).Count(AFTER, 40).Return());

            m_Caller = m_Field.AddEntity(CALLER, new byte[1], caller);
            m_Field.Tick(m_Caller, 3);
        }

        [Test]
        public void ThenTheCallerWaits()
        {
            Assert.AreEqual(0, m_Field.CountAt(AFTER));
        }

        [Test]
        public void ThenTheCallerCarriesOnOnceTheScriptIsRunning()
        {
            m_Target.ClearSlot(7);
            m_Field.Tick(m_Caller, 2);

            Assert.Greater(m_Field.CountAt(AFTER), 0);
        }
    }
}
