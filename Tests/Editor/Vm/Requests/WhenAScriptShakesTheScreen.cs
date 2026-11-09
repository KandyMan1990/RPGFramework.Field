using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>A script starting the screen's shake, or stopping it.</summary>
    internal sealed class WhenAScriptShakesTheScreen
    {
        private TestField m_Field;
        private float[]   m_Shake;
        private bool      m_Stopped;

        [SetUp]
        public void SetUp()
        {
            m_Field   = new TestField();
            m_Shake   = null;
            m_Stopped = false;

            m_Field.Vm.RequestShakeScreen     += OnShake;
            m_Field.Vm.RequestStopScreenShake += OnStop;
        }

        [Test]
        public void ThenBothAxesArePassedOn()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.ShakeScreen)
                                    .Sources(Source.Immediate, Source.Immediate).Float(24f).Float(0.5f)
                                    .Sources(Source.Immediate, Source.Immediate).Float(12f).Float(0.4f).Return());

            Assert.AreEqual(new[] { 24f, 0.5f, 12f, 0.4f }, m_Shake);
        }

        [Test]
        public void ThenStoppingIsPassedOn()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.StopScreenShake).Return());

            Assert.IsTrue(m_Stopped);
        }

        private void OnShake(float amplitudeX, float swingSecondsX, float amplitudeY, float swingSecondsY)
        {
            m_Shake = new[] { amplitudeX, swingSecondsX, amplitudeY, swingSecondsY };
        }

        private void OnStop()
        {
            m_Stopped = true;
        }
    }
}
