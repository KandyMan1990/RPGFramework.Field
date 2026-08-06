using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGFramework.Audio;
using RPGFramework.Battle.SharedTypes;
using RPGFramework.Battle.SharedTypes.Providers;
using RPGFramework.Core;
using RPGFramework.Core.Data;
using RPGFramework.Core.Dialogue;
using RPGFramework.Core.Dialogue.Flows;
using RPGFramework.Core.Input;
using RPGFramework.Core.PlayerLoop;
using RPGFramework.Core.Rendering;
using RPGFramework.Core.SaveData;
using RPGFramework.Core.SharedTypes;
using RPGFramework.DI;
using RPGFramework.Field.FieldVmArgs;
using RPGFramework.Field.SharedTypes;
using RPGFramework.Field.SharedTypes.Providers;
using RPGFramework.Localisation;
using RPGFramework.Menu.SharedTypes;
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
        private readonly IFieldModule                       m_This;
        private readonly Dictionary<ulong, IDialogueWindow> m_DialogueWindows;
        private readonly IMemoryService                     m_MemoryService;
        private readonly ISaveDataService                   m_SaveDataService;
        private readonly IScreenFadeService                 m_ScreenFadeService;
        private readonly IBattleArgsProvider                m_BattleArgsProvider;
        private readonly IFieldArgsProvider                 m_FieldArgsProvider;
        private readonly IMenuArgsProvider                  m_MenuArgsProvider;

        private FieldModuleMonoBehaviour m_FieldModuleMonoBehaviour;
        private IInputContext            m_CurrentInputContext;
        private Camera                   m_Camera;
        private VisualElement            m_RootElement;

        private InputAdapter                             m_InputAdapter;
        private FieldContext                             m_FieldContext;
        private SpawnPoint                               m_SpawnPoint;
        private Dictionary<int, FieldEntity>             m_EntityGameObjects;
        private Dictionary<int, FieldGatewayTrigger>     m_EntityGatewayTriggers;
        private Dictionary<int, FieldInteractionTrigger> m_EntityInteractionTriggers;
        private HashSet<int>                             m_ActiveInteractionTriggerIds;
        private int                                      m_PlayerEntityId;

        private FieldArgs          m_FieldArgs;
        private bool               m_FieldTransitionRequested;
        private FieldDatabaseAsset m_FieldDatabaseAsset;

        private bool m_BattleTransitionRequested;

        private IMovementDriver                  m_PlayerMovementDriver;
        private Dictionary<int, IMovementDriver> m_EntityMovementDrivers;

        private bool m_MainMenuAccessible;

        public FieldModule(ICoreModule          coreModule,
                           IDIResolver          diResolver,
                           IInputRouter         inputRouter,
                           IMusicPlayer         musicPlayer,
                           ISfxPlayer           sfxPlayer,
                           IFieldDatabase       fieldDatabase,
                           IFieldPresentation   fieldPresentation,
                           ILocalisationService localisationService,
                           IMemoryService       memoryService,
                           ISaveDataService     saveDataService,
                           IScreenFadeService   screenFadeService,
                           IBattleArgsProvider  battleArgsProvider,
                           IFieldArgsProvider   fieldArgsProvider,
                           IMenuArgsProvider    menuArgsProvider)
        {
            m_CoreModule          = coreModule;
            m_DIResolver          = diResolver;
            m_InputRouter         = inputRouter;
            m_MusicPlayer         = musicPlayer;
            m_SfxPlayer           = sfxPlayer;
            m_FieldDatabase       = fieldDatabase;
            m_FieldPresentation   = fieldPresentation;
            m_LocalisationService = localisationService;
            m_MemoryService       = memoryService;
            m_SaveDataService     = saveDataService;
            m_ScreenFadeService   = screenFadeService;
            m_BattleArgsProvider  = battleArgsProvider;
            m_FieldArgsProvider   = fieldArgsProvider;
            m_MenuArgsProvider    = menuArgsProvider;
            m_DialogueWindows     = new Dictionary<ulong, IDialogueWindow>(8);
            m_This                = this;
        }

        async Task IModule.OnEnterAsync()
        {
            await m_ScreenFadeService.FadeOutAsync(true);

            m_InputAdapter = Object.FindAnyObjectByType<InputAdapter>();
            m_DIResolver.InjectInto(m_InputAdapter);

            m_FieldModuleMonoBehaviour = Object.FindAnyObjectByType<FieldModuleMonoBehaviour>();

            UIDocument uiDoc = Object.FindAnyObjectByType<UIDocument>();
            m_RootElement = uiDoc.rootVisualElement.Q("Root");

            m_FieldArgs = m_FieldArgsProvider.Get;

            await LoadNewFieldAsync();
        }

        async Task IModule.OnExitAsync()
        {
            await UnloadCurrentFieldAsync();

            m_InputRouter.Clear();

            m_CoreModule.ResetModule<IFieldModule, FieldModule>();
        }

        Task IFieldModule.LoadMenuModuleAsync(byte menuId)
        {
            MenuArgs args = new MenuArgs(menuId);
            m_MenuArgsProvider.Set(args);

            return m_CoreModule.LoadModuleAsync<IMenuModule>();
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

            if (m_BattleTransitionRequested)
            {
                StoreToTempMemory();
                m_CoreModule.LoadModuleAsync<IBattleModule>().FireAndForget();
            }
        }

        private void SetFieldModuleArgs(FieldArgs args)
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

        private async Task LoadNewFieldAsync()
        {
            m_FieldDatabaseAsset = m_FieldDatabase.Get(m_FieldArgs.FieldId);

            await m_LocalisationService.LoadNewLocalisationDataAsync(m_FieldDatabaseAsset.LocalisationSheets);

            GameObject   fieldGameObject = await m_FieldPresentation.LoadAsync(m_FieldDatabaseAsset);
            SpawnPoint[] spawnPoints     = fieldGameObject.GetComponentsInChildren<SpawnPoint>();

            FieldVM vm = new FieldVM(m_MemoryService);

            m_SpawnPoint = Array.Find(spawnPoints, sp => sp.Id == m_FieldArgs.SpawnId);

            FieldEntity[] entitiesInGameObject = fieldGameObject.GetComponentsInChildren<FieldEntity>();

            m_EntityGameObjects         = new Dictionary<int, FieldEntity>(entitiesInGameObject.Length);
            m_EntityGatewayTriggers     = new Dictionary<int, FieldGatewayTrigger>();
            m_EntityInteractionTriggers = new Dictionary<int, FieldInteractionTrigger>();
            m_EntityMovementDrivers     = new Dictionary<int, IMovementDriver>();

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

                if (m_FieldArgs.SpawnId != -1)
                {
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
            }

            if (m_FieldArgs.SpawnId == -1)
            {
                m_FieldContext = m_MemoryService.GetTempModuleData<FieldContext>();
                RequestSetPlayerEntity(m_FieldContext.PlayerEntity);
                foreach ((int entityId, Vector3 position) in m_FieldContext.EntityPositions)
                {
                    RequestSetEntityPosition(entityId, position);
                }

                foreach ((int entityId, Quaternion rotation) in m_FieldContext.EntityRotations)
                {
                    RequestSetEntityRotation(entityId, rotation);
                }

                foreach ((int entityId, RotationState rotationState) in m_FieldContext.EntityRotationStates)
                {
                    m_EntityMovementDrivers[entityId].ResumeRotation(rotationState);
                }

                foreach (int entityId in m_FieldContext.VisibleEntityIds)
                {
                    RequestSetEntityVisible(entityId, true);
                }
            }
            else
            {
                m_FieldContext = new FieldContext(vm, entities);
            }

            m_ActiveInteractionTriggerIds = new HashSet<int>();

            m_FieldContext.VM.RequestFieldTransition             += SetFieldModuleArgs;
            m_FieldContext.VM.RequestMusic                       += RequestMusic;
            m_FieldContext.VM.RequestSfx                         += RequestSfx;
            m_FieldContext.VM.RequestSetPlayerEntity             += RequestSetPlayerEntity;
            m_FieldContext.VM.RequestSetEntityVisible            += RequestSetEntityVisible;
            m_FieldContext.VM.RequestSetGatewayTriggersActive    += RequestSetGatewayTriggersActive;
            m_FieldContext.VM.RequestSetInteractionTriggerActive += RequestSetInteractionTriggerActive;
            m_FieldContext.VM.RequestSetInteractionRange         += RequestSetInteractionRange;
            m_FieldContext.VM.RequestInputLock                   += RequestInputLock;
            m_FieldContext.VM.RequestSetEntityPosition           += RequestSetEntityPosition;
            m_FieldContext.VM.RequestSetEntityRotation           += RequestSetEntityRotation;
            m_FieldContext.VM.RequestSetEntityRotationAsync      += RequestSetEntityRotationAsync;
            m_FieldContext.VM.IsEntityRotating                   =  IsEntityRotating;
            m_FieldContext.VM.RequestSetEntityToFaceEntity       += RequestSetEntityToFaceEntity;
            m_FieldContext.VM.RequestSetEntityMovementSpeed      += RequestSetEntityMovementSpeed;
            m_FieldContext.VM.RequestSetMainMenuAccessibility    += RequestSetMainMenuAccessibility;
            m_FieldContext.VM.RequestCreateDialogueWindow        += RequestCreateDialogueWindow;
            m_FieldContext.VM.RequestShowDialogueWindow          += RequestShowDialogueWindow;
            m_FieldContext.VM.IsDialogueWindowOpen               =  IsDialogueWindowOpen;
            m_FieldContext.VM.RequestAskPlayerToMakeAChoice      += RequestAskPlayerToMakeAChoice;
            m_FieldContext.VM.IsPlayerMakingAChoice              =  IsDialogueWindowOpen;
            m_FieldContext.VM.RequestSetBattleModeOptions        += RequestSetBattleModeOptions;
            m_FieldContext.VM.RequestStartBattle                 += RequestStartBattle;

            m_Camera = Object.FindAnyObjectByType<Camera>();

            UpdateManager.RegisterUpdatable(this);

            m_MainMenuAccessible = true;

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

            m_FieldContext.VM.RequestStartBattle                 -= RequestStartBattle;
            m_FieldContext.VM.RequestSetBattleModeOptions        -= RequestSetBattleModeOptions;
            m_FieldContext.VM.IsPlayerMakingAChoice              =  null;
            m_FieldContext.VM.RequestAskPlayerToMakeAChoice      -= RequestAskPlayerToMakeAChoice;
            m_FieldContext.VM.IsDialogueWindowOpen               =  null;
            m_FieldContext.VM.RequestShowDialogueWindow          -= RequestShowDialogueWindow;
            m_FieldContext.VM.RequestCreateDialogueWindow        -= RequestCreateDialogueWindow;
            m_FieldContext.VM.RequestSetMainMenuAccessibility    -= RequestSetMainMenuAccessibility;
            m_FieldContext.VM.RequestSetEntityMovementSpeed      -= RequestSetEntityMovementSpeed;
            m_FieldContext.VM.RequestSetEntityToFaceEntity       -= RequestSetEntityToFaceEntity;
            m_FieldContext.VM.IsEntityRotating                   =  null;
            m_FieldContext.VM.RequestSetEntityRotationAsync      -= RequestSetEntityRotationAsync;
            m_FieldContext.VM.RequestSetEntityRotation           -= RequestSetEntityRotation;
            m_FieldContext.VM.RequestSetEntityPosition           -= RequestSetEntityPosition;
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
            RotationState rotationsState = m_EntityMovementDrivers[entityId].GetRotationState();

            return rotationsState.Active;
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

            m_PlayerEntityId = entity.EntityId;
            FieldEntity newPlayerEntity = m_EntityGameObjects[entity.EntityId];

            Vector3    position;
            Quaternion rotation;

            if (m_FieldArgs.SpawnId == -1)
            {
                position = m_FieldArgs.Position;
                rotation = m_FieldArgs.Rotation;
            }
            else
            {
                position = m_SpawnPoint.Position;
                rotation = m_SpawnPoint.Rotation;
            }

            newPlayerEntity.transform.SetPositionAndRotation(position, rotation);

            m_PlayerMovementDriver = MovementDriverFactory.Create(newPlayerEntity.gameObject, 3f);
        }

        private void RequestSetEntityVisible(int entityId, bool visible)
        {
            m_FieldContext.SetEntityVisible(entityId, visible);
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

        // TODO: when we have the main menu/party menu, it should load that instead
        private void OpenConfigMenu()
        {
            if (!m_MainMenuAccessible)
            {
                return;
            }

            StoreToTempMemory();

            byte type = (byte)MenuType.Config;

            m_This.LoadMenuModuleAsync(type).FireAndForget();
        }

        private void StoreToTempMemory()
        {
            // TODO: core should probably always try to resume via memory service first, fallback to save service if there is nothing in memory service

            m_SaveDataService.TryGetSection(FrameworkSaveSectionDatabase.RESUME_DATA, out SaveSection<RuntimeResumeData> saveSection);

            Transform  playerTransform = m_EntityGameObjects[m_PlayerEntityId].transform;
            Vector3    playerPosition  = playerTransform.position;
            Quaternion playerRotation  = playerTransform.rotation;

            saveSection.Data.Index     = m_FieldArgs.FieldId;
            saveSection.Data.SpawnId   = -1;
            saveSection.Data.PositionX = playerPosition.x;
            saveSection.Data.PositionY = playerPosition.y;
            saveSection.Data.PositionZ = playerPosition.z;
            saveSection.Data.RotationX = playerRotation.x;
            saveSection.Data.RotationY = playerRotation.y;
            saveSection.Data.RotationZ = playerRotation.z;
            saveSection.Data.RotationW = playerRotation.w;

            m_SaveDataService.SetSection(FrameworkSaveSectionDatabase.RESUME_DATA, saveSection);

            foreach (KeyValuePair<int, FieldEntity> kvp in m_EntityGameObjects)
            {
                m_FieldContext.SetEntityPositionAndRotation(kvp.Key, kvp.Value.transform);
            }

            foreach ((int entityId, IMovementDriver driver) in m_EntityMovementDrivers)
            {
                RotationState rotationState = driver.GetRotationState();

                m_FieldContext.SetEntityRotationState(entityId, rotationState);
            }

            m_MemoryService.SetTempModuleData(m_FieldContext);
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

        private void RequestInputLock(bool lockInput)
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
                    Debug.LogError($"{nameof(FieldModule)}::{nameof(RequestInputLock)} cannot pop {nameof(BlockAllInputContext)}, current input context is {m_CurrentInputContext.GetType()}");
                    return;
                }

                m_CurrentInputContext = m_InputRouter.Pop(m_CurrentInputContext);
            }
        }

        private void RequestSetEntityPosition(int entityId, Vector3 position)
        {
            if (!m_EntityMovementDrivers.TryGetValue(entityId, out IMovementDriver movementDriver))
            {
                FieldEntity entity = m_EntityGameObjects[entityId];
                movementDriver = MovementDriverFactory.Create(entity.gameObject, 3f);
                m_EntityMovementDrivers.Add(entityId, movementDriver);
            }

            movementDriver.SetPosition(position);
        }

        private void RequestSetEntityRotation(int entityId, Quaternion rotation)
        {
            if (!m_EntityMovementDrivers.TryGetValue(entityId, out IMovementDriver movementDriver))
            {
                FieldEntity entity = m_EntityGameObjects[entityId];
                movementDriver = MovementDriverFactory.Create(entity.gameObject, 3f);
                m_EntityMovementDrivers.Add(entityId, movementDriver);
            }

            movementDriver.SetRotation(rotation);
        }

        private void RequestSetEntityRotationAsync(int entityId, SetEntityRotationAsyncArgs args)
        {
            if (!m_EntityMovementDrivers.TryGetValue(entityId, out IMovementDriver movementDriver))
            {
                FieldEntity entity = m_EntityGameObjects[entityId];
                movementDriver = MovementDriverFactory.Create(entity.gameObject, 3f);
                m_EntityMovementDrivers.Add(entityId, movementDriver);
            }

            movementDriver.StartRotation(args);
        }

        private void RequestSetEntityToFaceEntity(int entityId, int targetEntityId)
        {
            Vector3 direction = m_EntityGameObjects[targetEntityId].transform.position - m_EntityGameObjects[entityId].transform.position;
            direction = Vector3.ProjectOnPlane(direction, m_FieldModuleMonoBehaviour.Up);

            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(direction, m_FieldModuleMonoBehaviour.Up);

            RequestSetEntityRotation(entityId, rotation);
        }

        private void RequestSetEntityMovementSpeed(int entityId, float movementSpeed)
        {
            if (!m_EntityMovementDrivers.TryGetValue(entityId, out IMovementDriver movementDriver))
            {
                FieldEntity entity = m_EntityGameObjects[entityId];
                movementDriver = MovementDriverFactory.Create(entity.gameObject, 3f);
                m_EntityMovementDrivers.Add(entityId, movementDriver);
            }

            movementDriver.SetMoveSpeed(movementSpeed);
        }

        private void RequestSetMainMenuAccessibility(bool enabled)
        {
            m_MainMenuAccessible = enabled;
        }

        private void RequestCreateDialogueWindow(DialogueWindowArgs args)
        {
            IDialogueWindow window = m_DIResolver.Resolve<IDialogueWindow>();
            window.Init(m_RootElement);
            window.SetRect(args.Rect);

            m_DialogueWindows.Add(args.DialogueId, window);
        }

        private void RequestShowDialogueWindow(ulong id, bool blockMovement)
        {
            RequestShowDialogueWindowAsync(id, blockMovement).FireAndForget();
        }

        private async Task RequestShowDialogueWindowAsync(ulong id, bool blockMovement)
        {
            if (blockMovement)
            {
                RequestInputLock(true);
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
                RequestInputLock(false);
            }
        }

        private void RequestAskPlayerToMakeAChoice(byte bank, ushort addressToStoreChoice, ulong dialogueId, ulong[] answerIds)
        {
            RequestAskPlayerToMakeAChoiceAsync(bank, addressToStoreChoice, dialogueId, answerIds).FireAndForget();
        }

        private async Task RequestAskPlayerToMakeAChoiceAsync(byte bank, ushort addressToStoreChoice, ulong dialogueId, ulong[] answerIds)
        {
            RequestInputLock(true);

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

            RequestInputLock(false);
        }

        private async Task RequestCloseDialogueWindowAsync(ulong id)
        {
            IDialogueWindow dialogueWindow = m_DialogueWindows[id];

            await dialogueWindow.AnimateWindowClosedAsync();

            m_DialogueWindows.Remove(id);

            dialogueWindow.Destroy();
        }

        private void RequestSetBattleModeOptions(BattleArgs args)
        {
            m_BattleArgsProvider.Set(args);
        }

        private void RequestStartBattle()
        {
            m_ScreenFadeService.SetFadeToBattleStart();
            m_BattleTransitionRequested = true;
        }
    }
}