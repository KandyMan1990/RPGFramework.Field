using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Export
{
    internal sealed class WhenAScriptNamesAnArena
    {
        private static List<string> Problems(string line)
        {
            List<string> problems = new List<string>();

            FieldDatabase.ValidateArenaNames(new List<string> { "Forest" }, "script", new[] { line }, problems);

            return problems;
        }

        [Test]
        public void ThenExportAcceptsOneADatabaseLists()
        {
            Assert.IsEmpty(Problems("START_BATTLE 3 Forest 0"));
        }

        [Test]
        public void ThenExportRefusesOneNoDatabaseLists()
        {
            Assert.AreEqual(1, Problems("START_BATTLE 3 Cave 0").Count);
        }
    }
}
