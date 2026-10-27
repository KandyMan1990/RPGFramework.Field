using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Interaction
{
    /// <summary>A 2D field lies on the screen, up out of it; the player faces down the screen.</summary>
    internal sealed class WhenThePlayerFacesDownA2DField
    {
        [Test]
        public void ThenTheOneBelowAnswersNotTheOneAbove()
        {
            InteractionSearch    search = new InteractionSearch(Vector3.forward, 180f, 2f);
            InteractionCandidate above  = new InteractionCandidate(1, Vector3.up,   Vector3.down, 2f, 360f);
            InteractionCandidate below  = new InteractionCandidate(2, Vector3.down, Vector3.up,   2f, 360f);

            Assert.AreEqual(2, search.Pick(0, Vector3.zero, Vector3.down, new[] { above, below }));
        }
    }
}
