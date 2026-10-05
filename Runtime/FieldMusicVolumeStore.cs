using RPGFramework.Core;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field
{
    /// <summary>
    /// The volume the field plays its music at, kept in a persistent variable so it holds across fields, battles and
    /// saves, as the menu and a battle rebuild the field.
    /// </summary>
    internal sealed class FieldMusicVolumeStore
    {
        private readonly IMemoryService m_MemoryService;
        private readonly ushort         m_Address;

        internal FieldMusicVolumeStore(IMemoryService memoryService, IVariableMap variableMap)
        {
            variableMap.TryGetVariable(FieldVariables.MUSIC_VOLUME, out VariableDefinition musicVolume);

            m_MemoryService = memoryService;
            m_Address       = (ushort)musicVolume.Offset;
        }

        internal float Get()
        {
            float volume = m_MemoryService.ReadFloat(MemoryBank.Persistent, m_Address);

            return volume;
        }

        internal void Set(float volume) => m_MemoryService.WriteFloat(MemoryBank.Persistent, m_Address, volume);
    }
}
