using NUnit.Framework;

namespace RPGFramework.Field.Tests.Export
{
    /// <summary>A move on a tile field to the centre of a tile, where it will end.</summary>
    internal sealed class WhenAScriptMovesAnEntityToATilesCentre
    {
        [Test]
        public void ThenExportTakesIt()
        {
            CollectionAssert.IsEmpty(TileMoves.Problems("MOVE_ENTITY 0.5 1.5 0"));
        }
    }
}
