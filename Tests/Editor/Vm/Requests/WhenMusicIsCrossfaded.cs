using NUnit.Framework;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>A crossfade's song and stem state are hashes, never variables; its seconds and volume can be either.</summary>
    internal sealed class WhenMusicIsCrossfaded
    {
        private const ulong  SONG  = 0x1122334455667788UL;
        private const ulong  STATE = 0x99AABBCCDDEEFF00UL;
        private const ushort MARK  = 1000;

        private TestField m_Field;
        private ulong     m_Song;
        private ulong     m_State;
        private float     m_Seconds;
        private float     m_Volume;

        [SetUp]
        public void SetUp()
        {
            m_Field   = new TestField();
            m_Song    = 0;
            m_State   = 0;
            m_Seconds = 0f;
            m_Volume  = 0f;

            m_Field.Memory.WriteFloat(MemoryBank.Persistent, 50, 2.5f);
            m_Field.Vm.RequestMusicCrossfade += OnCrossfade;

            m_Field.Run(new Script().Op(FieldScriptOpCode.CrossfadeMusic).ULong(SONG).ULong(STATE)
                                    .Sources(Source.Persistent, Source.Immediate).UShort(50).Float(0.25f)
                                    .SetByte(MARK, 1).Return());
        }

        [Test]
        public void ThenItAsksForTheSongItNames()
        {
            Assert.AreEqual(SONG, m_Song);
        }

        [Test]
        public void ThenItAsksForTheStemStateItNames()
        {
            Assert.AreEqual(STATE, m_State);
        }

        [Test]
        public void ThenItsSecondsComeFromAVariable()
        {
            Assert.AreEqual(2.5f, m_Seconds);
        }

        [Test]
        public void ThenItsVolumeSharesTheSecondsSourcesByte()
        {
            Assert.AreEqual(0.25f, m_Volume);
        }

        [Test]
        public void ThenItsArgumentsAreReadExactly()
        {
            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        private void OnCrossfade(ulong song, ulong state, float seconds, float volume)
        {
            m_Song    = song;
            m_State   = state;
            m_Seconds = seconds;
            m_Volume  = volume;
        }
    }
}
