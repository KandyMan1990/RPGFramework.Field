using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>A script playing a sound on channel 7, and changing what that channel holds.</summary>
    internal sealed class WhenAScriptPlaysAndControlsASound
    {
        private const byte  CHANNEL = 7;
        private const ulong SOUND   = 99;

        private TestField m_Field;
        private ulong     m_Played;
        private float     m_Volume;
        private float     m_Pan;
        private float     m_Seconds;
        private int       m_Channel;

        [SetUp]
        public void SetUp()
        {
            m_Field   = new TestField();
            m_Played  = 0;
            m_Volume  = -1f;
            m_Pan     = -9f;
            m_Seconds = -1f;
            m_Channel = -1;

            m_Field.Vm.RequestSfx                += OnPlay;
            m_Field.Vm.RequestStopSound          += OnStop;
            m_Field.Vm.RequestFadeSoundVolume    += OnFadeVolume;
            m_Field.Vm.RequestFadeSoundPan       += OnFadePan;
            m_Field.Vm.RequestFadeAllSoundVolume += OnFadeAll;
        }

        [Test]
        public void ThenPlayPassesOnEveryArgument()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.PlaySound).ULong(SOUND)
                                    .Sources(Source.Immediate, Source.Immediate).Float(0.5f).Float(-1f)
                                    .Byte(CHANNEL).Return());

            Assert.AreEqual(SOUND, m_Played);
            Assert.AreEqual(0.5f, m_Volume);
            Assert.AreEqual(-1f, m_Pan);
            Assert.AreEqual(CHANNEL, m_Channel);
        }

        [Test]
        public void ThenStopNamesTheChannel()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.StopSound).Byte(CHANNEL).Return());

            Assert.AreEqual(CHANNEL, m_Channel);
        }

        [Test]
        public void ThenAVolumeFadePassesOnEveryArgument()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.FadeSoundVolume).Byte(CHANNEL)
                                    .Sources(Source.Immediate, Source.Immediate).Float(0.25f).Float(2f).Return());

            Assert.AreEqual(CHANNEL, m_Channel);
            Assert.AreEqual(0.25f, m_Volume);
            Assert.AreEqual(2f, m_Seconds);
        }

        [Test]
        public void ThenAPanFadePassesOnEveryArgument()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.FadeSoundPan).Byte(CHANNEL)
                                    .Sources(Source.Immediate, Source.Immediate).Float(1f).Float(3f).Return());

            Assert.AreEqual(CHANNEL, m_Channel);
            Assert.AreEqual(1f, m_Pan);
            Assert.AreEqual(3f, m_Seconds);
        }

        [Test]
        public void ThenSettingEverySoundsVolumeTakesNoTime()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.SetAllSoundVolume).Sources(Source.Immediate, Source.Immediate).Float(0.5f).Return());

            Assert.AreEqual(0.5f, m_Volume);
            Assert.AreEqual(0f, m_Seconds);
        }

        [Test]
        public void ThenFadingEverySoundsVolumeTakesTheTimeGiven()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.FadeAllSoundVolume).Sources(Source.Immediate, Source.Immediate).Float(0.5f).Float(4f).Return());

            Assert.AreEqual(0.5f, m_Volume);
            Assert.AreEqual(4f, m_Seconds);
        }

        private void OnPlay(ulong nameHash, float volume, float pan, byte channel)
        {
            m_Played  = nameHash;
            m_Volume  = volume;
            m_Pan     = pan;
            m_Channel = channel;
        }

        private void OnStop(byte channel)
        {
            m_Channel = channel;
        }

        private void OnFadeVolume(byte channel, float volume, float seconds)
        {
            m_Channel = channel;
            m_Volume  = volume;
            m_Seconds = seconds;
        }

        private void OnFadePan(byte channel, float pan, float seconds)
        {
            m_Channel = channel;
            m_Pan     = pan;
            m_Seconds = seconds;
        }

        private void OnFadeAll(float volume, float seconds)
        {
            m_Volume  = volume;
            m_Seconds = seconds;
        }
    }
}
