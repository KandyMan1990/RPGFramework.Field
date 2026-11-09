using System;
using System.Collections.Generic;
using RPGFramework.Battle.SharedTypes;
using UnityEditor;

namespace RPGFramework.Field.Editor
{
    /// <summary>The arenas a script can start a battle in, as the battle module's editor lists them.</summary>
    internal static class ArenaNames
    {
        internal static List<string> Gather()
        {
            List<string>             names = new List<string>();
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<IBattleArenaNames>();

            for (int i = 0; i < types.Count; i++)
            {
                Type type = types[i];

                if (type.IsAbstract)
                {
                    continue;
                }

                IReadOnlyList<string> found = ((IBattleArenaNames)Activator.CreateInstance(type)).Names;

                for (int j = 0; j < found.Count; j++)
                {
                    if (!names.Contains(found[j]))
                    {
                        names.Add(found[j]);
                    }
                }
            }

            return names;
        }
    }
}
