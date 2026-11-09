using NUnit.Framework;

namespace RPGFramework.Field.Tests.Export
{
    /// <summary>A footstep sound against the sounds the providers list, which it may leave empty for the game's own.</summary>
    internal sealed class WhenAFootstepSoundIsChecked
    {
        [Test]
        public void ThenEmptyIsAccepted()
        {
            Assert.AreEqual(0, AudioProviders.Problems("SET_FOOTSTEP_SOUND \"\" false").Count);
        }

        [Test]
        public void ThenAListedSoundIsAccepted()
        {
            Assert.AreEqual(0, AudioProviders.Problems("SET_FOOTSTEP_SOUND Step false").Count);
        }

        [Test]
        public void ThenAnUnlistedSoundIsRefused()
        {
            Assert.AreEqual(1, AudioProviders.Problems("SET_FOOTSTEP_SOUND Stomp false").Count);
        }
    }
}
