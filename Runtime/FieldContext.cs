using System.Collections.Generic;

namespace RPGFramework.Field
{
    /// <summary>
    /// A field's state that outlives the field module: the module is recreated when the game returns from a battle,
    /// and this is what it resumes from. What a script sets on an entity or blocker is written to its record here, once,
    /// and the module applies it from there.
    /// </summary>
    internal sealed class FieldContext
    {
        internal IReadOnlyList<FieldEntityRuntime>     Entities              => m_Entities;
        internal FieldVM                               VM                    => m_VM;
        internal FieldEntityRuntime                    PlayerEntity          => m_PlayerEntity;
        internal int                                   BlockerCount          => m_Blockers.Length;
        internal bool                                  GatewaysActive        => m_GatewaysActive;
        internal bool                                  MainMenuAccessible    => m_MainMenuAccessible;
        internal bool                                  IsInputLockedByScript => m_IsInputLockedByScript;
        internal IReadOnlyList<ButtonWaitInputContext> ButtonWaits           => m_ButtonWaits;

        private readonly FieldVM                      m_VM;
        private readonly FieldEntityRuntime[]         m_Entities;
        private readonly FieldEntityState[]           m_EntityStates;
        private readonly Dictionary<int, int>         m_EntityIndices;
        private readonly FieldBlockerState[]          m_Blockers;
        private readonly Dictionary<byte, int>        m_BlockerIndices;
        private readonly List<ButtonWaitInputContext> m_ButtonWaits;
        private          FieldEntityRuntime           m_PlayerEntity;
        private          bool                         m_GatewaysActive;
        private          bool                         m_MainMenuAccessible;
        private          bool                         m_IsInputLockedByScript;

        internal FieldContext(FieldVM vm, FieldEntityRuntime[] entities, byte[] blockerIds)
        {
            m_VM                 = vm;
            m_Entities           = entities;
            m_EntityStates       = new FieldEntityState[entities.Length];
            m_EntityIndices      = new Dictionary<int, int>(entities.Length);
            m_Blockers           = new FieldBlockerState[blockerIds.Length];
            m_BlockerIndices     = new Dictionary<byte, int>(blockerIds.Length);
            m_ButtonWaits        = new List<ButtonWaitInputContext>();
            m_GatewaysActive     = true;
            m_MainMenuAccessible = true;

            for (int i = 0; i < entities.Length; i++)
            {
                m_EntityIndices.Add(entities[i].EntityId, i);
            }

            for (int i = 0; i < blockerIds.Length; i++)
            {
                m_Blockers[i].BlockerId = blockerIds[i];
                m_BlockerIndices.Add(blockerIds[i], i);
            }
        }

        /// <summary>The entity's record, written in place.</summary>
        internal ref FieldEntityState EntityState(int entityId) => ref m_EntityStates[m_EntityIndices[entityId]];

        /// <summary>The record of the entity at this place in <see cref="Entities" />.</summary>
        internal ref FieldEntityState EntityStateAt(int index) => ref m_EntityStates[index];

        /// <summary>The blocker's record, written in place.</summary>
        internal ref FieldBlockerState BlockerState(byte blockerId) => ref m_Blockers[m_BlockerIndices[blockerId]];

        internal ref FieldBlockerState BlockerStateAt(int index) => ref m_Blockers[index];

        internal void SetPlayerEntity(FieldEntityRuntime playerEntity) => m_PlayerEntity = playerEntity;

        internal void SetGatewaysActive(bool active) => m_GatewaysActive = active;

        internal void SetMainMenuAccessible(bool accessible) => m_MainMenuAccessible = accessible;

        internal void SetInputLockedByScript(bool locked) => m_IsInputLockedByScript = locked;

        /// <summary>A script waiting for a button, kept so the wait is offered input again after a battle.</summary>
        internal void AddButtonWait(ButtonWaitInputContext wait) => m_ButtonWaits.Add(wait);

        internal void RemoveButtonWait(ButtonWaitInputContext wait) => m_ButtonWaits.Remove(wait);
    }
}