using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Export
{
    /// <summary>A scroll in a field seen through a fixed camera, which a scroll cannot move.</summary>
    internal sealed class WhenAFieldWhoseCameraDoesNotFollowScrolls
    {
        [Test]
        public void ThenExportRefusesIt()
        {
            List<string> problems = new List<string>();

            FieldDatabase.ValidateScrollLines(false, "script", new[] { "SCROLL_SCREEN_TO_POSITION 0.5 0.5 0 1 0", "WAIT_FOR_SCROLLING" }, problems);

            Assert.AreEqual(1, problems.Count);
        }
    }
}
