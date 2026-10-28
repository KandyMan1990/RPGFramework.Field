using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Catalogue
{
    internal sealed class WhenANewBlockIsMade
    {
        [Test]
        public void ThenItHasAnInputPerArgument()
        {
            Assert.AreEqual(2, New("ADD_BYTE").Arguments.Count);
        }

        [Test]
        public void ThenItWritesItsScriptName()
        {
            StringAssert.StartsWith("ADD_BYTE", New("ADD_BYTE").ToLine());
        }

        [Test]
        public void ThenAnArgumentWithADefaultStartsAtIt()
        {
            StringAssert.EndsWith(" 1", New("CROSSFADE_MUSIC").ToLine());
        }

        [Test]
        public void ThenAQuestionStartsOnItsFirstAnswer()
        {
            Assert.AreEqual("0", New("ASK_PLAYER_TO_MAKE_A_CHOICE").Arguments[3]);
        }

        [Test]
        public void ThenAQuestionCancelsToItsLastAnswer()
        {
            Assert.AreEqual(FieldScriptCompiler.CANCEL_LAST, New("ASK_PLAYER_TO_MAKE_A_CHOICE").Arguments[4]);
        }

        [Test]
        public void ThenANameStartsWithNothingChosen()
        {
            Assert.AreEqual(string.Empty, New("PLAY_SOUND").Arguments[0]);
        }

        [Test]
        public void ThenAnArgumentWithNoDefaultStartsAtItsTypes()
        {
            Assert.AreEqual("FADE_MUSIC_VOLUME 0 0", New("FADE_MUSIC_VOLUME").ToLine());
        }

        private static FieldScriptBlock New(string scriptName)
        {
            FieldOpCodeCatalogue.TryGet(scriptName, out FieldOpCodeInfo info);

            return FieldScriptBlock.CreateEmpty(info);
        }
    }
}
