using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    internal sealed class WhenAScriptWaitsForTheMusicVolume
    {
        private const ushort AFTER = 200;

        private TestField          m_Field;
        private FieldEntityRuntime m_Entity;
        private bool               m_Fading;

        [SetUp]
        public void SetUp()
        {
            m_Field  = new TestField();
            m_Fading = true;

            m_Field.Vm.IsMusicVolumeFading = IsFading;

            m_Entity = m_Field.Start(new Script().Op(FieldScriptOpCode.WaitForMusicVolume).Count(AFTER, 40).Return());
            m_Field.Tick(m_Entity, 3);
        }

        [Test]
        public void ThenItWaitsWhileTheVolumeFades()
        {
            Assert.AreEqual(0, m_Field.CountAt(AFTER));
        }

        [Test]
        public void ThenItCarriesOnOnceTheFadeIsDone()
        {
            m_Fading = false;
            m_Field.Tick(m_Entity, 2);

            Assert.Greater(m_Field.CountAt(AFTER), 0);
        }

        private bool IsFading()
        {
            return m_Fading;
        }
    }
}
