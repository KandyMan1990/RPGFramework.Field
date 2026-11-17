using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    internal sealed class WhenAChannelsMessageSpeedIsSet
    {
        private byte  m_Channel;
        private float m_CharactersPerSecond;

        [SetUp]
        public void SetUp()
        {
            TestField field = new TestField();

            m_Channel             = 0;
            m_CharactersPerSecond = -1f;

            field.Vm.RequestSetMessageSpeed += OnSetMessageSpeed;

            field.Run(new Script().Op(FieldScriptOpCode.SetMessageSpeed).Byte(3).Sources(Source.Immediate, Source.Immediate).Float(45f).Return());
        }

        [Test]
        public void ThenItIsThatChannel()
        {
            Assert.AreEqual(3, m_Channel);
        }

        [Test]
        public void ThenItIsThatSpeed()
        {
            Assert.AreEqual(45f, m_CharactersPerSecond);
        }

        private void OnSetMessageSpeed(byte channel, float charactersPerSecond)
        {
            m_Channel             = channel;
            m_CharactersPerSecond = charactersPerSecond;
        }
    }
}
