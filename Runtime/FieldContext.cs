using System.Collections.Generic;
using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// A field's state that outlives the field module: the module is recreated when the game returns from the
    /// menu or a battle, and this is what it resumes from. Anything a script sets has to be recorded here.
    /// </summary>
    internal sealed class FieldContext
    {
        internal IReadOnlyList<FieldEntityRuntime>           Entities                => m_Entities;
        internal FieldVM                                     VM                      { get; }
        internal FieldEntityRuntime                          PlayerEntity            => m_PlayerEntity;
        internal IReadOnlyList<int>                          VisibleEntityIds        => m_VisibleEntityIds;
        internal IReadOnlyList<int>                          HiddenEntityIds         => m_HiddenEntityIds;
        internal IReadOnlyDictionary<int, Vector3>           EntityPositions         => m_EntityPositions;
        internal IReadOnlyDictionary<int, Quaternion>        EntityRotations         => m_EntityRotations;
        internal IReadOnlyDictionary<int, RotationState>     EntityRotationStates    => m_EntityRotationStates;
        internal IReadOnlyDictionary<int, bool>              InteractionsActive      => m_InteractionsActive;
        internal IReadOnlyDictionary<int, bool>              CollisionTriggersActive => m_CollisionTriggersActive;
        internal IReadOnlyDictionary<int, float>             InteractionRanges       => m_InteractionRanges;
        internal IReadOnlyDictionary<int, float>             MovementSpeeds          => m_MovementSpeeds;
        internal IReadOnlyDictionary<int, ulong>             BaseAnimations          => m_BaseAnimations;
        internal IReadOnlyDictionary<int, float>             AnimationSpeeds         => m_AnimationSpeeds;
        internal IReadOnlyDictionary<int, PlayAnimationArgs> PlayedAnimations        => m_PlayedAnimations;
        internal bool                                        GatewaysActive          => m_GatewaysActive;
        internal bool                                        MainMenuAccessible      => m_MainMenuAccessible;
        internal bool                                        IsInputLockedByScript   => m_IsInputLockedByScript;

        private readonly List<FieldEntityRuntime>           m_Entities;
        private readonly List<int>                          m_VisibleEntityIds;
        private readonly List<int>                          m_HiddenEntityIds;
        private readonly Dictionary<int, Vector3>           m_EntityPositions;
        private readonly Dictionary<int, Quaternion>        m_EntityRotations;
        private readonly Dictionary<int, RotationState>     m_EntityRotationStates;
        private readonly Dictionary<int, bool>              m_InteractionsActive;
        private readonly Dictionary<int, bool>              m_CollisionTriggersActive;
        private readonly Dictionary<int, float>             m_InteractionRanges;
        private readonly Dictionary<int, float>             m_MovementSpeeds;
        private readonly Dictionary<int, ulong>             m_BaseAnimations;
        private readonly Dictionary<int, float>             m_AnimationSpeeds;
        private readonly Dictionary<int, PlayAnimationArgs> m_PlayedAnimations;
        private          FieldEntityRuntime                 m_PlayerEntity;
        private          bool                               m_GatewaysActive;
        private          bool                               m_MainMenuAccessible;
        private          bool                               m_IsInputLockedByScript;

        internal FieldContext(FieldVM vm, List<FieldEntityRuntime> entities)
        {
            VM                        = vm;
            m_Entities                = entities;
            m_VisibleEntityIds        = new List<int>();
            m_HiddenEntityIds         = new List<int>();
            m_EntityPositions         = new Dictionary<int, Vector3>(entities.Count);
            m_EntityRotations         = new Dictionary<int, Quaternion>(entities.Count);
            m_EntityRotationStates    = new Dictionary<int, RotationState>();
            m_InteractionsActive      = new Dictionary<int, bool>();
            m_CollisionTriggersActive = new Dictionary<int, bool>();
            m_InteractionRanges       = new Dictionary<int, float>();
            m_MovementSpeeds          = new Dictionary<int, float>();
            m_BaseAnimations          = new Dictionary<int, ulong>();
            m_AnimationSpeeds         = new Dictionary<int, float>();
            m_PlayedAnimations        = new Dictionary<int, PlayAnimationArgs>();
            m_GatewaysActive          = true;
            m_MainMenuAccessible      = true;
        }

        internal void SetPlayerEntity(FieldEntityRuntime playerEntity) => m_PlayerEntity = playerEntity;

        /// <summary>
        /// Record a script showing or hiding an entity. Entities no script has shown or hidden are in
        /// neither list, because hiding also switches triggers off and an untouched entity keeps its own.
        /// </summary>
        internal void SetEntityVisible(int entityId, bool visible)
        {
            List<int> addTo      = visible ? m_VisibleEntityIds : m_HiddenEntityIds;
            List<int> removeFrom = visible ? m_HiddenEntityIds : m_VisibleEntityIds;

            removeFrom.Remove(entityId);

            if (!addTo.Contains(entityId))
            {
                addTo.Add(entityId);
            }
        }

        internal void SetEntityPositionAndRotation(int entityId, Transform transform)
        {
            m_EntityPositions[entityId] = transform.position;
            m_EntityRotations[entityId] = transform.rotation;
        }

        internal void SetEntityRotationState(int entityId, RotationState rotationState)
        {
            m_EntityRotationStates[entityId] = rotationState;
        }

        internal void SetInteractionActive(int entityId, bool active) => m_InteractionsActive[entityId] = active;

        internal void SetCollisionTriggerActive(int entityId, bool active) => m_CollisionTriggersActive[entityId] = active;

        internal void SetInteractionRange(int entityId, float range) => m_InteractionRanges[entityId] = range;

        internal void SetMovementSpeed(int entityId, float speed) => m_MovementSpeeds[entityId] = speed;

        internal void SetBaseAnimation(int entityId, ulong stateNameHash) => m_BaseAnimations[entityId] = stateNameHash;

        internal void SetAnimationSpeed(int entityId, float multiplier) => m_AnimationSpeeds[entityId] = multiplier;

        /// <summary>
        /// A looping or held animation is still showing when the field is left, so it is put back on return. One that
        /// returns to base is not: it would replay from the start, which is not where the player left it.
        /// </summary>
        internal void SetPlayedAnimation(int entityId, PlayAnimationArgs args)
        {
            if (args.Mode == AnimationPlayMode.ReturnToBase)
            {
                m_PlayedAnimations.Remove(entityId);

                return;
            }

            m_PlayedAnimations[entityId] = args;
        }

        internal void ClearPlayedAnimation(int entityId) => m_PlayedAnimations.Remove(entityId);

        internal void SetGatewaysActive(bool active) => m_GatewaysActive = active;

        internal void SetMainMenuAccessible(bool accessible) => m_MainMenuAccessible = accessible;

        internal void SetInputLockedByScript(bool locked) => m_IsInputLockedByScript = locked;
    }
}