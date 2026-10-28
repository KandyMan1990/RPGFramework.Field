using NUnit.Framework;
using RPGFramework.Core.Dialogue.Flows;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Compiler
{
    /// <summary>A question's cancel answer, of three: by number, as whichever is last, or none.</summary>
    internal sealed class WhenACancelAnswerIsWritten
    {
        private const int ANSWERS = 3;

        [Test]
        public void ThenANumberIsThatAnswer()
        {
            Assert.AreEqual(1, FieldScriptCompiler.ParseCancelAnswer("1", ANSWERS, 1));
        }

        [Test]
        public void ThenLastIsTheLastAnswer()
        {
            Assert.AreEqual(2, FieldScriptCompiler.ParseCancelAnswer(FieldScriptCompiler.CANCEL_LAST, ANSWERS, 1));
        }

        [Test]
        public void ThenNoneIsNoAnswer()
        {
            Assert.AreEqual(ChoiceDialogueFlow.NO_CANCEL_ANSWER, FieldScriptCompiler.ParseCancelAnswer(FieldScriptCompiler.CANCEL_NONE, ANSWERS, 1));
        }
    }
}
