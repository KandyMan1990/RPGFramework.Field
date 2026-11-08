using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Export
{
    /// <summary>An entity that climbs a ladder, with or without a script naming how it climbs.</summary>
    internal sealed class WhenAnEntityClimbsWithoutLadderAnimations
    {
        private const string CLIMB = "CLIMB_LADDER_AND_WAIT true 0 1 0 0 5 0 0 6 0\nRETURN";

        [Test]
        public void ThenExportRefusesIt()
        {
            Assert.AreEqual(1, Problems("RETURN").Count);
        }

        [Test]
        public void ThenANamedLadderIsAccepted()
        {
            Assert.AreEqual(0, Problems("SET_LADDER_ANIMATIONS Idle Idle Idle\nRETURN").Count);
        }

        private static List<string> Problems(string init)
        {
            List<string> problems = new List<string>();

            List<FieldScriptRecord> scripts = new List<FieldScriptRecord>
                                              {
                                                  new FieldScriptRecord(FieldScriptType.Init, string.Empty, init),
                                                  new FieldScriptRecord(FieldScriptType.OnInteraction, string.Empty, CLIMB)
                                              };

            FieldDatabase.ValidateLadderAnimations("Entity", scripts, problems);

            return problems;
        }
    }
}
