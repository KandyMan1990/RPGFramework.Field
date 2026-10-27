using NUnit.Framework;

namespace RPGFramework.Field.Tests.Export
{
    /// <summary>A track's name mistyped: it compiles, as names are hashed as written, and would fail as it played.</summary>
    internal sealed class WhenAScriptNamesMusicNoProviderLists
    {
        [Test]
        public void ThenExportRefusesIt()
        {
            Assert.AreEqual(1, AudioProviders.Problems("PLAY_MUSIC Twon Calm").Count);
        }
    }
}
