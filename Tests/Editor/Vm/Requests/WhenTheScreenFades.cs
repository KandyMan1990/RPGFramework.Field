using NUnit.Framework;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    internal sealed class WhenTheScreenFades
    {
        private const ulong  TRANSITION = 0x1122334455667788UL;
        private const ushort MARK       = 1000;

        private TestField m_Field;
        private bool      m_Out;
        private bool      m_AtOnce;
        private ulong     m_Transition;

        [SetUp]
        public void SetUp()
        {
            m_Field      = new TestField();
            m_AtOnce     = true;
            m_Out        = false;
            m_Transition = 0;

            m_Field.Vm.RequestScreenFade += OnFade;
            m_Field.Run(new Script().Op(FieldScriptOpCode.FadeScreen).Sources(Source.Immediate, Source.Immediate).Bool(true).Bool(false)
                                    .ULong(TRANSITION).SetByte(MARK, 1).Return());
        }

        [Test]
        public void ThenItAsksWhichWay()
        {
            Assert.IsTrue(m_Out);
        }

        [Test]
        public void ThenItAsksWhetherAtOnce()
        {
            Assert.IsFalse(m_AtOnce);
        }

        [Test]
        public void ThenItAsksForItsTransition()
        {
            Assert.AreEqual(TRANSITION, m_Transition);
        }

        [Test]
        public void ThenItsArgumentsAreReadExactly()
        {
            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        private void OnFade(bool fadeOut, bool atOnce, ulong transition)
        {
            m_Out        = fadeOut;
            m_AtOnce     = atOnce;
            m_Transition = transition;
        }
    }
}
