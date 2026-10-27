using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Catalogue
{
    /// <summary>A <c>JUMP_TO_MAP</c> to the Village's spawn point 2 is pointed at the Cave instead.</summary>
    internal sealed class WhenAJumpsFieldChanges
    {
        private Choices          m_Choices;
        private FieldScriptBlock m_Block;

        [SetUp]
        public void SetUp()
        {
            FieldOpCodeCatalogue.TryGet("JUMP_TO_MAP", out FieldOpCodeInfo info);

            m_Choices = new Choices();
            m_Block   = FieldScriptBlock.CreateEmpty(info);
            m_Block.PickFirstChoices(m_Choices);
            m_Block.SetArgument(1, "2");

            m_Block.SetArgument(0, "Cave");
            m_Block.KeepChoicesOffered(m_Choices);
        }

        [Test]
        public void ThenASpawnPointTheNewFieldLacksMovesToItsFirst()
        {
            Assert.AreEqual("5", m_Block.Arguments[1]);
        }
    }
}
