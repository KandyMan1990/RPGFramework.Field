using NUnit.Framework;
using UnityEngine;

namespace RPGFramework.Field.Tests.Interaction
{
    /// <summary>Someone whose interaction angle is narrowed, as a shopkeeper behind a counter might be.</summary>
    internal sealed class WhenSomeoneTalksOnlyFromTheFront
    {
        [Test]
        public void ThenFacingThePlayerTheyAnswer()
        {
            Assert.AreEqual(1, Around.Pick(Around.FacingOnly(1, Vector3.back)));
        }

        [Test]
        public void ThenFacingAwayTheyDoNot()
        {
            Assert.AreEqual(InteractionSearch.NONE, Around.Pick(Around.FacingOnly(1, Vector3.forward)));
        }
    }
}
