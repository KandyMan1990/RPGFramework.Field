using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Field
{
    [CreateAssetMenu(menuName = "RPG Framework/Field/Script Definition", fileName = "FieldScriptDefinition")]
    public sealed class FieldScriptDefinition : ScriptableObject
    {
        public string            EntityName;
        public int               EntityId;
        public List<ScriptEntry> Scripts;

        public FieldCompiledScript GetScript(FieldScriptType scriptType)
        {
            for (int i = 0; i < Scripts.Count; i++)
            {
                ScriptEntry s = Scripts[i];
                if (s.ScriptType == scriptType)
                {
                    return s.CompiledScript;
                }
            }

            throw new Exception($"Script '{scriptType}' not found on {EntityName}");
        }
    }
}