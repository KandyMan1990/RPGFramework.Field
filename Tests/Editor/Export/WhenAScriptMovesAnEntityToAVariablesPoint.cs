using NUnit.Framework;

namespace RPGFramework.Field.Tests.Export
{
    /// <summary>A move on a tile field to a point read from a variable, known only while playing.</summary>
    internal sealed class WhenAScriptMovesAnEntityToAVariablesPoint
    {
        [Test]
        public void ThenExportLeavesItToThen()
        {
            CollectionAssert.IsEmpty(TileMoves.Problems("MOVE_ENTITY $x 0.5 0"));
        }
    }
}
