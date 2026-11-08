using NUnit.Framework;

namespace RPGFramework.Field.Tests.Sounds
{
    /// <summary>A sound played at half volume, panned, on channel 3.</summary>
    internal sealed class WhenASoundIsPlayedOnAChannel
    {
        private const byte  CHANNEL = 3;
        private const ulong SOUND   = 11;

        private FakeFieldSoundPlayer m_Player;
        private FieldSoundChannels   m_Channels;

        [SetUp]
        public void SetUp()
        {
            m_Player   = new FakeFieldSoundPlayer();
            m_Channels = new FieldSoundChannels(m_Player);

            m_Channels.Play(SOUND, 0.5f, -1f, CHANNEL);
        }

        [Test]
        public void ThenItPlaysAsAsked()
        {
            Assert.AreEqual(SOUND, m_Player.Played[0].NameHash);
            Assert.AreEqual(0.5f, m_Player.Played[0].Volume);
            Assert.AreEqual(-1f, m_Player.Played[0].Pan);
        }

        [Test]
        public void ThenItStartsFromTheBeginning()
        {
            Assert.AreEqual(0f, m_Player.Played[0].StartSeconds);
        }

        [Test]
        public void ThenStoppingTheChannelStopsIt()
        {
            m_Channels.Stop(CHANNEL);

            Assert.AreEqual(1, m_Player.Played[0].StopCount);
        }

        [Test]
        public void ThenStoppingItTwiceStopsItOnce()
        {
            m_Channels.Stop(CHANNEL);
            m_Channels.Stop(CHANNEL);

            Assert.AreEqual(1, m_Player.Played[0].StopCount);
        }

        [Test]
        public void ThenAnotherChannelLeavesItAlone()
        {
            m_Channels.Stop(4);

            Assert.AreEqual(0, m_Player.Played[0].StopCount);
        }

        [Test]
        public void ThenAVolumeFadeReachesIt()
        {
            m_Channels.FadeVolume(CHANNEL, 0.25f, 2f);

            Assert.AreEqual(0.25f, m_Player.Played[0].Volume);
            Assert.AreEqual(2f, m_Player.Played[0].FadeSeconds);
        }

        [Test]
        public void ThenAPanFadeReachesIt()
        {
            m_Channels.FadePan(CHANNEL, 1f, 3f);

            Assert.AreEqual(1f, m_Player.Played[0].Pan);
            Assert.AreEqual(3f, m_Player.Played[0].FadeSeconds);
        }

        [Test]
        public void ThenASoundOnTheSameChannelReplacesIt()
        {
            m_Channels.Play(22, 1f, 0f, CHANNEL);

            Assert.AreEqual(1, m_Player.Played[0].StopCount);
            Assert.AreEqual(0, m_Player.Played[1].StopCount);
        }

        [Test]
        public void ThenStoppingEverythingStopsIt()
        {
            m_Channels.StopAll();

            Assert.AreEqual(1, m_Player.Played[0].StopCount);
        }
    }
}
