using System;
using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Compiler
{
    /// <summary>A channel as a script writes it, which only a number from 0 to 32 can be.</summary>
    internal sealed class WhenASoundChannelIsWritten
    {
        [TestCase(0)]
        [TestCase(32)]
        public void ThenOneInRangeCompiles(int channel)
        {
            Assert.DoesNotThrow(() => FieldScriptCompiler.Compile($"STOP_SOUND {channel}"));
        }

        [Test]
        public void ThenOnePastTheLastIsRefused()
        {
            Assert.Throws<Exception>(() => FieldScriptCompiler.Compile("STOP_SOUND 33"));
        }

        [Test]
        public void ThenAVariableIsRefused()
        {
            Assert.Throws<Exception>(() => FieldScriptCompiler.Compile("STOP_SOUND $TestChoiceAnswer"));
        }

        [Test]
        public void ThenAPlayWithoutItsArgumentsIsRefused()
        {
            Assert.Throws<Exception>(() => FieldScriptCompiler.Compile("PLAY_SOUND Equip"));
        }
    }
}
