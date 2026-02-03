using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class FieldEntity : MonoBehaviour
    {
        public FieldScriptDefinition ScriptDefinition;
        public string                InitialScriptName;

        public int EntityId => ScriptDefinition.EntityId;
    }
}