using NUnit.Framework;

namespace RPGFramework.Field.Tests.Sounds
{
    /// <summary>A looping sound on channel 5, 2.5 seconds in, and a one-shot on channel 6.</summary>
    internal sealed class WhenABattleInterruptsTheSounds
    {
        private const byte  LOOP_CHANNEL = 5;
        private const byte  SHOT_CHANNEL = 6;
        private const ulong LOOP         = 11;

        private FakeFieldSoundPlayer m_Player;
        private FieldSoundChannels   m_Channels;

        [SetUp]
        public void SetUp()
        {
            m_Player   = new FakeFieldSoundPlayer();
            m_Channels = new FieldSoundChannels(m_Player);

            m_Channels.Play(LOOP, 0.5f, -0.5f, LOOP_CHANNEL);
            m_Channels.Play(22, 1f, 0f, SHOT_CHANNEL);

            m_Player.Played[0].Loops       = true;
            m_Player.Played[0].PositionNow = 2.5f;
        }

        [Test]
        public void ThenOnlyTheLoopIsKept()
        {
            Assert.AreEqual(1, m_Channels.KeepLooping().Length);
        }

        [Test]
        public void ThenItKeepsItsChannelAndName()
        {
            KeptSound kept = m_Channels.KeepLooping()[0];

            Assert.AreEqual(LOOP_CHANNEL, kept.Channel);
            Assert.AreEqual(LOOP, kept.NameHash);
        }

        [Test]
        public void ThenItKeepsWhereItHadGotTo()
        {
            Assert.AreEqual(2.5f, m_Channels.KeepLooping()[0].Position);
        }

        [Test]
        public void ThenItKeepsWhereAFadeIsGoing()
        {
            m_Channels.FadeVolume(LOOP_CHANNEL, 0.1f, 5f);
            m_Channels.FadePan(LOOP_CHANNEL, 1f, 5f);

            KeptSound kept = m_Channels.KeepLooping()[0];

            Assert.AreEqual(0.1f, kept.Volume);
            Assert.AreEqual(1f, kept.Pan);
        }

        [Test]
        public void ThenALoopAlreadyStoppedIsNotKept()
        {
            m_Channels.Stop(LOOP_CHANNEL);

            Assert.AreEqual(0, m_Channels.KeepLooping().Length);
        }

        [Test]
        public void ThenStartingAgainPicksUpWhereItWas()
        {
            StartAgainInANewModule(out FakeFieldSoundPlayer player, out _);

            Assert.AreEqual(LOOP, player.Played[0].NameHash);
            Assert.AreEqual(2.5f, player.Played[0].StartSeconds);
            Assert.AreEqual(0.5f, player.Played[0].Volume);
            Assert.AreEqual(-0.5f, player.Played[0].Pan);
        }

        [Test]
        public void ThenItCanBeReachedOnItsChannelAgain()
        {
            StartAgainInANewModule(out FakeFieldSoundPlayer player, out FieldSoundChannels channels);

            channels.Stop(LOOP_CHANNEL);

            Assert.AreEqual(1, player.Played[0].StopCount);
        }

        private void StartAgainInANewModule(out FakeFieldSoundPlayer player, out FieldSoundChannels channels)
        {
            KeptSound[] kept = m_Channels.KeepLooping();

            player   = new FakeFieldSoundPlayer();
            channels = new FieldSoundChannels(player);

            channels.StartAgain(kept);
        }
    }
}
