using NUnit.Framework;

namespace RPGFramework.Field.Tests.Sounds
{
    /// <summary>A sound played on channel 0, which keeps nothing.</summary>
    internal sealed class WhenASoundIsPlayedOnNoChannel
    {
        private const byte NO_CHANNEL = 0;

        private FakeFieldSoundPlayer m_Player;
        private FieldSoundChannels   m_Channels;

        [SetUp]
        public void SetUp()
        {
            m_Player   = new FakeFieldSoundPlayer();
            m_Channels = new FieldSoundChannels(m_Player);

            m_Channels.Play(11, 1f, 0f, NO_CHANNEL);
        }

        [Test]
        public void ThenItPlays()
        {
            Assert.AreEqual(1, m_Player.Played.Count);
        }

        [Test]
        public void ThenASecondDoesNotStopTheFirst()
        {
            m_Channels.Play(22, 1f, 0f, NO_CHANNEL);

            Assert.AreEqual(0, m_Player.Played[0].StopCount);
        }

        [Test]
        public void ThenStoppingEverythingLeavesItPlaying()
        {
            m_Channels.StopAll();

            Assert.AreEqual(0, m_Player.Played[0].StopCount);
        }

        [Test]
        public void ThenItIsNotKeptEvenIfItLoops()
        {
            m_Player.Played[0].Loops = true;

            Assert.AreEqual(0, m_Channels.KeepLooping().Length);
        }
    }
}
