using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    internal sealed class WhenTheMusicIsStopped
    {
        private bool m_Stopped;

        [SetUp]
        public void SetUp()
        {
            TestField field = new TestField();

            m_Stopped = false;

            field.Vm.RequestStopMusic += OnStop;
            field.Run(new Script().Op(FieldScriptOpCode.StopMusic).Return());
        }

        [Test]
        public void ThenItIsAskedFor()
        {
            Assert.IsTrue(m_Stopped);
        }

        private void OnStop()
        {
            m_Stopped = true;
        }
    }
}
