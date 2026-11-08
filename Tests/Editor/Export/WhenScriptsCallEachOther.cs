using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Export
{
    internal sealed class WhenScriptsCallEachOther
    {
        [Test]
        public void ThenCallsThatEndAreTaken()
        {
            CollectionAssert.IsEmpty(Problems(Requested("CALL_SCRIPT 1\nRETURN"), Requested("RETURN_FROM_CALL")));
        }

        [Test]
        public void ThenAScriptCallingItselfIsRefused()
        {
            StringAssert.Contains("never end", Problems(Requested("CALL_SCRIPT 0\nRETURN"))[0]);
        }

        [Test]
        public void ThenAScriptCallingItselfThroughAnotherIsRefused()
        {
            StringAssert.Contains("never end", Problems(Requested("CALL_SCRIPT 1"), Requested("CALL_SCRIPT 0"))[0]);
        }

        [Test]
        public void ThenCallsAsDeepAsThereIsRoomForAreTaken()
        {
            CollectionAssert.IsEmpty(Problems(Chain(FieldScriptContext.MAX_CALL_DEPTH)));
        }

        [Test]
        public void ThenCallsDeeperThanThereIsRoomForAreRefused()
        {
            StringAssert.Contains("room for", Problems(Chain(FieldScriptContext.MAX_CALL_DEPTH + 1))[0]);
        }

        [Test]
        public void ThenAnInitCallingAScriptThatWaitsIsRefused()
        {
            FieldScriptRecord init = new FieldScriptRecord(FieldScriptType.Init, "Init", "CALL_SCRIPT 1\nRETURN");

            StringAssert.Contains("waits or gives up the frame", Problems(init, Requested("WAIT_SECONDS 1\nRETURN_FROM_CALL"))[0]);
        }

        [Test]
        public void ThenAReturnToALessUrgentSlotIsTaken()
        {
            CollectionAssert.IsEmpty(Problems(new FieldScriptRecord(FieldScriptType.OnInteraction, "Talk", "RETURN_TO_PRIORITY 0")));
        }

        [Test]
        public void ThenAReturnToItsOwnSlotIsRefused()
        {
            StringAssert.Contains("less urgent", Problems(new FieldScriptRecord(FieldScriptType.OnInteraction, "Talk", "RETURN_TO_PRIORITY 7"))[0]);
        }

        [Test]
        public void ThenACalledScriptIsNotJudgedByItsOwnSlot()
        {
            FieldScriptRecord talk = new FieldScriptRecord(FieldScriptType.OnInteraction, "Talk", "CALL_SCRIPT 1\nRETURN");

            CollectionAssert.IsEmpty(Problems(talk, new FieldScriptRecord(FieldScriptType.Default, "Default", "RETURN_TO_PRIORITY 0")));
        }

        private static FieldScriptRecord Requested(string text)
        {
            FieldScriptRecord record = new FieldScriptRecord(FieldScriptType.Requested, "Script", text);

            record.SetSlot(FieldScriptPriority.Unassigned);

            return record;
        }

        /// <summary>Scripts each calling the next, this many calls inside one another.</summary>
        private static FieldScriptRecord[] Chain(int calls)
        {
            FieldScriptRecord[] scripts = new FieldScriptRecord[calls + 1];

            for (int i = 0; i < calls; i++)
            {
                scripts[i] = Requested($"CALL_SCRIPT {i + 1}\nRETURN_FROM_CALL");
            }

            scripts[calls] = Requested("RETURN_FROM_CALL");

            return scripts;
        }

        private static List<string> Problems(params FieldScriptRecord[] scripts)
        {
            List<string> problems = new List<string>();

            FieldDatabase.ValidateScriptCalls("Entity", scripts, problems);

            return problems;
        }
    }
}
