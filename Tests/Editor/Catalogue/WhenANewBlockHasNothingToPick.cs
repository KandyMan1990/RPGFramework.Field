using System;
using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Catalogue
{
    /// <summary>A project with no music yet: a new block's names are left with nothing chosen.</summary>
    internal sealed class WhenANewBlockHasNothingToPick
    {
        [Test]
        public void ThenItsNamesStayEmpty()
        {
            FieldOpCodeCatalogue.TryGet("PLAY_MUSIC", out FieldOpCodeInfo info);

            FieldScriptBlock block = FieldScriptBlock.CreateEmpty(info);
            block.PickFirstChoices(new NothingOffered());

            CollectionAssert.AreEqual(new[] { string.Empty, string.Empty }, block.Arguments);
        }

        private sealed class NothingOffered : IArgumentChoiceProvider
        {
            IReadOnlyList<string> IArgumentChoiceProvider.ChoicesFor(FieldScriptBlock block, int argumentIndex)
            {
                return Array.Empty<string>();
            }
        }
    }
}
