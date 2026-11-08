using NUnit.Framework;

namespace RPGFramework.Field.Tests.Export
{
    /// <summary>On a tile field, a jump or a ladder ending off the centre of a tile, where nothing could step on from.</summary>
    internal sealed class WhenAJumpOrLadderLeavesATilesCentre
    {
        [Test]
        public void ThenExportRefusesTheJump()
        {
            Assert.AreEqual(1, TileMoves.Problems("JUMP_ENTITY_AND_WAIT 0.25 0.5 0 1 1").Count);
        }

        [Test]
        public void ThenExportAcceptsAJumpToACentre()
        {
            Assert.AreEqual(0, TileMoves.Problems("JUMP_ENTITY_AND_WAIT 0.5 0.5 0 1 1").Count);
        }

        [Test]
        public void ThenExportRefusesTheLadder()
        {
            Assert.AreEqual(1, TileMoves.Problems("CLIMB_LADDER_AND_WAIT true 0 0 0 0 0 0 0.25 0.5 0").Count);
        }

        [Test]
        public void ThenExportAcceptsALadderEndingOnACentre()
        {
            Assert.AreEqual(0, TileMoves.Problems("CLIMB_LADDER_AND_WAIT true 0 0 0 0 0 0 0.5 0.5 0").Count);
        }
    }
}
