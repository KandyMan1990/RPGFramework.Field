using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Catalogue
{
    /// <summary>A question of three answers, starting and cancelling on its third, loses one.</summary>
    internal sealed class WhenAQuestionsAnswersChange
    {
        private FieldScriptBlock m_Block;

        [SetUp]
        public void SetUp()
        {
            FieldOpCodeCatalogue.TryGet("ASK_PLAYER_TO_MAKE_A_CHOICE", out FieldOpCodeInfo info);

            m_Block = FieldScriptBlock.CreateEmpty(info);
            m_Block.SetArgument(3, "2");
            m_Block.SetArgument(4, "2");
            m_Block.SetArgument(5, "Sheet/Stay Sheet/Go Sheet/Wait");

            m_Block.SetArgument(5, "Sheet/Stay Sheet/Go");
            m_Block.KeepChoicesOffered(new Choices());
        }

        [Test]
        public void ThenAStartingAnswerItNoLongerHasMovesToTheFirst()
        {
            Assert.AreEqual("0", m_Block.Arguments[3]);
        }

        [Test]
        public void ThenACancelAnswerItNoLongerHasMovesToTheLast()
        {
            Assert.AreEqual(FieldScriptCompiler.CANCEL_LAST, m_Block.Arguments[4]);
        }
    }
}
