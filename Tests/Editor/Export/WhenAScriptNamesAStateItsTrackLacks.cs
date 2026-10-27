using NUnit.Framework;

namespace RPGFramework.Field.Tests.Export
{
    /// <summary>A state left behind when its block's track changed, or one typed for the wrong track.</summary>
    internal sealed class WhenAScriptNamesAStateItsTrackLacks
    {
        [Test]
        public void ThenExportRefusesIt()
        {
            Assert.AreEqual(1, AudioProviders.Problems("PLAY_MUSIC Battle Calm").Count);
        }
    }
}
