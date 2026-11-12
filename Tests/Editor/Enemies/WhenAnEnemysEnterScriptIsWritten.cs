using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Enemies
{
    internal sealed class WhenAnEnemysEnterScriptIsWritten
    {
        [Test]
        public void ThenItFightsAndThenHides()
        {
            Assert.AreEqual("START_BATTLE Goblins Forest 0\nVISIBILITY false\nRETURN", FieldEntityBodyBuilder.EnemyEnterScript("Goblins", "Forest"));
        }

        [Test]
        public void ThenANameWithASpaceIsQuoted()
        {
            Assert.AreEqual("START_BATTLE Goblins \"Dark Forest\" 0\nVISIBILITY false\nRETURN", FieldEntityBodyBuilder.EnemyEnterScript("Goblins", "Dark Forest"));
        }

        [Test]
        public void ThenWithNothingToPickTheNamesAreLeftEmpty()
        {
            Assert.AreEqual("START_BATTLE \"\" \"\" 0\nVISIBILITY false\nRETURN", FieldEntityBodyBuilder.EnemyEnterScript(string.Empty, string.Empty));
        }

        [Test]
        public void ThenItCompiles()
        {
            Assert.DoesNotThrow(() => FieldScriptCompiler.Compile(FieldEntityBodyBuilder.EnemyEnterScript("Goblins", "Dark Forest")));
        }
    }
}
