using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    internal sealed class WhenAScriptSwitchesAnEffect
    {
        private const byte EFFECT = 3;

        private TestField m_Field;
        private int       m_Effect;
        private bool      m_Active;

        [SetUp]
        public void SetUp()
        {
            m_Field  = new TestField();
            m_Effect = -1;
            m_Active = false;

            m_Field.Vm.RequestSetEffectActive += OnSwitched;
        }

        [Test]
        public void ThenSwitchingOnIsPassedOn()
        {
            Switch(true);

            Assert.AreEqual(EFFECT, m_Effect);
            Assert.IsTrue(m_Active);
        }

        [Test]
        public void ThenSwitchingOffIsPassedOn()
        {
            m_Active = true;

            Switch(false);

            Assert.AreEqual(EFFECT, m_Effect);
            Assert.IsFalse(m_Active);
        }

        private void Switch(bool active)
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.EffectActivation)
                                    .Sources(Source.Immediate, Source.Immediate).Byte(EFFECT).Bool(active).Return());
        }

        private void OnSwitched(byte effect, bool active)
        {
            m_Effect = effect;
            m_Active = active;
        }
    }
}
