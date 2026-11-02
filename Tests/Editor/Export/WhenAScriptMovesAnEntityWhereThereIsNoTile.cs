using NUnit.Framework;

namespace RPGFramework.Field.Tests.Export
{
    /// <summary>A move on a tile field to a point off its tiles, which the entity could never reach.</summary>
    internal sealed class WhenAScriptMovesAnEntityWhereThereIsNoTile
    {
        [Test]
        public void ThenExportRefusesIt()
        {
            Assert.AreEqual(1, TileMoves.Problems("SLIDE_ENTITY 4.5 0.5 0").Count);
        }
    }
}
