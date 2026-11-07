using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.GameOpCodes
{
    /// <summary>
    /// Exported scripts hold the opcodes' values, so export records the table it compiled with and a player build is
    /// refused once it has changed, the framework's or the game's.
    /// </summary>
    internal sealed class WhenTheOpCodeTableChangesUnderExportedFields
    {
        [Test]
        public void ThenTheSameTableHashesTheSame()
        {
            Assert.AreEqual(Hash(typeof(FieldScriptOpCode), typeof(TestGameOpCode)), Hash(typeof(FieldScriptOpCode), typeof(TestGameOpCode)));
        }

        [Test]
        public void ThenAGamesOpCodesChangeIt()
        {
            Assert.AreNotEqual(Hash(typeof(FieldScriptOpCode)), Hash(typeof(FieldScriptOpCode), typeof(TestGameOpCode)));
        }

        private static ulong Hash(params System.Type[] tables)
        {
            return FieldExport.OpCodeHash(new FieldOpCodeCatalogue(tables));
        }
    }
}
