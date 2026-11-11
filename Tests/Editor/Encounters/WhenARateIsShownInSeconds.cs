using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Encounters
{
    /// <summary>The Encounters tab's seconds, counted as a config is made, turned back into the checks a battle takes.</summary>
    internal sealed class WhenARateIsShownInSeconds
    {
        private const float TOLERANCE = 0.01f;

        private FieldEncounterConfig m_Config;

        [SetUp]
        public void SetUp()
        {
            m_Config = EncounterTable.Config();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Config);
        }

        [Test]
        public void ThenWalkingTakesAboutFourteenChecks()
        {
            Assert.AreEqual(13.855f, Checks(m_Config.DangerPerCheckWalking), TOLERANCE);
        }

        [Test]
        public void ThenRunningTakesAboutNine()
        {
            Assert.AreEqual(8.649f, Checks(m_Config.DangerPerCheckRunning), TOLERANCE);
        }

        [Test]
        public void ThenADangerStepOfEveryRollTakesOneCheck()
        {
            Assert.AreEqual(1f, Checks(FieldEncounterConfig.ROLLS), TOLERANCE);
        }

        [Test]
        public void ThenTwiceTheRateTakesHalfTheTime()
        {
            float once  = EncounterMeter.AverageSeconds(m_Config, 16, m_Config.DangerPerCheckWalking);
            float twice = EncounterMeter.AverageSeconds(m_Config, 32, m_Config.DangerPerCheckWalking);

            Assert.AreEqual(once / 2f, twice, TOLERANCE);
        }

        private float Checks(int dangerPerCheck)
        {
            return EncounterMeter.AverageSeconds(m_Config, 1, dangerPerCheck) * m_Config.ChecksPerSecondAtRateOne;
        }
    }
}
