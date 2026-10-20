using System;
using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Field.Editor;
using RPGFramework.Hashing;

namespace RPGFramework.Field.Tests.Compiler
{
    /// <summary>Unity's own default names have spaces, so a script writes such a name in double quotes.</summary>
    internal sealed class WhenANameHasASpace
    {
        private const string SCRIPT = "IF_BOOL true == true\n    PLAY_MUSIC \"Town Theme\" AllStems\nEND_IF\n" +
                                      "ASK_PLAYER_TO_MAKE_A_CHOICE $choice 0 Shop/Question \"Shop/Buy now\" Shop/Leave";

        [Test]
        public void ThenItIsHashedWithoutItsQuotes()
        {
            byte[] spaced = FieldScriptCompiler.Compile("PLAY_MUSIC \"Music Sample\" \"All Stems\"");

            Assert.AreEqual(Fnv1a64.Hash("Music Sample"), BitConverter.ToUInt64(spaced, 2));
            Assert.AreEqual(Fnv1a64.Hash("All Stems"), BitConverter.ToUInt64(spaced, 10));
        }

        [Test]
        public void ThenBlocksWriteItBackAsTheyReadIt()
        {
            Assert.AreEqual(SCRIPT, FieldScriptBlocks.ToScriptText(FieldScriptBlocks.Parse(SCRIPT)));
        }

        [Test]
        public void ThenABlockHoldsItWithoutItsQuotes()
        {
            List<FieldScriptBlock> blocks = FieldScriptBlocks.Parse(SCRIPT);

            Assert.AreEqual("Town Theme", blocks[0].Children[0].Arguments[0]);
        }
    }
}
