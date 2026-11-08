using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    /// <summary>A script asks for a script of an entity left out of the field, and waits for it, which would be for ever.</summary>
    internal sealed class WhenAScriptWaitsForAnEntityLeftOutOfTheField
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
            m_Target = m_Field.AddEntity(TARGET, new byte[] { 0, 3 }, m_Field.Add(new Script().Return()), m_Field.Add(new Script().Yield().Return()));
            m_Target.LeaveOutOfField();
        }

        [Test]
        public void ThenWaitingForItToFinishCarriesOn()
        {
            Request(FieldScriptOpCode.RunAnotherEntityScriptWaitUntilFinished);

            Assert.Greater(m_Field.CountAt(AFTER), 0);
        }

        [Test]
        public void ThenWaitingForItToStartCarriesOn()
        {
            Request(FieldScriptOpCode.RunAnotherEntityScriptWaitUntilStarted);

            Assert.Greater(m_Field.CountAt(AFTER), 0);
        }

        [Test]
        public void ThenItsSlotStaysEmpty()
        {
            Request(FieldScriptOpCode.RunAnotherEntityScriptWaitUntilFinished);

            Assert.IsFalse(m_Target.IsSlotOccupied(3));
        }

        private void Request(FieldScriptOpCode opCode)
        {
            int caller = m_Field.Add(new Script().Request(opCode, TARGET, 1).Count(AFTER, 1).Return());

            m_Caller = m_Field.AddEntity(CALLER, new byte[1], caller);
            m_Field.Tick(m_Caller, 3);
        }
    }
}
