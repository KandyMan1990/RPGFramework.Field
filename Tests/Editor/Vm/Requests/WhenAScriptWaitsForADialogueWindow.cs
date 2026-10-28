using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>
    /// A script waits for a dialogue channel to be free. The field is told, as a window left up in the background closes
    /// on a press once a script waits for it.
    /// </summary>
    internal sealed class WhenAScriptWaitsForADialogueWindow
    {
        private const byte CHANNEL = 2;
        private const int  NONE    = -1;

        private TestField m_Field;
        private bool      m_Open;
        private int       m_WaitedFor;

        [SetUp]
        public void SetUp()
        {
            m_Field     = new TestField();
            m_Open      = true;
            m_WaitedFor = NONE;

            m_Field.Vm.IsDialogueWindowOpen         =  IsOpen;
            m_Field.Vm.RequestWaitForDialogueWindow += OnWaitFor;
        }

        [Test]
        public void ThenTheFieldIsToldWhichChannel()
        {
            m_Field.Tick(m_Field.Start(WaitForTheWindow()));

            Assert.AreEqual(CHANNEL, m_WaitedFor);
        }

        [Test]
        public void ThenWithNothingShowingTheFieldIsNotTold()
        {
            m_Open = false;

            m_Field.Tick(m_Field.Start(WaitForTheWindow()));

            Assert.AreEqual(NONE, m_WaitedFor);
        }

        private static Script WaitForTheWindow()
        {
            return new Script().Op(FieldScriptOpCode.WaitForDialogueWindow).Byte(CHANNEL).Return();
        }

        private bool IsOpen(byte channel)
        {
            return m_Open;
        }

        private void OnWaitFor(byte channel)
        {
            m_WaitedFor = channel;
        }
    }
}
