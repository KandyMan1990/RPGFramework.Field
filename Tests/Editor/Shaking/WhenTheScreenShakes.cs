using NUnit.Framework;

namespace RPGFramework.Field.Tests.Shaking
{
    /// <summary>A shake 10 across in one-second swings and none down, its first swing started.</summary>
    internal sealed class WhenTheScreenShakes
    {
        private const float TOLERANCE = 0.001f;

        private ScreenShake m_Shake;

        [SetUp]
        public void SetUp()
        {
            m_Shake = default;
            m_Shake.Start(10f, 1f, 0f, 0f);
            m_Shake.Tick(0f, 0.5f, 0.5f);
        }

        [Test]
        public void ThenOneNeverStartedIsStill()
        {
            ScreenShake shake = default;

            Assert.IsTrue(shake.IsStill);
        }

        [Test]
        public void ThenItIsNotStill()
        {
            Assert.IsFalse(m_Shake.IsStill);
        }

        [Test]
        public void ThenItBeginsFromRest()
        {
            Assert.AreEqual(0f, m_Shake.Offset.x, TOLERANCE);
        }

        [Test]
        public void ThenASwingGoesThePartOfTheAmplitudeRolled()
        {
            m_Shake.Tick(0.5f, 0f, 0f);

            Assert.AreEqual(2.5f, m_Shake.Offset.x, TOLERANCE);
        }

        [Test]
        public void ThenASwingEndsWhereItWasGoing()
        {
            m_Shake.Tick(1f, 1f, 0f);

            Assert.AreEqual(5f, m_Shake.Offset.x, TOLERANCE);
        }

        [Test]
        public void ThenTheNextSwingGoesToTheOtherSide()
        {
            m_Shake.Tick(1f, 1f, 0f);
            m_Shake.Tick(0.5f, 0f, 0f);

            Assert.AreEqual(-2.5f, m_Shake.Offset.x, TOLERANCE);
        }

        [Test]
        public void ThenAnAxisWithNoAmplitudeStaysPut()
        {
            m_Shake.Tick(1f, 1f, 1f);

            Assert.AreEqual(0f, m_Shake.Offset.y, TOLERANCE);
        }

        [Test]
        public void ThenChangingItWaitsForTheNextSwing()
        {
            m_Shake.Start(40f, 1f, 0f, 0f);
            m_Shake.Tick(0.5f, 0f, 0f);

            Assert.AreEqual(2.5f, m_Shake.Offset.x, TOLERANCE);
        }

        [Test]
        public void ThenTheChangeIsInTheNextSwing()
        {
            m_Shake.Start(40f, 1f, 0f, 0f);
            m_Shake.Tick(1f, 1f, 0f);
            m_Shake.Tick(1f, 0f, 0f);

            Assert.AreEqual(-40f, m_Shake.Offset.x, TOLERANCE);
        }
    }
}
