using System;
using NUnit.Framework;
using RPGFramework.Field.Editor;
using RPGFramework.Hashing;

namespace RPGFramework.Field.Tests.Compiler
{
    /// <summary>Each battle opcode reads only its own flags: a battle's on START_BATTLE, the game-over rules on the mode.</summary>
    internal sealed class WhenABattleIsCompiled
    {
        private const int ARENA      = 5;
        private const int TRANSITION = 15;
        private const int LENGTH     = 23;

        [Test]
        public void ThenABattleWithItsOwnFlagsCompiles()
        {
            Assert.AreEqual(LENGTH, FieldScriptCompiler.Compile("START_BATTLE 3 Forest 129 \"\"").Length);
        }

        [Test]
        public void ThenItWritesItsArenasNameHash()
        {
            Assert.AreEqual(Fnv1a64.Hash("Forest"), BitConverter.ToUInt64(FieldScriptCompiler.Compile("START_BATTLE 3 Forest 0"), ARENA));
        }

        [Test]
        public void ThenABattleNamingNoTransitionLeavesItToTheRouter()
        {
            Assert.AreEqual(0UL, BitConverter.ToUInt64(FieldScriptCompiler.Compile("START_BATTLE 3 Forest 0 \"\""), TRANSITION));
        }

        [Test]
        public void ThenALineLeavingOffItsTransitionLeavesItToTheRouter()
        {
            byte[] line = FieldScriptCompiler.Compile("START_BATTLE 3 Forest 0");

            Assert.AreEqual(LENGTH, line.Length);
            Assert.AreEqual(0UL, BitConverter.ToUInt64(line, TRANSITION));
        }

        [Test]
        public void ThenABattleNamingItsTransitionWritesItsNamesHash()
        {
            Assert.AreEqual(Fnv1a64.Hash("BossSwirl"), BitConverter.ToUInt64(FieldScriptCompiler.Compile("START_BATTLE 3 Forest 0 BossSwirl"), TRANSITION));
        }

        [Test]
        public void ThenTheBattleModesGameOverRulesCompile()
        {
            Assert.AreEqual(5, FieldScriptCompiler.Compile("SET_BATTLE_MODE 768").Length);
        }

        [Test]
        public void ThenTheBattleModeTakesRandomBattlesFlags()
        {
            Assert.AreEqual(5, FieldScriptCompiler.Compile("SET_BATTLE_MODE 255").Length);
        }
    }
}
