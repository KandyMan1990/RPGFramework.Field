using NUnit.Framework;

namespace RPGFramework.Field.Tests.Export
{
    /// <summary>A move on a tile field to a point between tiles, which would end on a tile's centre instead.</summary>
    internal sealed class WhenAScriptMovesAnEntityOffATilesCentre
    {
        [Test]
        public void ThenExportNamesTheTilesCentre()
        {
            string[] expected =
            {
                "script uses GLIDE_ENTITY_AND_WAIT to 1 1.5 0, off the centre of its tile, but a move on a tile field ends on a tile's centre. Use 1.5 1.5 0"
            };

            CollectionAssert.AreEqual(expected, TileMoves.Problems("GLIDE_ENTITY_AND_WAIT 1 1.5 0"));
        }
    }
}
