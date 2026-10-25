using RPGFramework.Core;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// The player's position and facing, kept in persistent variables so a save holds where the player stood, and put
    /// back when that save is loaded.
    /// </summary>
    internal sealed class SavedPlayerPose
    {
        private readonly IMemoryService m_MemoryService;
        private readonly ushort         m_PositionXAddress;
        private readonly ushort         m_PositionYAddress;
        private readonly ushort         m_PositionZAddress;
        private readonly ushort         m_FacingAddress;
        private readonly ushort         m_LoadedFromSaveAddress;

        internal SavedPlayerPose(IMemoryService memoryService, IVariableMap variableMap)
        {
            m_MemoryService         = memoryService;
            m_PositionXAddress      = AddressOf(variableMap, FieldVariables.PLAYER_POSITION_X);
            m_PositionYAddress      = AddressOf(variableMap, FieldVariables.PLAYER_POSITION_Y);
            m_PositionZAddress      = AddressOf(variableMap, FieldVariables.PLAYER_POSITION_Z);
            m_FacingAddress         = AddressOf(variableMap, FieldVariables.PLAYER_FACING);
            m_LoadedFromSaveAddress = AddressOf(variableMap, CoreVariables.LOADED_FROM_SAVE);
        }

        /// <summary>
        /// Keeps where the player stands and which way they face, as degrees about <paramref name="up" /> from
        /// <paramref name="north" />, so a 3D player's is the turn of their body and a 2D player's the way their sprite
        /// faces.
        /// </summary>
        internal void Write(Vector3 position, Vector3 facing, Vector3 up, Vector3 north)
        {
            m_MemoryService.WriteFloat(MemoryBank.Persistent, m_PositionXAddress, position.x);
            m_MemoryService.WriteFloat(MemoryBank.Persistent, m_PositionYAddress, position.y);
            m_MemoryService.WriteFloat(MemoryBank.Persistent, m_PositionZAddress, position.z);
            m_MemoryService.WriteFloat(MemoryBank.Persistent, m_FacingAddress,    Vector3.SignedAngle(north, facing, up));
        }

        /// <summary>
        /// The saved pose, if the game has just been loaded. Clears the load either way, so only the first field
        /// entered after a load is placed by it.
        /// </summary>
        internal bool TryTakeAfterLoad(Vector3 up, out Vector3 position, out Quaternion rotation)
        {
            bool loaded = m_MemoryService.ReadBool(MemoryBank.Session, m_LoadedFromSaveAddress);

            m_MemoryService.WriteBool(MemoryBank.Session, m_LoadedFromSaveAddress, false);

            position = new Vector3(m_MemoryService.ReadFloat(MemoryBank.Persistent, m_PositionXAddress),
                                   m_MemoryService.ReadFloat(MemoryBank.Persistent, m_PositionYAddress),
                                   m_MemoryService.ReadFloat(MemoryBank.Persistent, m_PositionZAddress));
            rotation = Quaternion.AngleAxis(m_MemoryService.ReadFloat(MemoryBank.Persistent, m_FacingAddress), up);

            return loaded;
        }

        private static ushort AddressOf(IVariableMap variableMap, string name)
        {
            variableMap.TryGetVariable(name, out VariableDefinition variable);

            ushort address = (ushort)variable.Offset;

            return address;
        }
    }
}
