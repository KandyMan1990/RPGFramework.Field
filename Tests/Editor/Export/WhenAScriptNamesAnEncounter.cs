using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Export
{
    internal sealed class WhenAScriptNamesAnEncounter
    {
        private static List<string> Problems(string line)
        {
            List<string> problems = new List<string>();

            FieldDatabase.ValidateEncounterNames(new List<string> { "Goblins" }, "script", new[] { line }, problems);

            return problems;
        }

        [Test]
        public void ThenExportAcceptsOneADatabaseLists()
        {
            Assert.IsEmpty(Problems("START_BATTLE Goblins Forest 0"));
        }

        [Test]
        public void ThenExportRefusesOneNoDatabaseLists()
        {
            Assert.AreEqual(1, Problems("START_BATTLE Dragons Forest 0").Count);
        }
    }
}
