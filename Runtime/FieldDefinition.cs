using System.Collections.Generic;

namespace RPGFramework.Field
{
    public sealed class FieldDefinition
    {
        public ulong FieldId;

        // Presentation
        public FieldPresentationType PresentationType;

        // Either:
        public string PrefabAddress;   // Addressables / Resources
        public string BinaryFieldPath; // field.bin offset or file

        // Logic
        public IReadOnlyList<FieldEntityDefinition>       Entities;
        public IReadOnlyDictionary<int, ScriptTableEntry> Scripts;
    }
}