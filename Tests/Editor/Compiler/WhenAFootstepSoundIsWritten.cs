using System;
using NUnit.Framework;
using RPGFramework.Field.Editor;
using RPGFramework.Hashing;

namespace RPGFramework.Field.Tests.Compiler
{
    /// <summary>The name of an entity's footsteps, which can be left empty for the game's own.</summary>
    internal sealed class WhenAFootstepSoundIsWritten
    {
        [Test]
        public void ThenANameIsItsHash()
        {
            byte[] line = FieldScriptCompiler.Compile("SET_FOOTSTEP_SOUND Steps false");

            Assert.AreEqual(Fnv1a64.Hash("Steps"), BitConverter.ToUInt64(line, 2));
        }

        [Test]
        public void ThenEmptyIsZero()
        {
            byte[] line = FieldScriptCompiler.Compile("SET_FOOTSTEP_SOUND \"\" false");

            Assert.AreEqual(0UL, BitConverter.ToUInt64(line, 2));
        }
    }
}
