using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Catalogue
{
    /// <summary>The block editor gives a new block's names the first choice each of its pickers offers.</summary>
    internal sealed class WhenANewBlockTakesItsFirstChoices
    {
        [Test]
        public void ThenItsTrackIsTheFirstOffered()
        {
            Assert.AreEqual("Town", New("PLAY_MUSIC").Arguments[0]);
        }

        [Test]
        public void ThenItsStateIsItsTracksFirst()
        {
            Assert.AreEqual("Calm", New("PLAY_MUSIC").Arguments[1]);
        }

        [Test]
        public void ThenItsSoundIsTheFirstOffered()
        {
            Assert.AreEqual("Step", New("PLAY_SOUND").Arguments[0]);
        }

        [Test]
        public void ThenItsSpawnPointIsItsFieldsFirst()
        {
            Assert.AreEqual("1", New("JUMP_TO_MAP").Arguments[1]);
        }

        [Test]
        public void ThenANumberStaysAtItsDefault()
        {
            Assert.AreEqual("1", New("CROSSFADE_MUSIC").Arguments[3]);
        }

        private static FieldScriptBlock New(string scriptName)
        {
            FieldOpCodeCatalogue.TryGet(scriptName, out FieldOpCodeInfo info);

            FieldScriptBlock block = FieldScriptBlock.CreateEmpty(info);
            block.PickFirstChoices(new Choices());

            return block;
        }
    }
}
