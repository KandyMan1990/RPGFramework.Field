using RPGFramework.Core;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.SharedTypes;
using RPGFramework.Field.SharedTypes.Stores;

namespace RPGFramework.Field
{
    /// <summary>
    /// Reads the game-over field from <see cref="FieldVariables.GAME_OVER_FIELD" /> and
    /// <see cref="FieldVariables.GAME_OVER_SPAWN" />, session variables a new game and a load start at the map's
    /// defaults.
    /// </summary>
    public sealed class GameOverFieldStore : IGameOverFieldStore
    {
        private readonly IMemoryService m_MemoryService;
        private readonly ushort         m_FieldAddress;
        private readonly ushort         m_SpawnAddress;

        internal GameOverFieldStore(IMemoryService memoryService, IVariableMap variableMap)
        {
            variableMap.TryGetVariable(FieldVariables.GAME_OVER_FIELD, out VariableDefinition field);
            variableMap.TryGetVariable(FieldVariables.GAME_OVER_SPAWN, out VariableDefinition spawn);

            m_MemoryService = memoryService;
            m_FieldAddress  = (ushort)field.Offset;
            m_SpawnAddress  = (ushort)spawn.Offset;
        }

        FieldArgs IGameOverFieldStore.Args
        {
            get
            {
                FieldArgs args = new FieldArgs(m_MemoryService.ReadUlong(MemoryBank.Session, m_FieldAddress),
                                               m_MemoryService.ReadInt(MemoryBank.Session, m_SpawnAddress));

                return args;
            }
        }
    }
}