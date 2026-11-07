#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// Authoring only. The opcodes a game's <see cref="IFieldGameOpCodeDatabase" /> holds, which the editor cannot see before
    /// the game runs, against the ones its enums describe: checked as the field module enters in play mode, so an opcode
    /// described but never added is found before a script stops at it.
    /// </summary>
    internal static class FieldGameOpCodeDatabaseCheck
    {
        internal static void Log(IFieldGameOpCodeDatabase database)
        {
            List<string> problems = new List<string>();

            Check(database, FieldOpCodeTables.DescribeGameOpCodes(FieldOpCodeTables.Find(false)), problems);

            if (problems.Count > 0)
            {
                Debug.LogError($"The game's opcode database has {problems.Count} problem(s):\n  {string.Join("\n  ", problems)}");
            }
        }

        internal static void Check(IFieldGameOpCodeDatabase database, Dictionary<ushort, DescribedGameOpCode> described, List<string> problems)
        {
            foreach (KeyValuePair<ushort, FieldOpCodeHandler> added in database.Handlers)
            {
                if (!FieldScriptOpCodeRanges.IsGameOpCode(added.Key))
                {
                    problems.Add($"0x{added.Key:X4} is added with {added.Value.Method.Name}, but a game's opcodes start at 0x{FieldScriptOpCodeRanges.GAME_BASE:X4}: the framework's own are below, and it would replace one of them");
                }
                else if (!described.ContainsKey(added.Key))
                {
                    problems.Add($"0x{added.Key:X4} is added with {added.Value.Method.Name}, but no [FieldOpCode] in a game's enum describes it, so no script can use it");
                }
            }

            foreach (KeyValuePair<ushort, DescribedGameOpCode> opCode in described)
            {
                if (!database.Handlers.ContainsKey(opCode.Key))
                {
                    problems.Add($"{opCode.Value.ScriptName} (0x{opCode.Key:X4}) is described but not added, so a script stops where it uses it: add its handler to the game's {nameof(IFieldGameOpCodeDatabase)}");
                }
            }
        }
    }
}
#endif
