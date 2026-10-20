using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    internal sealed class WhenTheExitFadeIsTurnedOff
    {
        private bool m_Fades;

        [SetUp]
        public void SetUp()
        {
            TestField field = new TestField();

            m_Fades = true;

            field.Vm.RequestFieldExitFade += OnExitFade;
            field.Run(new Script().Op(FieldScriptOpCode.SetFieldExitFade).Sources(Source.Immediate, Source.Immediate).Bool(false).Return());
        }

        [Test]
        public void ThenItIsAskedFor()
        {
            Assert.IsFalse(m_Fades);
        }

        private void OnExitFade(bool fades)
        {
            m_Fades = fades;
        }
    }
}
