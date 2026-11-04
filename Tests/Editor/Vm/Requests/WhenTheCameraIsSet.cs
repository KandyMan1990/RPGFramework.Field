using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.FieldVmArgs;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>A blend to a camera named by a variable, over seconds with an easing.</summary>
    internal sealed class WhenTheCameraIsSet
    {
        private const ushort MARK = 1000;

        private TestField m_Field;
        private int       m_Camera;
        private float     m_Seconds;
        private Easing    m_Easing;

        [SetUp]
        public void SetUp()
        {
            m_Field   = new TestField();
            m_Camera  = -1;
            m_Seconds = 0f;
            m_Easing  = Easing.Linear;

            m_Field.Memory.WriteByte(MemoryBank.Persistent, 50, 3);
            m_Field.Vm.RequestSetCamera += OnSetCamera;

            m_Field.Run(new Script().Op(FieldScriptOpCode.SetCamera)
                                    .Sources(Source.Persistent, Source.Immediate).UShort(50).Float(1.5f)
                                    .Sources(Source.Immediate, Source.Immediate).Byte((byte)Easing.Smooth)
                                    .SetByte(MARK, 1).Return());
        }

        [Test]
        public void ThenItAsksForThatCamera()
        {
            Assert.AreEqual(3, m_Camera);
        }

        [Test]
        public void ThenItAsksForTheBlend()
        {
            Assert.AreEqual(1.5f, m_Seconds);
            Assert.AreEqual(Easing.Smooth, m_Easing);
        }

        [Test]
        public void ThenItsArgumentsAreReadExactly()
        {
            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        private void OnSetCamera(byte camera, float seconds, Easing easing)
        {
            m_Camera  = camera;
            m_Seconds = seconds;
            m_Easing  = easing;
        }
    }
}
