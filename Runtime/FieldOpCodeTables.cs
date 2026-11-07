#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;

namespace RPGFramework.Field
{
    /// <summary>
    /// Authoring only. Every opcode table the editor has loaded, the framework's enum and any game's, found by their
    /// <see cref="FieldOpCodeAttribute" />s; and a game's opcodes as their attributes describe them, for the checks
    /// made in play.
    /// </summary>
    internal static class FieldOpCodeTables
    {
        private static Dictionary<ushort, DescribedGameOpCode> m_GameOpCodes;

        /// <summary>
        /// The enums describing opcodes. Without tests, only those a player builds, where a game's handlers run, so a
        /// test's table or an editor assembly's is never offered.
        /// </summary>
        internal static Type[] Find(bool withTests)
        {
            HashSet<string> playerAssemblies = null;

            if (!withTests)
            {
                playerAssemblies = new HashSet<string>(StringComparer.Ordinal);

                UnityEditor.Compilation.Assembly[] assemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);

                for (int i = 0; i < assemblies.Length; i++)
                {
                    playerAssemblies.Add(assemblies[i].name);
                }
            }

            TypeCache.FieldInfoCollection fields = TypeCache.GetFieldsWithAttribute<FieldOpCodeAttribute>();
            List<Type>                    tables = new List<Type>();

            for (int i = 0; i < fields.Count; i++)
            {
                Type table = fields[i].DeclaringType;

                if (table == null || !table.IsEnum || tables.Contains(table))
                {
                    continue;
                }

                if (playerAssemblies != null && !playerAssemblies.Contains(table.Assembly.GetName().Name))
                {
                    continue;
                }

                tables.Add(table);
            }

            return tables.ToArray();
        }

        /// <summary>A game's opcode with this value, as its attributes describe it, the tests' tables included.</summary>
        internal static bool TryGetGameOpCode(ushort opCode, out DescribedGameOpCode described)
        {
            m_GameOpCodes ??= DescribeGameOpCodes(Find(true));

            return m_GameOpCodes.TryGetValue(opCode, out described);
        }

        /// <summary>The game's opcodes in these tables, those whose arguments a handler can read.</summary>
        internal static Dictionary<ushort, DescribedGameOpCode> DescribeGameOpCodes(Type[] tables)
        {
            Dictionary<ushort, DescribedGameOpCode> described = new Dictionary<ushort, DescribedGameOpCode>();

            for (int i = 0; i < tables.Length; i++)
            {
                if (Enum.GetUnderlyingType(tables[i]) != typeof(ushort))
                {
                    continue;
                }

                FieldInfo[] members = tables[i].GetFields(BindingFlags.Public | BindingFlags.Static);

                for (int j = 0; j < members.Length; j++)
                {
                    FieldOpCodeAttribute opCode = members[j].GetCustomAttribute<FieldOpCodeAttribute>();
                    ushort               value  = (ushort)members[j].GetRawConstantValue();

                    if (opCode == null || !FieldScriptOpCodeRanges.IsGameOpCode(value) || described.ContainsKey(value))
                    {
                        continue;
                    }

                    GameArgumentRead[] reads = DescribeReads(members[j]);

                    if (reads != null)
                    {
                        described.Add(value, new DescribedGameOpCode(value, opCode.ScriptName, reads));
                    }
                }
            }

            return described;
        }

        // Null when an argument is one a handler cannot read, which the catalogue's check refuses at export.
        private static GameArgumentRead[] DescribeReads(FieldInfo member)
        {
            ArgumentAttribute[] arguments = (ArgumentAttribute[])member.GetCustomAttributes<ArgumentAttribute>();
            GameArgumentRead[]  reads     = new GameArgumentRead[arguments.Length];

            for (int i = 0; i < arguments.Length; i++)
            {
                ArgumentAttribute argument = arguments[i];

                if (argument.Index < 0 || argument.Index >= reads.Length ||
                    !ArgumentTypes.TryGetGameRead(argument.Type, argument.Width, out reads[argument.Index]))
                {
                    return null;
                }
            }

            return reads;
        }
    }

    /// <summary>Authoring only. One of a game's opcodes, as its attributes describe it.</summary>
    internal sealed class DescribedGameOpCode
    {
        internal readonly ushort             Value;
        internal readonly string             ScriptName;
        internal readonly GameArgumentRead[] Reads;

        internal DescribedGameOpCode(ushort value, string scriptName, GameArgumentRead[] reads)
        {
            Value      = value;
            ScriptName = scriptName;
            Reads      = reads;
        }
    }
}
#endif
