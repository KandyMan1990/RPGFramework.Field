using System.Collections.Generic;

namespace RPGFramework.Field
{
    public sealed class FieldDefinition
    {
        public string FieldId;

        public string PrefabAddress;

        // Logic
        public IReadOnlyList<FieldEntityDefinition>       Entities;
        public IReadOnlyDictionary<int, ScriptTableEntry> Scripts;
        
        public IReadOnlyList<SpawnPoint> SpawnPoints;
    }
}