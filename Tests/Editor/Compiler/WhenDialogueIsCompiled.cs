using System;
using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Compiler
{
    internal sealed class WhenDialogueIsCompiled
    {
        [Test]
        public void ThenALineNamesItsChannelBeforeItsKey()
        {
            byte[] show = FieldScriptCompiler.Compile("SHOW_DIALOGUE_WINDOW 3 Test/Line true");

            Assert.AreEqual(2 + 1 + 8 + 1 + 1, show.Length);
            Assert.AreEqual(3, show[2]);
        }

        [Test]
        public void ThenAWindowStyleIsWrittenByNameAndStoredAsItsNumber()
        {
            byte[] thought = FieldScriptCompiler.Compile("SET_DIALOGUE_WINDOW_STYLE 2 Thought");

            CollectionAssert.AreEqual(new byte[] { 2, 1 }, new[] { thought[2], thought[3] });
        }

        [Test]
        public void ThenAMessageVariableNamesItsSlotThenAnInt()
        {
            byte[] variable = FieldScriptCompiler.Compile("SET_MESSAGE_VARIABLE 7 1500");

            Assert.AreEqual(2 + 1 + 1 + 4, variable.Length);
            Assert.AreEqual(7, variable[2]);
            Assert.AreEqual(1500, BitConverter.ToInt32(variable, 4));
        }

        [Test]
        public void ThenAMessageSpeedNamesItsChannelThenAFloat()
        {
            byte[] speed = FieldScriptCompiler.Compile("SET_MESSAGE_SPEED 2 45");

            Assert.AreEqual(2 + 1 + 1 + 4, speed.Length);
            Assert.AreEqual(2, speed[2]);
            Assert.AreEqual(45f, BitConverter.ToSingle(speed, 4));
        }
    }
}
