using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Compiler
{
    /// <summary>A name is written in double quotes where it needs them, so it reads back as itself.</summary>
    internal sealed class WhenANameIsWritten
    {
        [TestCase("plain")]
        [TestCase("Town Theme")]
        [TestCase("\"quoted")]
        [TestCase("a \"b\\ c")]
        [TestCase("")]
        [TestCase("Bob\"s")]
        public void ThenItReadsBackAsItself(string name)
        {
            string line = "PLAY_SOUND " + FieldScriptLine.Quote(name);

            Assert.IsTrue(FieldScriptLine.TrySplit(line, out string[] parts, out _));
            Assert.AreEqual(name, parts[1]);
        }

        [Test]
        public void ThenANameWithNoSpaceIsWrittenAsItIs()
        {
            Assert.AreEqual("MusicSample", FieldScriptLine.Quote("MusicSample"));
        }
    }
}
