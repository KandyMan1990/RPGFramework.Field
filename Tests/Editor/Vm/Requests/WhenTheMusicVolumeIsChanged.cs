using NUnit.Framework;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>Setting the music volume is a fade of no length, so setting and fading ask for the same thing.</summary>
    internal sealed class WhenTheMusicVolumeIsChanged
    {
        private const ushort MARK = 1000;

        private TestField m_Field;
        private float     m_From;
        private float     m_Volume;
        private float     m_Seconds;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
            m_Field.Memory.WriteFloat(MemoryBank.Persistent, 50, 2.5f);

            m_From    = -1f;
            m_Volume  = -1f;
            m_Seconds = -1f;

            m_Field.Vm.RequestMusicVolume     += OnVolume;
            m_Field.Vm.RequestMusicVolumeFrom += OnVolumeFrom;
        }

        [Test]
        public void ThenSettingItAsksForItAtOnce()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.SetMusicVolume).Sources(Source.Immediate, Source.Immediate).Float(0.6f).Return());

            Assert.AreEqual(0.6f, m_Volume);
            Assert.AreEqual(0f, m_Seconds);
        }

        [Test]
        public void ThenFadingItAsksForItOverItsSeconds()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.FadeMusicVolume).Sources(Source.Immediate, Source.Persistent).Float(0.4f).UShort(50)
                                    .Return());

            Assert.AreEqual(0.4f, m_Volume);
            Assert.AreEqual(2.5f, m_Seconds);
        }

        [Test]
        public void ThenFadingFromAVolumeAsksWhereItStartsWhereItEndsAndHowLong()
        {
            m_Field.Run(FadeFrom().Return());

            Assert.AreEqual(0.1f, m_From);
            Assert.AreEqual(0.9f, m_Volume);
            Assert.AreEqual(2.5f, m_Seconds);
        }

        [Test]
        public void ThenAFadeFromAVolumeIsReadExactly()
        {
            m_Field.Run(FadeFrom().SetByte(MARK, 1).Return());

            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        private static Script FadeFrom()
        {
            return new Script().Op(FieldScriptOpCode.FadeMusicVolumeFrom).Sources(Source.Immediate, Source.Immediate).Float(0.1f).Float(0.9f)
                               .Sources(Source.Persistent, Source.Immediate).UShort(50);
        }

        private void OnVolume(float volume, float seconds)
        {
            m_Volume  = volume;
            m_Seconds = seconds;
        }

        private void OnVolumeFrom(float from, float volume, float seconds)
        {
            m_From    = from;
            m_Volume  = volume;
            m_Seconds = seconds;
        }
    }
}
