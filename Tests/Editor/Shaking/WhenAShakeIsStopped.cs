using NUnit.Framework;

namespace RPGFramework.Field.Tests.Shaking
{
    /// <summary>A shake 10 across in one-second swings, stopped partway through a swing out to 5.</summary>
    internal sealed class WhenAShakeIsStopped
    {
        private const float TOLERANCE = 0.001f;

        private ScreenShake m_Shake;

        [SetUp]
        public void SetUp()
        {
            m_Shake = default;
            m_Shake.Start(10f, 1f, 0f, 0f);
            m_Shake.Tick(0f, 0.5f, 0f);
            m_Shake.Tick(0.5f, 0f, 0f);
            m_Shake.Stop();
        }

        [Test]
        public void ThenItFinishesTheSwing()
        {
            m_Shake.Tick(0.25f, 0f, 0f);

            Assert.Greater(m_Shake.Offset.x, 2.5f);
            Assert.IsFalse(m_Shake.IsStill);
        }

        [Test]
        public void ThenItComesBackOverOneMore()
        {
            m_Shake.Tick(0.5f, 0f, 0f);
            m_Shake.Tick(0.5f, 0f, 0f);

            Assert.AreEqual(2.5f, m_Shake.Offset.x, TOLERANCE);
            Assert.IsFalse(m_Shake.IsStill);
        }

        [Test]
        public void ThenItEndsAtRestAndStill()
        {
            m_Shake.Tick(0.5f, 0f, 0f);
            m_Shake.Tick(1f, 0f, 0f);

            Assert.AreEqual(0f, m_Shake.Offset.x, TOLERANCE);
            Assert.IsTrue(m_Shake.IsStill);
        }

        [Test]
        public void ThenShakingAgainStartsFromWhereItHasGot()
        {
            m_Shake.Tick(0.5f, 0f, 0f);
            m_Shake.Tick(0.5f, 0f, 0f);
            m_Shake.Start(10f, 1f, 0f, 0f);
            m_Shake.Tick(0f, 1f, 0f);

            Assert.AreEqual(2.5f, m_Shake.Offset.x, TOLERANCE);
        }
    }
}
