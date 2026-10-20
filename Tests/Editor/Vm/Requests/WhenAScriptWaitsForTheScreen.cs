using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>Each wait holds the script while the field says the screen is still changing.</summary>
    internal sealed class WhenAScriptWaitsForTheScreen
    {
        private const ushort AFTER = 200;

        private TestField m_Field;
        private bool      m_Changing;
        private bool      m_FadeAsked;

        [SetUp]
        public void SetUp()
        {
            m_Field     = new TestField();
            m_Changing  = true;
            m_FadeAsked = false;

            m_Field.Vm.IsScreenFading       = IsChanging;
            m_Field.Vm.IsScreenColourFading = IsChanging;
            m_Field.Vm.RequestScreenFade    += OnFade;
        }

        [TestCase(FieldScriptOpCode.WaitForFade)]
        [TestCase(FieldScriptOpCode.WaitForScreenColour)]
        public void ThenItWaitsWhileTheScreenChanges(FieldScriptOpCode opCode)
        {
            FieldEntityRuntime entity = m_Field.Start(new Script().Op(opCode).Count(AFTER, 40).Return());

            m_Field.Tick(entity, 3);

            Assert.AreEqual(0, m_Field.CountAt(AFTER));
        }

        [TestCase(FieldScriptOpCode.WaitForFade)]
        [TestCase(FieldScriptOpCode.WaitForScreenColour)]
        public void ThenItCarriesOnOnceTheScreenHasChanged(FieldScriptOpCode opCode)
        {
            FieldEntityRuntime entity = m_Field.Start(new Script().Op(opCode).Count(AFTER, 40).Return());

            m_Field.Tick(entity, 3);
            m_Changing = false;
            m_Field.Tick(entity, 2);

            Assert.Greater(m_Field.CountAt(AFTER), 0);
        }

        [Test]
        public void ThenFadingAndWaitingAsksForTheFade()
        {
            m_Field.Run(FadeAndWait());

            Assert.IsTrue(m_FadeAsked);
        }

        [Test]
        public void ThenFadingAndWaitingWaitsUntilTheScreenHasFaded()
        {
            FieldEntityRuntime entity = m_Field.Start(FadeAndWait());

            m_Field.Tick(entity, 3);
            int whileFading = m_Field.CountAt(AFTER);
            m_Changing = false;
            m_Field.Tick(entity, 2);

            Assert.AreEqual(0, whileFading);
            Assert.Greater(m_Field.CountAt(AFTER), 0);
        }

        private static Script FadeAndWait()
        {
            return new Script().Op(FieldScriptOpCode.FadeScreenWait).Sources(Source.Immediate, Source.Immediate).Bool(false).Bool(true)
                               .ULong(0).Count(AFTER, 40).Return();
        }

        private bool IsChanging()
        {
            return m_Changing;
        }

        private void OnFade(bool fadeOut, bool atOnce, ulong transition)
        {
            m_FadeAsked = true;
        }
    }
}
