using System;

namespace RPGFramework.Field
{
    [Serializable]
    public sealed class ScriptEntry
    {
        public string              Name;     // "Init", "Main", "Talk", "OnTouch", etc.
        public FieldCompiledScript CompiledScript;
    }
}