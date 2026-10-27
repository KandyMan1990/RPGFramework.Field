using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Catalogue
{
    /// <summary>
    /// The block editor edits the script text rather than keeping a second form, so the text must survive becoming
    /// blocks and text again, or opening a script would damage it.
    /// </summary>
    internal sealed class WhenBlocksAreReadAndWrittenBack
    {
        [TestCase("SET_BYTE $storyProgress 3\nADD_BYTE $gold 100\nWAIT_SECONDS 1.5\nRETURN")]
        [TestCase("SET_BYTE $flag 1\nSOME_FUTURE_OPCODE 1 2 3\nRETURN")]
        [TestCase("ASK_PLAYER_TO_MAKE_A_CHOICE $answer question yes no maybe")]
        [TestCase("LABEL Top\nIF_BYTE $flag == 1\n    LABEL Inside\n    GOTO_JUMP Top\nEND_IF\nGOTO_DIRECTLY Inside")]
        [TestCase("LABEL two names\nRETURN")]
        [TestCase("IF_BYTE $flag == 1\n    SHOW_DIALOGUE_WINDOW hello true\nEND_IF")]
        [TestCase("IF_BYTE $a == 1\n    IF_BYTE $b == 2\n        YIELD\n    END_IF\nEND_IF")]
        [TestCase("IF_BYTE $flag == 1\n    YIELD\nEND_IF\nRETURN")]
        [TestCase("IF_BYTE $flag == 1\n    YIELD\nELSE\n    RETURN\nEND_IF")]
        [TestCase("IF_BYTE $a == 1\n    YIELD\nELSE\n    IF_BYTE $b == 2\n        YIELD\n    ELSE\n        RETURN\n    END_IF\nEND_IF")]
        [TestCase("IF_BYTE $flag == 1\n    YIELD\nELSE\nEND_IF")]
        [TestCase("CROSSFADE_MUSIC \"\" Calm 2 1")]
        public void ThenTheTextIsUnchanged(string script)
        {
            Assert.AreEqual(script, RoundTrip(script));
        }

        [Test]
        public void ThenBlankLinesAreDropped()
        {
            Assert.AreEqual("RETURN", RoundTrip("RETURN\n\n\n"));
        }

        [Test]
        public void ThenAStrayElseKeepsTheRestOfTheScript()
        {
            string written = RoundTrip("ELSE\nRETURN");

            StringAssert.Contains("ELSE", written);
            StringAssert.Contains("RETURN", written);
        }

        [Test]
        public void ThenAStrayEndIfKeepsTheRestOfTheScript()
        {
            StringAssert.Contains("RETURN", RoundTrip("END_IF\nRETURN"));
        }

        private static string RoundTrip(string script)
        {
            return FieldScriptBlocks.ToScriptText(FieldScriptBlocks.Parse(script));
        }
    }
}
