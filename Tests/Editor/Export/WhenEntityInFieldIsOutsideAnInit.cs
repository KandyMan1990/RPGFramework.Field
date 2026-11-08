using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Export
{
    /// <summary>A Default script says whether its entity is in the field, which is read only as init ends.</summary>
    internal sealed class WhenEntityInFieldIsOutsideAnInit
    {
        [Test]
        public void ThenExportRefusesIt()
        {
            List<string> problems = new List<string>();

            FieldDatabase.ValidateEntityInField(FieldScriptType.Default, "script", new[] { "ENTITY_IN_FIELD false" }, problems);

            Assert.AreEqual(1, problems.Count);
        }
    }
}
