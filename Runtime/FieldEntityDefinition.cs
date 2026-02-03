namespace RPGFramework.Field
{
    public sealed class FieldEntityDefinition
    {
        public int EntityId;

        // Optional presentation hint
        public string PrefabPath;

        // Script bindings
        public int InitScriptId;
        public int MainScriptId;
    }
}