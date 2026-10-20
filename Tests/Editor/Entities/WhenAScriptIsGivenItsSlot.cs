using System;
using NUnit.Framework;

namespace RPGFramework.Field.Tests.Entities
{
    /// <summary>
    /// Where each script type runs, whether an event or a request starts it. Compiled fields name the slots by
    /// number, so these hold still.
    /// </summary>
    internal sealed class WhenAScriptIsGivenItsSlot
    {
        [Test]
        public void ThenThereAreEightSlots()
        {
            Assert.AreEqual(8, FieldEntityRuntime.PRIORITY_COUNT);
        }

        [Test]
        public void ThenEverySlotIsNamed()
        {
            Assert.AreEqual(FieldEntityRuntime.PRIORITY_COUNT, Enum.GetValues(typeof(FieldScriptPriority)).Length);
        }

        [TestCase(FieldScriptPriority.Main,        0)]
        [TestCase(FieldScriptPriority.Enter,       2)]
        [TestCase(FieldScriptPriority.Leave,       3)]
        [TestCase(FieldScriptPriority.Push,        6)]
        [TestCase(FieldScriptPriority.Interaction, 7)]
        public void ThenEachSlotHasItsNumber(FieldScriptPriority priority, int slot)
        {
            Assert.AreEqual(slot, (int)priority);
        }

        [TestCase(FieldScriptType.Requested,     FieldScriptPriority.Main,  1)]
        [TestCase(FieldScriptType.Requested,     FieldScriptPriority.Leave, 3)]
        [TestCase(FieldScriptType.OnInteraction, FieldScriptPriority.Leave, 7)]
        [TestCase(FieldScriptType.Gateway,       FieldScriptPriority.Main,  2)]
        [TestCase(FieldScriptType.OnPush,        FieldScriptPriority.Main,  6)]
        public void ThenATypeRunsInItsSlotAndOnlyARequestChooses(FieldScriptType type, FieldScriptPriority chosen, int slot)
        {
            Assert.AreEqual(slot, FieldScriptSlots.For(type, chosen));
        }

        [Test]
        public void ThenLeavingIsMoreUrgentThanEntering()
        {
            Assert.Greater((int)FieldScriptPriority.Leave, (int)FieldScriptPriority.Enter);
        }
    }
}
