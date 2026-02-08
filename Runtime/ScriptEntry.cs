using System;

namespace RPGFramework.Field
{
    [Serializable]
    public sealed class ScriptEntry
    {
        public FieldScriptType     ScriptType;
        public FieldCompiledScript CompiledScript;
    }
}