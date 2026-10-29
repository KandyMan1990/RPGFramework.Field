using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>
    /// A message left up while the script carries on: until a script closes it, or, dismissible, until a press does once
    /// it has typed.
    /// </summary>
    internal sealed class WhenAMessageIsShownWithoutWaiting
    {
        private TestField m_Field;
        private bool      m_Shown;
        private bool      m_Dismissible;

        [SetUp]
        public void SetUp()
        {
            m_Field       = new TestField();
            m_Shown       = false;
            m_Dismissible = false;

            m_Field.Vm.IsDialogueWindowOpen            =  IsOpen;
            m_Field.Vm.RequestShowDialogueWindowNoWait += OnShown;
        }

        [Test]
        public void ThenNoWaitIsNotDismissible()
        {
            m_Field.Run(Show(FieldScriptOpCode.ShowDialogueWindowNoWait));

            Assert.IsTrue(m_Shown);
            Assert.IsFalse(m_Dismissible);
        }

        [Test]
        public void ThenTheDismissibleOneIs()
        {
            m_Field.Run(Show(FieldScriptOpCode.ShowDialogueWindowNoWaitDismissible));

            Assert.IsTrue(m_Shown);
            Assert.IsTrue(m_Dismissible);
        }

        private static Script Show(FieldScriptOpCode opCode)
        {
            return new Script().Op(opCode).Byte(1).ULong(10).Return();
        }

        private static bool IsOpen(byte channel)
        {
            return false;
        }

        private void OnShown(byte channel, ulong dialogueId, bool dismissible)
        {
            m_Shown       = true;
            m_Dismissible = dismissible;
        }
    }
}
