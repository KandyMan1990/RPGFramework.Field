using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGFramework.Audio;
using RPGFramework.Battle.SharedTypes;
using RPGFramework.Battle.SharedTypes.Constants;
using RPGFramework.Battle.SharedTypes.Providers;
using RPGFramework.Core;
using RPGFramework.Core.Dialogue;
using RPGFramework.Core.Dialogue.Flows;
using RPGFramework.Core.Input;
using RPGFramework.Core.PlayerLoop;
using RPGFramework.Core.Rendering;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Core.Store;
using RPGFramework.DI;
using RPGFramework.Field.FieldVmArgs;
using RPGFramework.Field.SharedTypes;
using RPGFramework.Field.SharedTypes.Constants;
using RPGFramework.Field.SharedTypes.Providers;
using RPGFramework.Localisation;
using RPGFramework.Menu.SharedTypes;
using RPGFramework.Menu.SharedTypes.Constants;
using RPGFramework.Menu.SharedTypes.Providers;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace RPGFramework.Field
{
    public class FieldModule : IFieldModule, IUpdatable
    {
        private readonly ICoreModule                        m_CoreModule;
        private readonly IDIResolver                        m_DIResolver;
        private readonly IInputRouter                       m_InputRouter;
        private readonly IMusicPlayer                       m_MusicPlayer;
        private readonly ISfxPlayer                         m_SfxPlayer;
        private readonly IFieldDatabase                     m_FieldDatabase;
        private readonly IFieldPresentation                 m_FieldPresentation;
        private readonly ILocalisationService               m_LocalisationService;
        private readonly Dictionary<ulong, IDialogueWindow> m_DialogueWindows;
        private readonly IMemoryService                     m_MemoryService;
        private readonly IScreenFadeService                 m_ScreenFadeService;
        private readonly IBattleArgsProvider                m_BattleArgsProvider;
        private readonly IFieldArgsProvider                 m_FieldArgsProvider;
        private readonly IMenuArgsProvider                  m_MenuArgsProvider;
        private readonly IChangeModuleStore                 m_ChangeModuleStore;
        private readonly IResumeModuleStore                 m_ResumeModuleStore;
        private readonly IFieldResumeDataStore              m_FieldResumeDataStore;

        private FieldModuleMonoBehaviour m_FieldModuleMonoBehaviour;
        private IInputContext            m_CurrentInputContext;
        private TransformHandle          m_CameraTransformHandle;
        private VisualElement            m_RootElement;

        private InputAdapter                           m_InputAdapter;
        private FieldContext                           m_FieldContext;
        private SpawnPoint                             m_InitialPlayerSpawn;
        private Dictionary<int, FieldEntityComponents> m_Entities;
        private HashSet<int>                           m_ActiveInteractionTriggerIds;
        private int                                    m_PlayerEntityId;

        private FieldArgs          m_FieldArgs;
        private bool               m_FieldTransitionRequested;
        private FieldDatabaseAsset m_FieldDatabaseAsset;

        private bool m_BattleTransitionRequested;
        private bool m_MenuTransitionRequested;

        private IMovementDriver m_PlayerMovementDriver;

        private bool m_MainMenuAccessible;

        public FieldModule(ICoreModule           coreModule,
                           IDIResolver           diResolver,
                           IInputRouter          inputRouter,
                           IMusicPlayer          musicPlayer,
                           ISfxPlayer            sfxPlayer,
                           IFieldDatabase        fieldDatabase,
                           IFieldPresentation    fieldPresentation,
                           ILocalisationService  localisationService,
                           IMemoryService        memoryService,
                           IScreenFadeService    screenFadeService,
                           IBattleArgsProvider   battleArgsProvider,
                           IFieldArgsProvider    fieldArgsProvider,
                           IMenuArgsProvider     menuArgsProvider,
                           IChangeModuleStore    changeModuleStore,
                           IResumeModuleStore    resumeModuleStore,
                           IFieldResumeDataStore fieldResumeDataStore)
        {
            m_CoreModule           = coreModule;
            m_DIResolver           = diResolver;
            m_InputRouter          = inputRouter;
            m_MusicPlayer          = musicPlayer;
            m_SfxPlayer            = sfxPlayer;
            m_FieldDatabase        = fieldDatabase;
            m_FieldPresentation    = fieldPresentation;
            m_LocalisationService  = localisationService;
            m_MemoryService        = memoryService;
            m_ScreenFadeService    = screenFadeService;
            m_BattleArgsProvider   = battleArgsProvider;
            m_FieldArgsProvider    = fieldArgsProvider;
            m_MenuArgsProvider     = menuArgsProvider;
            m_ChangeModuleStore    = changeModuleStore;
            m_ResumeModuleStore    = resumeModuleStore;
            m_FieldResumeDataStore = fieldResumeDataStore;
            m_DialogueWindows      = new Dictionary<ulong, IDialogueWindow>(8);
        }

        async Task IModule.OnEnterAsync()
        {
            await m_ScreenFadeService.FadeOutAsync(true);

            m_FieldModuleMonoBehaviour = Object.FindAnyObjectByType<FieldModuleMonoBehaviour>();

            m_InputAdapter = m_FieldModuleMonoBehaviour.InputAdapter;
            m_DIResolver.InjectInto(m_InputAdapter);

            UIDocument uiDoc = m_FieldModuleMonoBehaviour.UIDocument;
            m_RootElement = uiDoc.rootVisualElement.Q("Root");

            m_CameraTransformHandle = m_FieldModuleMonoBehaviour.CameraTransformHandle;

            m_FieldContext = m_FieldResumeDataStore.Get<FieldContext>();

            if (m_FieldContext == null)
            {
                await LoadNewFieldAsync();
                return;
            }

            await ResumeFieldAsync();
        }

        async Task IModule.OnExitAsync()
        {
            await UnloadCurrentFieldAsync();

            m_InputRouter.Clear();

            m_CoreModule.ResetModule<IFieldModule, FieldModule>();
        }

        private void RequestMenuModule(byte menuId)
        {
            MenuArgs args = new MenuArgs(menuId);
            m_MenuArgsProvider.Set(args);

            m_MenuTransitionRequested = true;
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
                return;
            }

            if (m_MenuTransitionRequested)
            {
                TriggerMenuTransitionAsync().FireAndForget();
                return;
            }

            if (m_BattleTransitionRequested)
            {
                TriggerBattleTransitionAsync().FireAndForget();
            }
        }

        private void SubscribeVm()
        {
            m_FieldContext.VM.RequestFieldTransition             += OnSetFieldModuleArgs;
            m_FieldContext.VM.RequestMusic                       += OnRequestMusic;
            m_FieldContext.VM.RequestSfx                         += OnRequestSfx;
            m_FieldContext.VM.RequestSetPlayerEntity             += OnRequestSetPlayerEntity;
            m_FieldContext.VM.RequestSetEntityVisible            += OnRequestSetEntityVisible;
            m_FieldContext.VM.RequestSetGatewayTriggersActive    += OnRequestSetGatewayTriggersActive;
            m_FieldContext.VM.RequestSetInteractionTriggerActive += OnRequestSetInteractionTriggerActive;
            m_FieldContext.VM.RequestSetInteractionRange         += OnRequestSetInteractionRange;
            m_FieldContext.VM.RequestInputLock                   += OnRequestInputLock;
            m_FieldContext.VM.RequestSetEntityPosition           += OnRequestSetEntityPosition;
            m_FieldContext.VM.RequestSetEntityRotation           += OnRequestSetEntityRotation;
            m_FieldContext.VM.RequestSetEntityRotationAsync      += OnRequestSetEntityRotationAsync;
            m_FieldContext.VM.IsEntityRotating                   =  IsEntityRotating;
            m_FieldContext.VM.RequestSetEntityToFaceEntity       += OnRequestSetEntityToFaceEntity;
            m_FieldContext.VM.RequestSetEntityMovementSpeed      += OnRequestSetEntityMovementSpeed;
            m_FieldContext.VM.RequestSetMainMenuAccessibility    += OnRequestSetMainMenuAccessibility;
            m_FieldContext.VM.RequestCreateDialogueWindow        += OnRequestCreateDialogueWindow;
            m_FieldContext.VM.RequestShowDialogueWindow          += OnRequestShowDialogueWindow;
            m_FieldContext.VM.IsDialogueWindowOpen               =  IsDialogueWindowOpen;
            m_FieldContext.VM.RequestAskPlayerToMakeAChoice      += OnRequestAskPlayerToMakeAChoice;
            m_FieldContext.VM.IsPlayerMakingAChoice              =  IsDialogueWindowOpen;
            m_FieldContext.VM.RequestSetBattleModeOptions        += OnRequestSetBattleModeOptions;
            m_FieldContext.VM.RequestStartBattle                 += OnRequestStartBattle;
        }

        private void UnsubscribeVm()
        {
            m_FieldContext.VM.RequestStartBattle                 -= OnRequestStartBattle;
            m_FieldContext.VM.RequestSetBattleModeOptions        -= OnRequestSetBattleModeOptions;
            m_FieldContext.VM.IsPlayerMakingAChoice              =  null;
            m_FieldContext.VM.RequestAskPlayerToMakeAChoice      -= OnRequestAskPlayerToMakeAChoice;
            m_FieldContext.VM.IsDialogueWindowOpen               =  null;
            m_FieldContext.VM.RequestShowDialogueWindow          -= OnRequestShowDialogueWindow;
            m_FieldContext.VM.RequestCreateDialogueWindow        -= OnRequestCreateDialogueWindow;
            m_FieldContext.VM.RequestSetMainMenuAccessibility    -= OnRequestSetMainMenuAccessibility;
            m_FieldContext.VM.RequestSetEntityMovementSpeed      -= OnRequestSetEntityMovementSpeed;
            m_FieldContext.VM.RequestSetEntityToFaceEntity       -= OnRequestSetEntityToFaceEntity;
            m_FieldContext.VM.IsEntityRotating                   =  null;
            m_FieldContext.VM.RequestSetEntityRotationAsync      -= OnRequestSetEntityRotationAsync;
            m_FieldContext.VM.RequestSetEntityRotation           -= OnRequestSetEntityRotation;
            m_FieldContext.VM.RequestSetEntityPosition           -= OnRequestSetEntityPosition;
            m_FieldContext.VM.RequestInputLock                   -= OnRequestInputLock;
            m_FieldContext.VM.RequestSetInteractionRange         -= OnRequestSetInteractionRange;
            m_FieldContext.VM.RequestSetInteractionTriggerActive -= OnRequestSetInteractionTriggerActive;
            m_FieldContext.VM.RequestSetGatewayTriggersActive    -= OnRequestSetGatewayTriggersActive;
            m_FieldContext.VM.RequestSetEntityVisible            -= OnRequestSetEntityVisible;
            m_FieldContext.VM.RequestSetPlayerEntity             -= OnRequestSetPlayerEntity;
            m_FieldContext.VM.RequestSfx                         -= OnRequestSfx;
            m_FieldContext.VM.RequestMusic                       -= OnRequestMusic;
            m_FieldContext.VM.RequestFieldTransition             -= OnSetFieldModuleArgs;
        }

        private void OnSetFieldModuleArgs(FieldArgs args)
        {
            m_FieldArgs                = args;
            m_FieldTransitionRequested = true;
        }

        private async Task TriggerFieldTransitionAsync()
        {
            m_FieldTransitionRequested = false;

            await UnloadCurrentFieldAsync();
            await LoadNewFieldAsync();
        }

        private Task TriggerMenuTransitionAsync()
        {
            m_MenuTransitionRequested = false;

            StoreToTempMemory();

            m_ResumeModuleStore.SetModuleId(FieldConstants.MODULE_ID);
            m_ChangeModuleStore.SetModuleId(MenuConstants.MODULE_ID);

            return m_CoreModule.RequestModuleChangeAsync();
        }

        private Task TriggerBattleTransitionAsync()
        {
            m_BattleTransitionRequested = false;

            StoreToTempMemory();

            m_ResumeModuleStore.SetModuleId(FieldConstants.MODULE_ID);
            m_ChangeModuleStore.SetModuleId(BattleConstants.MODULE_ID);

            return m_CoreModule.RequestModuleChangeAsync();
        }

        private async Task<FieldEntity[]> PreLoadFieldAsync()
        {
            m_FieldArgs          = m_FieldArgsProvider.Get;
            m_FieldDatabaseAsset = m_FieldDatabase.Get(m_FieldArgs.FieldId);

            await m_LocalisationService.LoadNewLocalisationDataAsync(m_FieldDatabaseAsset.LocalisationSheets);

            GameObject   fieldGameObject = await m_FieldPresentation.LoadAsync(m_FieldDatabaseAsset);
            SpawnPoint[] spawnPoints     = fieldGameObject.GetComponentsInChildren<SpawnPoint>();
            m_InitialPlayerSpawn = Array.Find(spawnPoints, sp => sp.Id == m_FieldArgs.SpawnId);

            FieldEntity[] entitiesInGameObject = fieldGameObject.GetComponentsInChildren<FieldEntity>();
            m_Entities = new Dictionary<int, FieldEntityComponents>(entitiesInGameObject.Length);

            return entitiesInGameObject;
        }

        private async Task LoadNewFieldAsync()
        {
            FieldEntity[] entitiesInGameObject = await PreLoadFieldAsync();

            FieldVM                  vm       = new FieldVM(m_MemoryService);
            List<FieldEntityRuntime> entities = new List<FieldEntityRuntime>(entitiesInGameObject.Length);

            int scriptId = 0;

            foreach (FieldEntity entity in entitiesInGameObject)
            {
                FieldEntityComponents entityComponents = new FieldEntityComponents();
                entityComponents.SetEntity(entity);

                m_Entities.Add(entity.EntityId, entityComponents);
                FieldGatewayTrigger gatewayTrigger = entity.GetComponentInChildren<FieldGatewayTrigger>();

                if (gatewayTrigger != null)
                {
                    entityComponents.SetGatewayTrigger(gatewayTrigger);
                    gatewayTrigger.OnTriggered += OnGatewayTriggered;
                }

                FieldInteractionTrigger interactionTrigger = entity.GetComponentInChildren<FieldInteractionTrigger>();

                if (interactionTrigger != null)
                {
                    entityComponents.SetInteractionTrigger(interactionTrigger);
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

            m_FieldContext = new FieldContext(vm, entities);

            await PostFieldLoadAsync();
        }

        private async Task ResumeFieldAsync()
        {
            FieldEntity[] entitiesInGameObject = await PreLoadFieldAsync();

            foreach (FieldEntity entity in entitiesInGameObject)
            {
                FieldEntityComponents entityComponents = new FieldEntityComponents();
                entityComponents.SetEntity(entity);

                m_Entities.Add(entity.EntityId, entityComponents);
                FieldGatewayTrigger gatewayTrigger = entity.GetComponentInChildren<FieldGatewayTrigger>();

                if (gatewayTrigger != null)
                {
                    entityComponents.SetGatewayTrigger(gatewayTrigger);
                    gatewayTrigger.OnTriggered += OnGatewayTriggered;
                }

                FieldInteractionTrigger interactionTrigger = entity.GetComponentInChildren<FieldInteractionTrigger>();

                if (interactionTrigger != null)
                {
                    entityComponents.SetInteractionTrigger(interactionTrigger);
                    interactionTrigger.OnInteracted     += OnInteractionTriggered;
                    interactionTrigger.OnTriggerEntered += OnInteractionTriggerEntered;
                    interactionTrigger.OnTriggerExited  += OnInteractionTriggerExited;
                }
            }

            m_PlayerEntityId       = m_FieldContext.PlayerEntity.EntityId;
            m_PlayerMovementDriver = MovementDriverFactory.Create(m_Entities[m_PlayerEntityId].Entity.gameObject, 3f);

            foreach ((int entityId, Vector3 position) in m_FieldContext.EntityPositions)
            {
                OnRequestSetEntityPosition(entityId, position);
            }

            foreach ((int entityId, Quaternion rotation) in m_FieldContext.EntityRotations)
            {
                OnRequestSetEntityRotation(entityId, rotation);
            }

            foreach ((int entityId, RotationState rotationState) in m_FieldContext.EntityRotationStates)
            {
                m_Entities[entityId].MovementDriver.ResumeRotation(rotationState);
            }

            foreach (int entityId in m_FieldContext.VisibleEntityIds)
            {
                OnRequestSetEntityVisible(entityId, true);
            }

            await PostFieldLoadAsync();
        }

        private async Task PostFieldLoadAsync()
        {
            m_ActiveInteractionTriggerIds = new HashSet<int>();

            SubscribeVm();

            m_MainMenuAccessible = true;

            UpdateManager.RegisterUpdatable(this);

            m_CurrentInputContext = new FieldExplorationInputContext(GetBestInteractionTrigger, OpenConfigMenu, OnMove);
            m_InputRouter.Push(m_CurrentInputContext);

            await m_ScreenFadeService.FadeInAsync();

            m_InputAdapter.Enable();
        }

        private async Task UnloadCurrentFieldAsync()
        {
            m_InputAdapter.Disable();

            m_CurrentInputContext = m_InputRouter.Pop(m_CurrentInputContext);

            UpdateManager.QueueForUnregisterUpdatable(this);

            UnsubscribeVm();

            m_ActiveInteractionTriggerIds.Clear();

            foreach (KeyValuePair<int, FieldEntityComponents> entity in m_Entities)
            {
                if (entity.Value.InteractionTrigger != null)
                {
                    entity.Value.InteractionTrigger.OnTriggerExited  -= OnInteractionTriggerExited;
                    entity.Value.InteractionTrigger.OnTriggerEntered -= OnInteractionTriggerEntered;
                    entity.Value.InteractionTrigger.OnInteracted     -= OnInteractionTriggered;
                }

                if (entity.Value.GatewayTrigger != null)
                {
                    entity.Value.GatewayTrigger.OnTriggered -= OnGatewayTriggered;
                }
            }

            m_FieldContext = null;

            await m_ScreenFadeService.FadeOutAsync();

            await m_FieldPresentation.Unload();

            m_LocalisationService.UnloadLocalisationData(m_FieldDatabaseAsset.LocalisationSheets);
        }

        private bool IsDialogueWindowOpen(ulong id)
        {
            return m_DialogueWindows.ContainsKey(id);
        }

        private bool IsEntityRotating(int entityId)
        {
            FieldEntityComponents entity = m_Entities[entityId];

            if (entity.MovementDriver == null)
            {
                return false;
            }

            RotationState rotationsState = entity.MovementDriver.GetRotationState();

            return rotationsState.Active;
        }

        private void OnRequestMusic(int id)
        {
            m_MusicPlayer.Play(id).FireAndForget();
        }

        private void OnRequestSfx(int id)
        {
            m_SfxPlayer.Play(id);
        }

        private void OnRequestSetPlayerEntity(FieldEntityRuntime entity)
        {
            if (m_PlayerMovementDriver != null)
            {
                Component currentDriver = (Component)m_PlayerMovementDriver;
                Object.Destroy(currentDriver);
            }

            m_FieldContext.SetPlayerEntity(entity);

            m_PlayerEntityId = entity.EntityId;
            FieldEntity newPlayerEntity = m_Entities[m_PlayerEntityId].Entity;

            Vector3    position;
            Quaternion rotation;

            // TODO:
            // this method is doing 2 things
            // an entity position shouldn't change because it became a player
            // add an op code that lets the vm set an entity to a spawn point
            {
                position = m_InitialPlayerSpawn.Position;
                rotation = m_InitialPlayerSpawn.Rotation;
            }

            newPlayerEntity.transform.SetPositionAndRotation(position, rotation);

            m_PlayerMovementDriver = MovementDriverFactory.Create(newPlayerEntity.gameObject, 3f);
        }

        private void OnRequestSetEntityVisible(int entityId, bool visible)
        {
            m_FieldContext.SetEntityVisible(entityId, visible);
            m_Entities[entityId].Entity.SetVisible(visible);
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
            FieldEntity player = m_Entities[m_FieldContext.PlayerEntity.EntityId].Entity;
            FieldEntity entity = m_Entities[entityId].Entity;

            Transform playerTransform = player.transform;

            Vector3 playerPos = playerTransform.position;
            Vector3 entityPos = entity.transform.position;

            return IsFacing(playerPos, playerTransform.forward, entityPos, m_FieldModuleMonoBehaviour.PlayerInteractionAngle);
        }

        private bool IsEntityFacingPlayer(int entityId)
        {
            FieldEntity           player      = m_Entities[m_FieldContext.PlayerEntity.EntityId].Entity;
            FieldEntityComponents entity      = m_Entities[entityId];
            FieldEntity           fieldEntity = m_Entities[entityId].Entity;

            Transform entityTransform = fieldEntity.transform;

            Vector3 playerPos = player.transform.position;
            Vector3 entityPos = entityTransform.position;

            if (entity.InteractionTrigger == null)
            {
                return false;
            }

            return IsFacing(entityPos, entityTransform.forward, playerPos, entity.InteractionTrigger.InteractionAngle);
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

            FieldEntity player          = m_Entities[m_FieldContext.PlayerEntity.EntityId].Entity;
            Transform   playerTransform = player.transform;

            Vector3 playerPos     = playerTransform.position;
            Vector3 playerForward = Vector3.ProjectOnPlane(playerTransform.forward, m_FieldModuleMonoBehaviour.Up);
            playerForward = playerForward.normalized;

            FieldInteractionTrigger best      = null;
            float                   bestScore = float.MinValue;

            foreach (int entityId in m_ActiveInteractionTriggerIds)
            {
                FieldInteractionTrigger entity = m_Entities[entityId].InteractionTrigger;

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

        // TODO: when we have the main menu/party menu, it should load that instead
        private void OpenConfigMenu()
        {
            if (!m_MainMenuAccessible)
            {
                return;
            }

            byte type = (byte)MenuType.Config;

            RequestMenuModule(type);
        }

        private void StoreToTempMemory()
        {
            foreach (KeyValuePair<int, FieldEntityComponents> entity in m_Entities)
            {
                m_FieldContext.SetEntityPositionAndRotation(entity.Key, entity.Value.Entity.transform);

                if (entity.Value.MovementDriver != null)
                {
                    RotationState rotationState = entity.Value.MovementDriver.GetRotationState();

                    m_FieldContext.SetEntityRotationState(entity.Key, rotationState);
                }
            }

            m_FieldResumeDataStore.Set(m_FieldContext);
        }

        private void OnMove(Vector2 move)
        {
            Vector3 up = m_FieldModuleMonoBehaviour.Up;

            Vector3 forward = Vector3.ProjectOnPlane(m_CameraTransformHandle.forward, up).normalized;
            Vector3 right   = Vector3.ProjectOnPlane(m_CameraTransformHandle.right,   up).normalized;

            Vector3 worldMove = forward * move.y + right * move.x;

            MovePlayer(worldMove);
        }

        private void MovePlayer(Vector3 worldMove)
        {
            m_PlayerMovementDriver.SetMoveInput(worldMove);
        }

        private void OnRequestSetGatewayTriggersActive(bool active)
        {
            foreach (KeyValuePair<int, FieldEntityComponents> entity in m_Entities)
            {
                if (entity.Value.GatewayTrigger != null)
                {
                    entity.Value.GatewayTrigger.SetActive(active);
                }
            }
        }

        private void OnRequestSetInteractionTriggerActive(int entityId, bool active)
        {
            m_Entities[entityId].InteractionTrigger.SetActive(active);
        }

        private void OnRequestSetInteractionRange(int entityId, float range)
        {
            m_Entities[entityId].InteractionTrigger.SetInteractionRange(range);
        }

        private void OnRequestInputLock(bool lockInput)
        {
            if (lockInput)
            {
                MovePlayer(Vector3.zero);
                m_CurrentInputContext = new BlockAllInputContext();
                m_InputRouter.Push(m_CurrentInputContext);
            }
            else
            {
                BlockAllInputContext currentInputContext = m_CurrentInputContext as BlockAllInputContext;
                if (currentInputContext == null)
                {
                    Debug.LogError($"{nameof(FieldModule)}::{nameof(OnRequestInputLock)} cannot pop {nameof(BlockAllInputContext)}, current input context is {m_CurrentInputContext.GetType()}");
                    return;
                }

                m_CurrentInputContext = m_InputRouter.Pop(m_CurrentInputContext);
            }
        }

        private void OnRequestSetEntityPosition(int entityId, Vector3 position)
        {
            GetMovementDriver(entityId).SetPosition(position);
        }

        private void OnRequestSetEntityRotation(int entityId, Quaternion rotation)
        {
            GetMovementDriver(entityId).SetRotation(rotation);
        }

        private void OnRequestSetEntityRotationAsync(int entityId, SetEntityRotationAsyncArgs args)
        {
            GetMovementDriver(entityId).StartRotation(args);
        }

        private void OnRequestSetEntityToFaceEntity(int entityId, int targetEntityId)
        {
            FieldEntity fieldEntity       = m_Entities[entityId].Entity;
            FieldEntity targetFieldEntity = m_Entities[targetEntityId].Entity;

            Vector3 direction = targetFieldEntity.transform.position - fieldEntity.transform.position;
            direction = Vector3.ProjectOnPlane(direction, m_FieldModuleMonoBehaviour.Up);

            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(direction, m_FieldModuleMonoBehaviour.Up);

            OnRequestSetEntityRotation(entityId, rotation);
        }

        private void OnRequestSetEntityMovementSpeed(int entityId, float movementSpeed)
        {
            GetMovementDriver(entityId).SetMoveSpeed(movementSpeed);
        }

        private IMovementDriver GetMovementDriver(int entityId)
        {
            FieldEntityComponents entity         = m_Entities[entityId];
            IMovementDriver       movementDriver = entity.MovementDriver;

            if (entity.MovementDriver == null)
            {
                movementDriver = MovementDriverFactory.Create(entity.Entity.gameObject, 3f);
                entity.SetMovementDriver(movementDriver);
            }

            return movementDriver;
        }

        private void OnRequestSetMainMenuAccessibility(bool enabled)
        {
            m_MainMenuAccessible = enabled;
        }

        private void OnRequestCreateDialogueWindow(DialogueWindowArgs args)
        {
            IDialogueWindow window = m_DIResolver.Resolve<IDialogueWindow>();
            window.Init(m_RootElement);
            window.SetRect(args.Rect);

            m_DialogueWindows.Add(args.DialogueId, window);
        }

        private void OnRequestShowDialogueWindow(ulong id, bool blockMovement)
        {
            RequestShowDialogueWindowAsync(id, blockMovement).FireAndForget();
        }

        private async Task RequestShowDialogueWindowAsync(ulong id, bool blockMovement)
        {
            if (blockMovement)
            {
                OnRequestInputLock(true);
            }

            IDialogueWindow dialogueWindow = m_DialogueWindows[id];
            string          text           = m_LocalisationService.Get(id);

            await dialogueWindow.AnimateWindowOpenAsync();

            DialogueInputContext fieldDialogueInputContext = new DialogueInputContext();

            m_CurrentInputContext = fieldDialogueInputContext;
            m_InputRouter.Push(m_CurrentInputContext);

            IDialogueFlow dialogueFlow = new TextDialogueFlow();

            await dialogueWindow.RunAsync(dialogueFlow, new[] { text }, fieldDialogueInputContext);

            await RequestCloseDialogueWindowAsync(id);

            m_CurrentInputContext = m_InputRouter.Pop(m_CurrentInputContext);

            if (blockMovement)
            {
                OnRequestInputLock(false);
            }
        }

        private void OnRequestAskPlayerToMakeAChoice(byte bank, ushort addressToStoreChoice, ulong dialogueId, ulong[] answerIds)
        {
            RequestAskPlayerToMakeAChoiceAsync(bank, addressToStoreChoice, dialogueId, answerIds).FireAndForget();
        }

        private async Task RequestAskPlayerToMakeAChoiceAsync(byte bank, ushort addressToStoreChoice, ulong dialogueId, ulong[] answerIds)
        {
            OnRequestInputLock(true);

            IDialogueWindow dialogueWindow = m_DialogueWindows[dialogueId];

            await dialogueWindow.AnimateWindowOpenAsync();

            DialogueInputContext fieldDialogueInputContext = new DialogueInputContext();

            m_CurrentInputContext = fieldDialogueInputContext;
            m_InputRouter.Push(m_CurrentInputContext);

            string[] dialogues = new string[answerIds.Length + 1];
            dialogues[0] = m_LocalisationService.Get(dialogueId);

            for (int i = 1; i < dialogues.Length; i++)
            {
                dialogues[i] = m_LocalisationService.Get(answerIds[i - 1]);
            }

            IDialogueFlow dialogueFlow = new ChoiceDialogueFlow();

            await dialogueWindow.RunAsync(dialogueFlow, dialogues, fieldDialogueInputContext);

            byte       selectedChoice = dialogueWindow.GetSelectedChoice();
            MemoryBank memoryBank     = (MemoryBank)bank;
            m_MemoryService.WriteByte(memoryBank, addressToStoreChoice, selectedChoice);

            await RequestCloseDialogueWindowAsync(dialogueId);

            m_CurrentInputContext = m_InputRouter.Pop(m_CurrentInputContext);

            OnRequestInputLock(false);
        }

        private async Task RequestCloseDialogueWindowAsync(ulong id)
        {
            IDialogueWindow dialogueWindow = m_DialogueWindows[id];

            await dialogueWindow.AnimateWindowClosedAsync();

            m_DialogueWindows.Remove(id);

            dialogueWindow.Destroy();
        }

        private void OnRequestSetBattleModeOptions(BattleArgs args)
        {
            m_BattleArgsProvider.Set(args);
        }

        private void OnRequestStartBattle()
        {
            m_ScreenFadeService.SetFadeToBattleStart();
            m_BattleTransitionRequested = true;
        }
    }
}