using NUnit.Framework;

namespace RPGFramework.Field.Tests.Export
{
    internal sealed class WhenAScriptNamesASoundNoProviderLists
    {
        [Test]
        public void ThenExportRefusesIt()
        {
            Assert.AreEqual(1, AudioProviders.Problems("PLAY_SOUND Stomp").Count);
        }
    }
}
