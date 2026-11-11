using RPGFramework.Core;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field
{
    /// <summary>
    /// Keeps the meter in the Session variables <see cref="FieldVariables.ENCOUNTER_DANGER" />,
    /// <see cref="FieldVariables.ENCOUNTER_STEP_FRACTION" /> and <see cref="FieldVariables.LAST_ENCOUNTER" />, which a new
    /// game or a load clears, and reads the rate from the persistent <see cref="FieldVariables.ENCOUNTER_RATE" />.
    /// </summary>
    internal sealed class FieldEncounterStore : IFieldEncounterStore
    {
        private readonly IMemoryService m_MemoryService;
        private readonly ushort         m_DangerAddress;
        private readonly ushort         m_StepFractionAddress;
        private readonly ushort         m_LastEncounterAddress;
        private readonly ushort         m_RateAddress;

        internal FieldEncounterStore(IMemoryService memoryService, IVariableMap variableMap)
        {
            variableMap.TryGetVariable(FieldVariables.ENCOUNTER_DANGER,        out VariableDefinition danger);
            variableMap.TryGetVariable(FieldVariables.ENCOUNTER_STEP_FRACTION, out VariableDefinition stepFraction);
            variableMap.TryGetVariable(FieldVariables.LAST_ENCOUNTER,          out VariableDefinition lastEncounter);
            variableMap.TryGetVariable(FieldVariables.ENCOUNTER_RATE,          out VariableDefinition rate);

            m_MemoryService        = memoryService;
            m_DangerAddress        = (ushort)danger.Offset;
            m_StepFractionAddress  = (ushort)stepFraction.Offset;
            m_LastEncounterAddress = (ushort)lastEncounter.Offset;
            m_RateAddress          = (ushort)rate.Offset;
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

        ulong IFieldEncounterStore.LastEncounter => m_MemoryService.ReadUlong(MemoryBank.Session, m_LastEncounterAddress);

        void IFieldEncounterStore.SetLastEncounter(ulong encounter)
        {
            m_MemoryService.WriteUlong(MemoryBank.Session, m_LastEncounterAddress, encounter);
        }

        float IFieldEncounterStore.Rate => m_MemoryService.ReadFloat(MemoryBank.Persistent, m_RateAddress);
    }
}
