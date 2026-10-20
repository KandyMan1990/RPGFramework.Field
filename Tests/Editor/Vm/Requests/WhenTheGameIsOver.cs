using NUnit.Framework;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    internal sealed class WhenTheGameIsOver
    {
        private const ushort MARK = 1000;

        private TestField m_Field;
        private bool      m_Asked;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
            m_Asked = false;

            m_Field.Vm.RequestGameOver += OnGameOver;

            m_Field.Run(new Script().Op(FieldScriptOpCode.GameOver).SetByte(MARK, 1).Return());
        }

        [Test]
        public void ThenItIsAskedFor()
        {
            Assert.IsTrue(m_Asked);
        }

        [Test]
        public void ThenTheScriptsFrameEnds()
        {
            Assert.AreEqual(0, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        private void OnGameOver()
        {
            m_Asked = true;
        }
    }
}
