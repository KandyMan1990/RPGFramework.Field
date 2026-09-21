using RPGFramework.Core;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.SharedTypes;
using RPGFramework.Field.SharedTypes.Providers;

namespace RPGFramework.Field
{
    /// <summary>
    /// Keeps the next field in <see cref="FieldVariables.CURRENT_FIELD" /> and <see cref="FieldVariables.CURRENT_SPAWN" />,
    /// so it is saved with persistent memory: a new game begins at their defaults, and a load resumes at the
    /// spawn point the player last entered by.
    /// </summary>
    public sealed class VariableFieldArgsStore : IFieldArgsStore
    {
        private readonly IMemoryService m_MemoryService;
        private readonly ushort         m_FieldAddress;
        private readonly ushort         m_SpawnAddress;

        public VariableFieldArgsStore(IMemoryService memoryService, IVariableMap variableMap)
        {
            variableMap.TryGetVariable(FieldVariables.CURRENT_FIELD, out VariableDefinition field);
            variableMap.TryGetVariable(FieldVariables.CURRENT_SPAWN, out VariableDefinition spawn);

            m_MemoryService = memoryService;
            m_FieldAddress  = (ushort)field.Offset;
            m_SpawnAddress  = (ushort)spawn.Offset;
        }

        FieldArgs IFieldArgsStore.Get
        {
            get
            {
                FieldArgs args = new FieldArgs(m_MemoryService.ReadUlong(MemoryBank.Persistent, m_FieldAddress),
                                               m_MemoryService.ReadInt(MemoryBank.Persistent, m_SpawnAddress));

                return args;
            }
        }

        void IFieldArgsStore.Set(FieldArgs args)
        {
            m_MemoryService.WriteUlong(MemoryBank.Persistent, m_FieldAddress, args.FieldId);
            m_MemoryService.WriteInt(MemoryBank.Persistent, m_SpawnAddress, args.SpawnId);
        }
    }
}
