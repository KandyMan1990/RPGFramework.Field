using System.Threading;
using NUnit.Framework;

namespace RPGFramework.Field.Tests.Sounds
{
    /// <summary>A sound on channel 1 that has not finished loading, with continuations running as it finishes.</summary>
    internal sealed class WhenASoundIsStillLoading
    {
        private const byte CHANNEL = 1;

        private SynchronizationContext m_Context;
        private FakeFieldSoundPlayer   m_Player;
        private FieldSoundChannels     m_Channels;

        [SetUp]
        public void SetUp()
        {
            m_Context = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);

            m_Player   = new FakeFieldSoundPlayer { Loading = true };
            m_Channels = new FieldSoundChannels(m_Player);

            m_Channels.Play(11, 1f, 0f, CHANNEL);
        }

        [TearDown]
        public void TearDown()
        {
            SynchronizationContext.SetSynchronizationContext(m_Context);
        }

        [Test]
        public void ThenAFadeWaitsForIt()
        {
            m_Channels.FadeVolume(CHANNEL, 0.25f, 1f);

            Assert.AreEqual(1f, m_Player.Played[0].Volume);
        }

        [Test]
        public void ThenAFadeHappensOnceItHasLoaded()
        {
            m_Channels.FadeVolume(CHANNEL, 0.25f, 1f);
            m_Player.FinishLoading();

            Assert.AreEqual(0.25f, m_Player.Played[0].Volume);
        }

        [Test]
        public void ThenAStopHappensOnceItHasLoaded()
        {
            m_Channels.Stop(CHANNEL);
            m_Player.FinishLoading();

            Assert.AreEqual(1, m_Player.Played[0].StopCount);
        }

        [Test]
        public void ThenItIsNotKept()
        {
            m_Player.Played[0].Loops = true;

            Assert.AreEqual(0, m_Channels.KeepLooping().Length);
        }
    }
}
