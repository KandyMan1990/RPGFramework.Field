using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Export
{
    /// <summary>An init script says whether its entity is in the field.</summary>
    internal sealed class WhenEntityInFieldIsInAnInit
    {
        [Test]
        public void ThenExportAcceptsIt()
        {
            List<string> problems = new List<string>();

            FieldDatabase.ValidateEntityInField(FieldScriptType.Init, "script", new[] { "ENTITY_IN_FIELD false" }, problems);

            Assert.AreEqual(0, problems.Count);
        }
    }
}
