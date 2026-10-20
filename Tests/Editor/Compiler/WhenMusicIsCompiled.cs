using System;
using NUnit.Framework;
using RPGFramework.Field.Editor;
using RPGFramework.Hashing;

namespace RPGFramework.Field.Tests.Compiler
{
    /// <summary>
    /// Songs, sounds and stem states are named, and written as their names' hashes, so reordering what lists them
    /// repoints nothing.
    /// </summary>
    internal sealed class WhenMusicIsCompiled
    {
        [Test]
        public void ThenPlayingASongNamesItAndAStemState()
        {
            byte[] music = FieldScriptCompiler.Compile("PLAY_MUSIC MusicSample AllStems");

            Assert.AreEqual(2 + 8 + 8, music.Length);
            Assert.AreEqual(Fnv1a64.Hash("MusicSample"), BitConverter.ToUInt64(music, 2));
            Assert.AreEqual(Fnv1a64.Hash("AllStems"), BitConverter.ToUInt64(music, 10));
        }

        [Test]
        public void ThenAStemStateChangeIsItsNamesHashThenItsFade()
        {
            byte[] stemState = FieldScriptCompiler.Compile("MUSIC_STEM_STATE MusicSample Combat 1.5");

            Assert.AreEqual(15, stemState.Length);
            Assert.AreEqual(Fnv1a64.Hash("Combat"), BitConverter.ToUInt64(stemState, 2));
            Assert.AreEqual(1.5f, BitConverter.ToSingle(stemState, 11));
        }

        [Test]
        public void ThenTheSongAStemStateChangeNamesIsForTheEditorAlone()
        {
            CollectionAssert.AreEqual(FieldScriptCompiler.Compile("MUSIC_STEM_STATE MusicSample Combat 1.5"),
                                      FieldScriptCompiler.Compile("MUSIC_STEM_STATE SomethingElse Combat 1.5"));
        }

        [Test]
        public void ThenACrossfadeNamesItsSongAndStateAsPlayingDoes()
        {
            byte[] music     = FieldScriptCompiler.Compile("PLAY_MUSIC MusicSample AllStems");
            byte[] crossfade = FieldScriptCompiler.Compile("CROSSFADE_MUSIC MusicSample AllStems 2.5 0.25");

            Assert.AreEqual(27, crossfade.Length);
            Assert.AreEqual(BitConverter.ToString(music, 2, 16), BitConverter.ToString(crossfade, 2, 16));
        }

        [Test]
        public void ThenACrossfadesSecondsAndVolumeShareASourcesByte()
        {
            byte[] crossfade = FieldScriptCompiler.Compile("CROSSFADE_MUSIC MusicSample AllStems 2.5 0.25");

            CollectionAssert.AreEqual(new[] { 2.5f, 0.25f }, new[] { BitConverter.ToSingle(crossfade, 19), BitConverter.ToSingle(crossfade, 23) });
        }

        [Test]
        public void ThenSettingTheVolumeIsASourcesByteAndAFloat()
        {
            byte[] volume = FieldScriptCompiler.Compile("SET_MUSIC_VOLUME 0.5");

            Assert.AreEqual(7, volume.Length);
            Assert.AreEqual(0.5f, BitConverter.ToSingle(volume, 3));
        }

        [Test]
        public void ThenFadingTheVolumeIsASourcesByteAndTwoFloats()
        {
            byte[] fade = FieldScriptCompiler.Compile("FADE_MUSIC_VOLUME 0 2");

            Assert.AreEqual(11, fade.Length);
            Assert.AreEqual(2f, BitConverter.ToSingle(fade, 7));
        }

        [Test]
        public void ThenAFadeFromOneVolumeIsTwoSourcesBytesAndThreeFloats()
        {
            byte[] fadeFrom = FieldScriptCompiler.Compile("FADE_MUSIC_VOLUME_FROM 0 0.5 2");

            Assert.AreEqual(16, fadeFrom.Length);
            CollectionAssert.AreEqual(new[] { 0.5f, 2f }, new[] { BitConverter.ToSingle(fadeFrom, 7), BitConverter.ToSingle(fadeFrom, 12) });
        }

        [TestCase("WAIT_FOR_MUSIC_VOLUME")]
        [TestCase("STOP_MUSIC")]
        public void ThenWaitingAndStoppingTakeNothing(string source)
        {
            Assert.AreEqual(2, FieldScriptCompiler.Compile(source).Length);
        }

        [Test]
        public void ThenASoundIsItsNamesHash()
        {
            byte[] sound = FieldScriptCompiler.Compile("PLAY_SOUND Equip");

            Assert.AreEqual(2 + 8, sound.Length);
            Assert.AreEqual(Fnv1a64.Hash("Equip"), BitConverter.ToUInt64(sound, 2));
        }
    }
}
