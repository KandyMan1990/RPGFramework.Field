using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Catalogue
{
    /// <summary>The opcodes' attributes are what the palette, the compiler and export read, so they must hold together.</summary>
    internal sealed class WhenTheCatalogueIsRead
    {
        [Test]
        public void ThenItsAttributesHaveNoProblems()
        {
            CollectionAssert.IsEmpty(FieldOpCodeCatalogue.Validate());
        }

        [Test]
        public void ThenAnOpcodeIsFoundByItsScriptName()
        {
            Assert.IsTrue(FieldOpCodeCatalogue.TryGet("ADD_BYTE", out _));
        }

        [Test]
        public void ThenAnOpcodeDeclaresItsArguments()
        {
            FieldOpCodeCatalogue.TryGet("ADD_BYTE", out FieldOpCodeInfo info);

            Assert.AreEqual(2, info.Arguments.Count);
        }

        [Test]
        public void ThenAnOpcodeHasASummaryForItsBlock()
        {
            FieldOpCodeCatalogue.TryGet("ADD_BYTE", out FieldOpCodeInfo info);

            Assert.IsNotEmpty(info.Summary);
        }
    }
}
