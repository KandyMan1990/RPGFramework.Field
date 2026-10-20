using NUnit.Framework;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    internal sealed class WhenAScriptAsksWhetherMusicIsPlaying
    {
        private const ushort PLAYING = 70;

        private TestField m_Field;
        private bool      m_Playing;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
            m_Field.Vm.IsMusicPlaying = IsPlaying;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ThenTheAnswerIsStoredWhereItAsks(bool playing)
        {
            m_Playing = playing;
            m_Field.Memory.WriteBool(MemoryBank.Persistent, PLAYING, !playing);

            m_Field.Run(new Script().Op(FieldScriptOpCode.CheckIfMusicIsPlaying).Sources(Source.Persistent, Source.Immediate).UShort(PLAYING)
                                    .Return());

            Assert.AreEqual(playing, m_Field.Memory.ReadBool(MemoryBank.Persistent, PLAYING));
        }

        private bool IsPlaying()
        {
            return m_Playing;
        }
    }
}
