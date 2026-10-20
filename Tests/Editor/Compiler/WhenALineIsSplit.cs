using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Compiler
{
    /// <summary>Every reader of script text splits a line the same way: on spaces, but not inside double quotes.</summary>
    internal sealed class WhenALineIsSplit
    {
        [TestCase("PLAY_MUSIC \"Town Theme\" AllStems",  "PLAY_MUSIC|Town Theme|AllStems")]
        [TestCase("  WAIT_SECONDS   1.5 ",               "WAIT_SECONDS|1.5")]
        [TestCase("PLAY_SOUND Bob\"s",                   "PLAY_SOUND|Bob\"s")]
        [TestCase("PLAY_SOUND \"a \\\"b\\\\\"",          "PLAY_SOUND|a \"b\\")]
        [TestCase("PLAY_SOUND \"\" 1",                   "PLAY_SOUND||1")]
        public void ThenItsPartsAreAsWritten(string line, string parts)
        {
            Assert.AreEqual(parts, string.Join("|", FieldScriptLine.Split(line)));
        }

        [Test]
        public void ThenABlankLineHasAnEmptyScriptName()
        {
            CollectionAssert.AreEqual(new[] { string.Empty }, FieldScriptLine.Split("   "));
        }

        [TestCase("PLAY_SOUND \"Town Theme", "never closed")]
        [TestCase("PLAY_SOUND \"Town\"Theme", "Leave a space")]
        public void ThenAQuoteItCannotReadIsAProblem(string line, string why)
        {
            bool split = FieldScriptLine.TrySplit(line, out _, out string problem);

            Assert.IsFalse(split);
            StringAssert.Contains(why, problem);
        }
    }
}
