using NUnit.Framework;

namespace RPGFramework.Field.Tests.Export
{
    /// <summary>
    /// <c>MUSIC_STEM_STATE</c> names a track only to narrow the states offered, and applies to whatever is playing, so
    /// with the track left empty its state may be any track's.
    /// </summary>
    internal sealed class WhenAStemStateNamesNoTrack
    {
        [Test]
        public void ThenAnyTracksStateIsAccepted()
        {
            CollectionAssert.IsEmpty(AudioProviders.Problems("MUSIC_STEM_STATE \"\" Fight 0"));
        }

        [Test]
        public void ThenAStateNoTrackHasIsRefused()
        {
            Assert.AreEqual(1, AudioProviders.Problems("MUSIC_STEM_STATE \"\" Loud 0").Count);
        }
    }
}
