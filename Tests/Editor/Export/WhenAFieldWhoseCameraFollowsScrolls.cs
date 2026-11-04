using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Export
{
    /// <summary>A scroll in a field seen through a follow camera.</summary>
    internal sealed class WhenAFieldWhoseCameraFollowsScrolls
    {
        [Test]
        public void ThenExportTakesIt()
        {
            List<string> problems = new List<string>();

            FieldDatabase.ValidateScrollLines(true, "script", new[] { "SCROLL_SCREEN_TO_ENTITY 3 1 1" }, problems);

            CollectionAssert.IsEmpty(problems);
        }
    }
}
