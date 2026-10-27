using NUnit.Framework;

namespace RPGFramework.Field.Tests.Export
{
    internal sealed class WhenAScriptNamesAudioTheProvidersList
    {
        [Test]
        public void ThenExportAcceptsIt()
        {
            CollectionAssert.IsEmpty(AudioProviders.Problems("PLAY_MUSIC Town Tense",
                                                             "CROSSFADE_MUSIC Battle Fight 2 1",
                                                             "MUSIC_STEM_STATE Town Calm 0",
                                                             "SET_BATTLE_MUSIC Battle",
                                                             "PLAY_SOUND Step"));
        }
    }
}
