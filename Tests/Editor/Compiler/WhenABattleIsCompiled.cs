using System;
using NUnit.Framework;
using RPGFramework.Field.Editor;
using RPGFramework.Hashing;

namespace RPGFramework.Field.Tests.Compiler
{
    /// <summary>Each battle opcode reads only its own flags: a battle's on START_BATTLE, the game-over rules on the mode.</summary>
    internal sealed class WhenABattleIsCompiled
    {
        [Test]
        public void ThenABattleWithItsOwnFlagsCompiles()
        {
            Assert.AreEqual(19, FieldScriptCompiler.Compile("START_BATTLE 0 3 129 15 \"\"").Length);
        }

        [Test]
        public void ThenABattleNamingNoTransitionLeavesItToTheRouter()
        {
            Assert.AreEqual(0UL, BitConverter.ToUInt64(FieldScriptCompiler.Compile("START_BATTLE 0 3 0 15 \"\""), 11));
        }

        [Test]
        public void ThenALineFromBeforeTransitionsLeavesItToTheRouter()
        {
            byte[] older = FieldScriptCompiler.Compile("START_BATTLE 0 3 0 15");

            Assert.AreEqual(19, older.Length);
            Assert.AreEqual(0UL, BitConverter.ToUInt64(older, 11));
        }

        [Test]
        public void ThenABattleNamingItsTransitionWritesItsNamesHash()
        {
            Assert.AreEqual(Fnv1a64.Hash("BossSwirl"), BitConverter.ToUInt64(FieldScriptCompiler.Compile("START_BATTLE 0 3 0 15 BossSwirl"), 11));
        }

        [Test]
        public void ThenTheBattleModesGameOverRulesCompile()
        {
            Assert.AreEqual(5, FieldScriptCompiler.Compile("SET_BATTLE_MODE 768").Length);
        }
    }
}
