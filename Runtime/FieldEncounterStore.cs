using RPGFramework.Core;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field
{
    /// <summary>
    /// Keeps the meter in the Session variables <see cref="FieldVariables.ENCOUNTER_DANGER" />,
    /// <see cref="FieldVariables.ENCOUNTER_STEP_FRACTION" /> and <see cref="FieldVariables.LAST_ENEMY_GROUP" />, which a new
    /// game or a load clears, and reads the rate from the persistent <see cref="FieldVariables.ENCOUNTER_RATE" />.
    /// </summary>
    internal sealed class FieldEncounterStore : IFieldEncounterStore
    {
        private readonly IMemoryService m_MemoryService;
        private readonly ushort         m_DangerAddress;
        private readonly ushort         m_StepFractionAddress;
        private readonly ushort         m_LastEnemyGroupAddress;
        private readonly ushort         m_RateAddress;

        internal FieldEncounterStore(IMemoryService memoryService, IVariableMap variableMap)
        {
            variableMap.TryGetVariable(FieldVariables.ENCOUNTER_DANGER,        out VariableDefinition danger);
            variableMap.TryGetVariable(FieldVariables.ENCOUNTER_STEP_FRACTION, out VariableDefinition stepFraction);
            variableMap.TryGetVariable(FieldVariables.LAST_ENEMY_GROUP,        out VariableDefinition lastEnemyGroup);
            variableMap.TryGetVariable(FieldVariables.ENCOUNTER_RATE,          out VariableDefinition rate);

            m_MemoryService         = memoryService;
            m_DangerAddress         = (ushort)danger.Offset;
            m_StepFractionAddress   = (ushort)stepFraction.Offset;
            m_LastEnemyGroupAddress = (ushort)lastEnemyGroup.Offset;
            m_RateAddress           = (ushort)rate.Offset;
        }

        ushort IFieldEncounterStore.Danger => m_MemoryService.ReadUshort(MemoryBank.Session, m_DangerAddress);

        void IFieldEncounterStore.SetDanger(ushort danger)
        {
            m_MemoryService.WriteUshort(MemoryBank.Session, m_DangerAddress, danger);
        }

        float IFieldEncounterStore.StepFraction => m_MemoryService.ReadFloat(MemoryBank.Session, m_StepFractionAddress);

        void IFieldEncounterStore.SetStepFraction(float fraction)
        {
            m_MemoryService.WriteFloat(MemoryBank.Session, m_StepFractionAddress, fraction);
        }

        ushort IFieldEncounterStore.LastEnemyGroup => m_MemoryService.ReadUshort(MemoryBank.Session, m_LastEnemyGroupAddress);

        void IFieldEncounterStore.SetLastEnemyGroup(ushort group)
        {
            m_MemoryService.WriteUshort(MemoryBank.Session, m_LastEnemyGroupAddress, group);
        }

        float IFieldEncounterStore.Rate => m_MemoryService.ReadFloat(MemoryBank.Persistent, m_RateAddress);
    }
}
