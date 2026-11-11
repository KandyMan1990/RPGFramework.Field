using System;
using System.Collections.Generic;
using RPGFramework.Battle.SharedTypes;
using UnityEditor;

namespace RPGFramework.Field.Editor
{
    /// <summary>The encounters a script or a field's random battles can start, as the battle module's editor lists them.</summary>
    internal static class EncounterNames
    {
        internal static List<string> Gather()
        {
            List<string>             names = new List<string>();
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<IBattleEncounterNames>();

            for (int i = 0; i < types.Count; i++)
            {
                Type type = types[i];

                if (type.IsAbstract)
                {
                    continue;
                }

                IReadOnlyList<string> found = ((IBattleEncounterNames)Activator.CreateInstance(type)).Names;

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
