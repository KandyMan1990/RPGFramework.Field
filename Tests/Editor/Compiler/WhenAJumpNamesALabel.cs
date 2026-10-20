using System;
using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Compiler
{
    /// <summary>
    /// A jump names a LABEL, which writes nothing and marks where the next instruction starts. A relative jump is
    /// counted from its own end.
    /// </summary>
    internal sealed class WhenAJumpNamesALabel
    {
        [Test]
        public void ThenTheLabelWritesNothing()
        {
            // YIELD (2) and RETURN (2), then GOTO_JUMP (6).
            Assert.AreEqual(10, FieldScriptCompiler.Compile("LABEL Top\nYIELD\nRETURN\nGOTO_JUMP Top").Length);
        }

        [Test]
        public void ThenAJumpBackIsTheDistanceFromItsEnd()
        {
            byte[] back = FieldScriptCompiler.Compile("LABEL Top\nYIELD\nRETURN\nGOTO_JUMP Top");

            Assert.AreEqual(-10, BitConverter.ToInt32(back, 6));
        }

        [Test]
        public void ThenAJumpForwardIsFilledInOnceTheLabelIsRead()
        {
            byte[] ahead = FieldScriptCompiler.Compile("GOTO_JUMP Skip\nYIELD\nLABEL Skip\nRETURN");

            Assert.AreEqual(2, BitConverter.ToInt32(ahead, 2));
        }

        [Test]
        public void ThenAnAbsoluteJumpIsTheLabelsPosition()
        {
            byte[] absolute = FieldScriptCompiler.Compile("YIELD\nLABEL Here\nRETURN\nGOTO_DIRECTLY Here");

            Assert.AreEqual(2, BitConverter.ToInt32(absolute, 6));
        }

        [Test]
        public void ThenAScriptWithNoJumpsIsUnaffected()
        {
            Assert.AreEqual(2 + 7 + 2, FieldScriptCompiler.Compile("YIELD\nWAIT_SECONDS 1.5\nRETURN").Length);
        }
    }
}
