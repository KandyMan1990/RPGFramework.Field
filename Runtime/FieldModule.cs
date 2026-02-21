using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGFramework.Audio;
using RPGFramework.Core;
using RPGFramework.Core.Input;
using RPGFramework.Core.PlayerLoop;
using RPGFramework.Core.SharedTypes;
using RPGFramework.DI;
using RPGFramework.Field.SharedTypes;
using RPGFramework.Menu.SharedTypes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGFramework.Field
{
    public class FieldModule : IFieldModule, IUpdatable
    {
        private readonly ICoreModule        m_CoreModule;
        private readonly IDIResolver        m_DIResolver;
        private readonly IInputRouter       m_InputRouter;
        private readonly IMenuTypeProvider  m_MenuTypeProvider;
        private readonly IMusicPlayer       m_MusicPlayer;
        private readonly ISfxPlayer         m_SfxPlayer;
        private readonly IFieldRegistry     m_FieldRegistry;
        private readonly IFieldPresentation m_FieldPresentation;

        private FieldModuleMonoBehaviour m_FieldModuleMonoBehaviour;
        private IInputContext            m_CurrentInputContext;
        private Camera                   m_Camera;

        private InputAdapter                             m_InputAdapter;
        private FieldContext                             m_FieldContext;
        private SpawnPoint                               m_SpawnPoint;
        private Dictionary<int, FieldEntity>             m_EntityGameObjects;
        private Dictionary<int, FieldGatewayTrigger>     m_EntityGatewayTriggers;
        private Dictionary<int, FieldInteractionTrigger> m_EntityInteractionTriggers;
        private HashSet<int>                             m_ActiveInteractionTriggerIds;

        private IFieldModuleArgs m_FieldTransitionArgs;
        private bool             m_FieldTransitionRequested = false;

        private IMovementDriver m_PlayerMovementDriver;

        public FieldModule(ICoreModule        coreModule,
                           IDIResolver        diResolver,
                           IInputRouter       inputRouter,
                           IMenuTypeProvider  menuTypeProvider,
                           IMusicPlayer       musicPlayer,
                           ISfxPlayer         sfxPlayer,
                           IFieldRegistry     fieldRegistry,
                           IFieldPresentation fieldPresentation)
        {
            m_CoreModule        = coreModule;
            m_DIResolver        = diResolver;
            m_InputRouter       = inputRouter;
            m_MenuTypeProvider  = menuTypeProvider;
            m_MusicPlayer       = musicPlayer;
            m_SfxPlayer         = sfxPlayer;
            m_FieldRegistry     = fieldRegistry;
            m_FieldPresentation = fieldPresentation;
        }

        async Task IModule.OnEnterAsync(IModuleArgs args)
        {
            m_InputAdapter = Object.FindFirstObjectByType<InputAdapter>();
            m_DIResolver.InjectInto(m_InputAdapter);

            m_FieldModuleMonoBehaviour = Object.FindFirstObjectByType<FieldModuleMonoBehaviour>();

            IFieldModuleArgs fieldArgs = (IFieldModuleArgs)args;

            await LoadNewFieldAsync(fieldArgs);
        }

        async Task IModule.OnExitAsync()
        {
            await UnloadCurrentFieldAsync();

            m_InputRouter.Clear();

            m_CoreModule.ResetModule<IFieldModule, FieldModule>();
        }

        Task IFieldModule.LoadMenuModuleAsync(byte menuId)
        {
            Type            type = m_MenuTypeProvider.GetType(menuId);
            IMenuModuleArgs args = new MenuModuleArgs(type);

            return m_CoreModule.LoadModuleAsync<IMenuModule>(args);
        }

        void IUpdatable.Update()
        {
            foreach (FieldEntityRuntime entity in m_FieldContext.Entities)
            {
                entity.Update(m_FieldContext.VM);
            }

            if (m_FieldTransitionRequested)
            {
                TriggerFieldTransitionAsync().FireAndForget();
            }
        }

        private void SetFieldModuleArgs(IFieldModuleArgs args)
        {
            m_FieldTransitionArgs      = args;
            m_FieldTransitionRequested = true;
        }

        private async Task TriggerFieldTransitionAsync()
        {
            m_FieldTransitionRequested = false;

            await UnloadCurrentFieldAsync();
            await LoadNewFieldAsync(m_FieldTransitionArgs);
        }

        private async Task LoadNewFieldAsync(IFieldModuleArgs args)
        {
            FieldDefinition fieldDefinition = m_FieldRegistry.LoadField(args.GetFieldId);

            GameObject   fieldGameObject = await m_FieldPresentation.LoadAsync(fieldDefinition);
            SpawnPoint[] spawnPoints     = fieldGameObject.GetComponentsInChildren<SpawnPoint>();

            m_SpawnPoint = Array.Find(spawnPoints, sp => sp.Id == args.GetSpawnId);

            FieldVM vm = new FieldVM();

            FieldEntity[] entitiesInGameObject = fieldGameObject.GetComponentsInChildren<FieldEntity>();

            m_EntityGameObjects         = new Dictionary<int, FieldEntity>(entitiesInGameObject.Length);
            m_EntityGatewayTriggers     = new Dictionary<int, FieldGatewayTrigger>();
            m_EntityInteractionTriggers = new Dictionary<int, FieldInteractionTrigger>();

            List<FieldEntityRuntime> entities = new List<FieldEntityRuntime>(entitiesInGameObject.Length);

            int scriptId = 0;

            foreach (FieldEntity entity in entitiesInGameObject)
            {
                m_EntityGameObjects.Add(entity.EntityId, entity);
                FieldGatewayTrigger gatewayTrigger = entity.GetComponentInChildren<FieldGatewayTrigger>();

                if (gatewayTrigger != null)
                {
                    m_EntityGatewayTriggers.Add(entity.EntityId, gatewayTrigger);
                    gatewayTrigger.OnTriggered += OnGatewayTriggered;
                }

                FieldInteractionTrigger interactionTrigger = entity.GetComponentInChildren<FieldInteractionTrigger>();

                if (interactionTrigger != null)
                {
                    m_EntityInteractionTriggers.Add(entity.EntityId, interactionTrigger);
                    interactionTrigger.OnInteracted     += OnInteractionTriggered;
                    interactionTrigger.OnTriggerEntered += OnInteractionTriggerEntered;
                    interactionTrigger.OnTriggerExited  += OnInteractionTriggerExited;
                }

                // TODO: ensure entity has a FieldScriptType.Init script as its first script
                FieldEntityRuntime fieldEntityRuntime = new FieldEntityRuntime(entity.EntityId, scriptId);

                entities.Add(fieldEntityRuntime);
                vm.RegisterEntity(entity.EntityId, fieldEntityRuntime);

                foreach (ScriptEntry scriptEntry in entity.ScriptDefinition.Scripts)
                {
                    vm.RegisterScript(scriptId, scriptEntry.CompiledScript);
                    scriptId++;
                }
            }

            m_FieldContext                = new FieldContext(vm, entities);
            m_ActiveInteractionTriggerIds = new HashSet<int>();

            vm.RequestFieldTransition             += SetFieldModuleArgs;
            vm.RequestMusic                       += RequestMusic;
            vm.RequestSfx                         += RequestSfx;
            vm.RequestSetPlayerEntity             += RequestSetPlayerEntity;
            vm.RequestSetEntityVisible            += RequestSetEntityVisible;
            vm.RequestSetGatewayTriggersActive    += RequestSetGatewayTriggersActive;
            vm.RequestSetInteractionTriggerActive += RequestSetInteractionTriggerActive;
            vm.RequestSetInteractionRange         += RequestSetInteractionRange;
            vm.RequestInputLock                   += RequestInputLock;

            m_Camera = Object.FindFirstObjectByType<Camera>();

            UpdateManager.RegisterUpdatable(this);

            m_CurrentInputContext = new FieldExplorationInputContext(GetBestInteractionTrigger, OpenConfigMenu, OnMove);
            m_InputRouter.Push(m_CurrentInputContext);

            m_InputAdapter.Enable();
        }

        private Task UnloadCurrentFieldAsync()
        {
            m_InputAdapter.Disable();

            m_CurrentInputContext = m_InputRouter.Pop(m_CurrentInputContext);

            UpdateManager.QueueForUnregisterUpdatable(this);

            m_FieldContext.VM.RequestInputLock                   -= RequestInputLock;
            m_FieldContext.VM.RequestSetInteractionRange         -= RequestSetInteractionRange;
            m_FieldContext.VM.RequestSetInteractionTriggerActive -= RequestSetInteractionTriggerActive;
            m_FieldContext.VM.RequestSetGatewayTriggersActive    -= RequestSetGatewayTriggersActive;
            m_FieldContext.VM.RequestSetEntityVisible            -= RequestSetEntityVisible;
            m_FieldContext.VM.RequestSetPlayerEntity             -= RequestSetPlayerEntity;
            m_FieldContext.VM.RequestSfx                         -= RequestSfx;
            m_FieldContext.VM.RequestMusic                       -= RequestMusic;
            m_FieldContext.VM.RequestFieldTransition             -= SetFieldModuleArgs;

            m_ActiveInteractionTriggerIds.Clear();

            foreach (KeyValuePair<int, FieldInteractionTrigger> entityInteractionTrigger in m_EntityInteractionTriggers)
            {
                entityInteractionTrigger.Value.OnTriggerExited  -= OnInteractionTriggerExited;
                entityInteractionTrigger.Value.OnTriggerEntered -= OnInteractionTriggerEntered;
                entityInteractionTrigger.Value.OnInteracted     -= OnInteractionTriggered;
            }

            foreach (KeyValuePair<int, FieldGatewayTrigger> entityGatewayTrigger in m_EntityGatewayTriggers)
            {
                entityGatewayTrigger.Value.OnTriggered -= OnGatewayTriggered;
            }

            m_FieldContext = null;

            m_FieldPresentation.Unload();

            return Task.CompletedTask;
        }

        private void RequestMusic(int id)
        {
            m_MusicPlayer.Play(id).FireAndForget();
        }

        private void RequestSfx(int id)
        {
            m_SfxPlayer.Play(id);
        }

        private void RequestSetPlayerEntity(FieldEntityRuntime entity)
        {
            if (m_PlayerMovementDriver != null)
            {
                Component currentDriver = (Component)m_PlayerMovementDriver;
                Object.Destroy(currentDriver);
            }

            m_FieldContext.SetPlayerEntity(entity);

            FieldEntity newPlayerEntity = m_EntityGameObjects[entity.EntityId];

            newPlayerEntity.transform.SetPositionAndRotation(m_SpawnPoint.Position, m_SpawnPoint.Rotation);

            m_PlayerMovementDriver = MovementDriverFactory.Create(newPlayerEntity.gameObject, 3f);
        }

        private void RequestSetEntityVisible(int entityId, bool visible)
        {
            m_EntityGameObjects[entityId].SetVisible(visible);
        }

        private void OnGatewayTriggered(int entityId, int scriptId)
        {
            m_FieldContext.VM.RequestScriptImmediately(entityId, scriptId);
        }

        private void OnInteractionTriggered(int entityId, int scriptId)
        {
            m_FieldContext.VM.RequestScriptImmediately(entityId, scriptId);
        }

        private void OnInteractionTriggerEntered(int entityId)
        {
            m_ActiveInteractionTriggerIds.Add(entityId);
        }

        private void OnInteractionTriggerExited(int entityId)
        {
            m_ActiveInteractionTriggerIds.Remove(entityId);
        }

        private bool IsPlayerFacingEntity(int entityId)
        {
            FieldEntity player = m_EntityGameObjects[m_FieldContext.PlayerEntity.EntityId];
            FieldEntity entity = m_EntityGameObjects[entityId];

            Transform playerTransform = player.transform;

            Vector3 playerPos = playerTransform.position;
            Vector3 entityPos = entity.transform.position;

            return IsFacing(playerPos, playerTransform.forward, entityPos, m_FieldModuleMonoBehaviour.PlayerInteractionAngle);
        }

        private bool IsEntityFacingPlayer(int entityId)
        {
            FieldEntity player = m_EntityGameObjects[m_FieldContext.PlayerEntity.EntityId];
            FieldEntity entity = m_EntityGameObjects[entityId];

            Transform entityTransform = entity.transform;

            Vector3 playerPos = player.transform.position;
            Vector3 entityPos = entityTransform.position;

            return IsFacing(entityPos, entityTransform.forward, playerPos, m_EntityInteractionTriggers[entityId].InteractionAngle);
        }

        private bool IsFacing(Vector3 fromPosition, Vector3 fromForward, Vector3 toPosition, float maxAngle)
        {
            Vector3 toEntity = toPosition - fromPosition;
            toEntity = Vector3.ProjectOnPlane(toEntity, m_FieldModuleMonoBehaviour.Up);

            if (toEntity.sqrMagnitude < 0.0001f)
            {
                return true;
            }

            toEntity.Normalize();

            Vector3 forward = Vector3.ProjectOnPlane(fromForward, m_FieldModuleMonoBehaviour.Up);
            forward.Normalize();

            float dot = Vector3.Dot(forward, toEntity);

            float halfAngle = maxAngle * 0.5f;
            float threshold = Mathf.Cos(halfAngle * Mathf.Deg2Rad);

            return dot >= threshold;
        }

        private FieldInteractionTrigger GetBestInteractionTrigger()
        {
            if (m_ActiveInteractionTriggerIds.Count == 0)
            {
                return null;
            }

            FieldEntity player          = m_EntityGameObjects[m_FieldContext.PlayerEntity.EntityId];
            Transform   playerTransform = player.transform;

            Vector3 playerPos     = playerTransform.position;
            Vector3 playerForward = Vector3.ProjectOnPlane(playerTransform.forward, m_FieldModuleMonoBehaviour.Up);
            playerForward = playerForward.normalized;

            FieldInteractionTrigger best      = null;
            float                   bestScore = float.MinValue;

            foreach (int entityId in m_ActiveInteractionTriggerIds)
            {
                FieldInteractionTrigger entity = m_EntityInteractionTriggers[entityId];

                if (!IsPlayerFacingEntity(entityId))
                {
                    continue;
                }

                if (!IsEntityFacingPlayer(entityId))
                {
                    continue;
                }

                Vector3 toEntity = entity.transform.position - playerPos;
                toEntity = Vector3.ProjectOnPlane(toEntity, m_FieldModuleMonoBehaviour.Up);

                float distance = toEntity.magnitude;
                if (distance < 0.0001f)
                {
                    continue;
                }

                Vector3 dir = toEntity.normalized;

                float dot = Vector3.Dot(playerForward, dir);

                float score = (dot * 2f) - distance;

                if (score > bestScore)
                {
                    bestScore = score;
                    best      = entity;
                }
            }

            return best;
        }

        private void OpenConfigMenu()
        {
            Type            type = m_MenuTypeProvider.GetType(MenuType.Config);
            IMenuModuleArgs args = new MenuModuleArgs(type);

            m_CoreModule.LoadModuleAsync<IMenuModule>(args).FireAndForget();
        }

        private void OnMove(Vector2 move)
        {
            Transform cameraTransform = m_Camera.transform;

            Vector3 up = m_FieldModuleMonoBehaviour.Up;

            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, up).normalized;
            Vector3 right   = Vector3.ProjectOnPlane(cameraTransform.right,   up).normalized;

            Vector3 worldMove = forward * move.y + right * move.x;

            MovePlayer(worldMove);
        }

        private void MovePlayer(Vector3 worldMove)
        {
            m_PlayerMovementDriver.SetMoveInput(worldMove);
        }

        private void RequestSetGatewayTriggersActive(bool active)
        {
            foreach (FieldGatewayTrigger fieldGatewayTrigger in m_EntityGatewayTriggers.Values)
            {
                fieldGatewayTrigger.SetActive(active);
            }
        }

        private void RequestSetInteractionTriggerActive(int entityId, bool active)
        {
            m_EntityInteractionTriggers[entityId].SetActive(active);
        }

        private void RequestSetInteractionRange(int entityId, float range)
        {
            m_EntityInteractionTriggers[entityId].SetInteractionRange(range);
        }

        private void RequestInputLock(int entityId, bool lockInput)
        {
            if (lockInput)
            {
                m_CurrentInputContext = new BlockAllInputContext();
                m_InputRouter.Push(m_CurrentInputContext);
            }
            else
            {
                BlockAllInputContext currentInputContext = m_CurrentInputContext as BlockAllInputContext;
                if (currentInputContext == null)
                {
                    Debug.LogError($"{nameof(FieldModule)}::{nameof(RequestInputLock)} cannot pop {nameof(BlockAllInputContext)}, current input context is {m_CurrentInputContext.GetType()}");
                    return;
                }

                m_CurrentInputContext = m_InputRouter.Pop(m_CurrentInputContext);
            }
        }
    }
}