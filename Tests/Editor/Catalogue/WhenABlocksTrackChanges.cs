using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Catalogue
{
    /// <summary>A <c>PLAY_MUSIC</c> block on Town's Calm state is given another track.</summary>
    internal sealed class WhenABlocksTrackChanges
    {
        private Choices          m_Choices;
        private FieldScriptBlock m_Block;

        [SetUp]
        public void SetUp()
        {
            FieldOpCodeCatalogue.TryGet("PLAY_MUSIC", out FieldOpCodeInfo info);

            m_Choices = new Choices();
            m_Block   = FieldScriptBlock.CreateEmpty(info);
            m_Block.PickFirstChoices(m_Choices);
        }

        [Test]
        public void ThenAStateTheNewTrackHasStays()
        {
            m_Block.SetArgument(0, "Night");
            m_Block.KeepChoicesOffered(m_Choices);

            Assert.AreEqual("Calm", m_Block.Arguments[1]);
        }

        [Test]
        public void ThenAStateTheNewTrackLacksMovesToItsFirst()
        {
            m_Block.SetArgument(0, "Battle");
            m_Block.KeepChoicesOffered(m_Choices);

            Assert.AreEqual("Fight", m_Block.Arguments[1]);
        }
    }
}
