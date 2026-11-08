using NUnit.Framework;

namespace RPGFramework.Field.Tests.Entities
{
    /// <summary>What an entity's record makes of what its init script said, which the field reads once.</summary>
    internal sealed class WhenAnEntitysRecordSaysWhetherItIsInTheField
    {
        private FieldEntityState m_State;

        [SetUp]
        public void SetUp()
        {
            m_State = default;
        }

        [Test]
        public void ThenSayingNothingLeavesItIn()
        {
            Assert.IsFalse(m_State.IsLeftOut);
        }

        [Test]
        public void ThenSayingItIsNotLeavesItOut()
        {
            m_State.SetInField(false);

            Assert.IsTrue(m_State.IsLeftOut);
        }

        [Test]
        public void ThenThinkingBetterOfItLeavesItIn()
        {
            m_State.SetInField(false);
            m_State.SetInField(true);

            Assert.IsFalse(m_State.IsLeftOut);
        }
    }
}
