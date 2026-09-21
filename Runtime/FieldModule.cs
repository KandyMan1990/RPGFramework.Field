using System;
using System.Collections.Generic;
using System.Threading;
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
    public partial class FieldModule : IFieldModule, IUpdatable, IFixedUpdatable
    {
        private readonly ICoreModule            m_CoreModule;
        private readonly IDIResolver            m_DIResolver;
        private readonly IInputRouter           m_InputRouter;
        private readonly IMusicPlayer           m_MusicPlayer;
        private readonly ISfxPlayer             m_SfxPlayer;
        private readonly IFieldDatabase         m_FieldDatabase;
        private readonly IFieldPresentation     m_FieldPresentation;
        private readonly ILocalisationService   m_LocalisationService;
        private readonly FieldDialogueChannel[] m_DialogueChannels;
        private readonly int[]                  m_MessageVariables;
        private readonly IMemoryService         m_MemoryService;
        private readonly ITempMemoryArgs        m_TempMemoryArgs;
        private readonly IScreenFadeService     m_ScreenFadeService;
        private readonly IBattleArgsProvider    m_BattleArgsProvider;
        private readonly IFieldArgsStore        m_FieldArgsStore;
        private readonly IMenuArgsProvider      m_MenuArgsProvider;
        private readonly IChangeModuleStore     m_ChangeModuleStore;
        private readonly IResumeModuleStore     m_ResumeModuleStore;
        private readonly ICurrentModuleStore    m_CurrentModuleStore;
        private readonly IFieldResumeDataStore  m_FieldResumeDataStore;

        private FieldModuleMonoBehaviour m_FieldModuleMonoBehaviour;
        private IInputContext            m_ExplorationInputContext;
        private BlockAllInputContext     m_ScriptInputLock;

        // Shared by every open dialogue window, so one press reaches them all; see DialogueInputContext.
        private readonly DialogueInputContext m_DialogueInputContext = new DialogueInputContext();
        private          int                  m_OpenDialogueWindows;
        private          TransformHandle      m_CameraTransformHandle;
        private          VisualElement        m_RootElement;

        private InputAdapter                           m_InputAdapter;
        private FieldContext                           m_FieldContext;
        private SpawnPoint                             m_InitialPlayerSpawn;
        private Dictionary<int, FieldEntityComponents> m_Entities;
        private int                                    m_PlayerEntityId;

        private bool               m_FieldTransitionRequested;
        private FieldDatabaseAsset m_FieldDatabaseAsset;

        private bool m_BattleTransitionRequested;
        private bool m_MenuTransitionRequested;

        private IMovementDriver m_PlayerMovementDriver;


        public FieldModule(ICoreModule           coreModule,
                           IDIResolver           diResolver,
                           IInputRouter          inputRouter,
                           IMusicPlayer          musicPlayer,
                           ISfxPlayer            sfxPlayer,
                           IFieldDatabase        fieldDatabase,
                           IFieldPresentation    fieldPresentation,
                           ILocalisationService  localisationService,
                           IMemoryService        memoryService,
                           ITempMemoryArgs       tempMemoryArgs,
                           IScreenFadeService    screenFadeService,
                           IBattleArgsProvider   battleArgsProvider,
                           IFieldArgsStore       fieldArgsStore,
                           IMenuArgsProvider     menuArgsProvider,
                           IChangeModuleStore    changeModuleStore,
                           IResumeModuleStore    resumeModuleStore,
                           ICurrentModuleStore   currentModuleStore,
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
            m_TempMemoryArgs       = tempMemoryArgs;
            m_ScreenFadeService    = screenFadeService;
            m_BattleArgsProvider   = battleArgsProvider;
            m_FieldArgsStore       = fieldArgsStore;
            m_MenuArgsProvider     = menuArgsProvider;
            m_ChangeModuleStore    = changeModuleStore;
            m_ResumeModuleStore    = resumeModuleStore;
            m_CurrentModuleStore   = currentModuleStore;
            m_FieldResumeDataStore = fieldResumeDataStore;
            m_DialogueChannels     = new FieldDialogueChannel[ArgumentTypes.DIALOGUE_CHANNEL_COUNT];
            m_MessageVariables     = new int[DialogueMarkup.MESSAGE_VARIABLE_COUNT];

            for (int i = 0; i < m_DialogueChannels.Length; i++)
            {
                m_DialogueChannels[i] = new FieldDialogueChannel();
            }
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

            float deltaTime = Time.deltaTime;

            foreach (FieldEntityComponents entity in m_Entities.Values)
            {
                entity.MovementDriver?.Tick(deltaTime);
            }

#if UNITY_EDITOR
            DrawInteractionDebug();
#endif

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

        void IFixedUpdatable.FixedUpdate()
        {
            float fixedDeltaTime = Time.fixedDeltaTime;

            foreach (FieldEntityComponents entity in m_Entities.Values)
            {
                entity.MovementDriver?.PhysicsTick(fixedDeltaTime);
            }
        }

        private void SubscribeVm()
        {
            m_FieldContext.VM.RequestFieldTransition             += OnSetFieldModuleArgs;
            m_FieldContext.VM.RequestMusic                       += OnRequestMusic;
            m_FieldContext.VM.RequestMusicStemState              += OnRequestMusicStemState;
            m_FieldContext.VM.RequestSfx                         += OnRequestSfx;
            m_FieldContext.VM.RequestSetPlayerEntity             += OnRequestSetPlayerEntity;
            m_FieldContext.VM.RequestSetEntityVisible            += OnRequestSetEntityVisible;
            m_FieldContext.VM.RequestSetGatewayTriggersActive    += OnRequestSetGatewayTriggersActive;
            m_FieldContext.VM.RequestSetInteractionTriggerActive += OnRequestSetInteractionTriggerActive;
            m_FieldContext.VM.RequestSetCollisionTriggerActive   += OnRequestSetCollisionTriggerActive;
            m_FieldContext.VM.RequestSetInteractionRange         += OnRequestSetInteractionRange;
            m_FieldContext.VM.RequestInputLock                   += OnRequestScriptInputLock;
            m_FieldContext.VM.RequestSetEntityPosition           += OnRequestSetEntityPosition;
            m_FieldContext.VM.RequestSetEntityRotation           += OnRequestSetEntityRotation;
            m_FieldContext.VM.RequestSetEntityRotationAsync      += OnRequestSetEntityRotationAsync;
            m_FieldContext.VM.IsEntityRotating                   =  IsEntityRotating;
            m_FieldContext.VM.RequestSetEntityToFaceEntity       += OnRequestSetEntityToFaceEntity;
            m_FieldContext.VM.RequestSetEntityMovementSpeed      += OnRequestSetEntityMovementSpeed;
            m_FieldContext.VM.RequestSetMainMenuAccessibility    += OnRequestSetMainMenuAccessibility;
            m_FieldContext.VM.RequestCreateDialogueWindow        += OnRequestCreateDialogueWindow;
            m_FieldContext.VM.RequestShowDialogueWindow          += OnRequestShowDialogueWindow;
            m_FieldContext.VM.RequestShowDialogueWindowNoWait    += OnRequestShowDialogueWindowNoWait;
            m_FieldContext.VM.RequestCloseDialogueWindow         += OnRequestCloseDialogueWindow;
            m_FieldContext.VM.RequestSetDialogueWindowStyle      += OnRequestSetDialogueWindowStyle;
            m_FieldContext.VM.RequestSetMessageVariable          += OnRequestSetMessageVariable;
            m_FieldContext.VM.IsDialogueWindowOpen               =  IsDialogueWindowOpen;
            m_FieldContext.VM.RequestAskPlayerToMakeAChoice      += OnRequestAskPlayerToMakeAChoice;
            m_FieldContext.VM.RequestSetBattleModeOptions        += OnRequestSetBattleModeOptions;
            m_FieldContext.VM.RequestStartBattle                 += OnRequestStartBattle;
        }

        private void UnsubscribeVm()
        {
            m_FieldContext.VM.RequestStartBattle                 -= OnRequestStartBattle;
            m_FieldContext.VM.RequestSetBattleModeOptions        -= OnRequestSetBattleModeOptions;
            m_FieldContext.VM.RequestAskPlayerToMakeAChoice      -= OnRequestAskPlayerToMakeAChoice;
            m_FieldContext.VM.IsDialogueWindowOpen               =  null;
            m_FieldContext.VM.RequestShowDialogueWindow          -= OnRequestShowDialogueWindow;
            m_FieldContext.VM.RequestShowDialogueWindowNoWait    -= OnRequestShowDialogueWindowNoWait;
            m_FieldContext.VM.RequestCloseDialogueWindow         -= OnRequestCloseDialogueWindow;
            m_FieldContext.VM.RequestSetDialogueWindowStyle      -= OnRequestSetDialogueWindowStyle;
            m_FieldContext.VM.RequestSetMessageVariable          -= OnRequestSetMessageVariable;
            m_FieldContext.VM.RequestCreateDialogueWindow        -= OnRequestCreateDialogueWindow;
            m_FieldContext.VM.RequestSetMainMenuAccessibility    -= OnRequestSetMainMenuAccessibility;
            m_FieldContext.VM.RequestSetEntityMovementSpeed      -= OnRequestSetEntityMovementSpeed;
            m_FieldContext.VM.RequestSetEntityToFaceEntity       -= OnRequestSetEntityToFaceEntity;
            m_FieldContext.VM.IsEntityRotating                   =  null;
            m_FieldContext.VM.RequestSetEntityRotationAsync      -= OnRequestSetEntityRotationAsync;
            m_FieldContext.VM.RequestSetEntityRotation           -= OnRequestSetEntityRotation;
            m_FieldContext.VM.RequestSetEntityPosition           -= OnRequestSetEntityPosition;
            m_FieldContext.VM.RequestInputLock                   -= OnRequestScriptInputLock;
            m_FieldContext.VM.RequestSetInteractionRange         -= OnRequestSetInteractionRange;
            m_FieldContext.VM.RequestSetCollisionTriggerActive   -= OnRequestSetCollisionTriggerActive;
            m_FieldContext.VM.RequestSetInteractionTriggerActive -= OnRequestSetInteractionTriggerActive;
            m_FieldContext.VM.RequestSetGatewayTriggersActive    -= OnRequestSetGatewayTriggersActive;
            m_FieldContext.VM.RequestSetEntityVisible            -= OnRequestSetEntityVisible;
            m_FieldContext.VM.RequestSetPlayerEntity             -= OnRequestSetPlayerEntity;
            m_FieldContext.VM.RequestSfx                         -= OnRequestSfx;
            m_FieldContext.VM.RequestMusic                       -= OnRequestMusic;
            m_FieldContext.VM.RequestMusicStemState              -= OnRequestMusicStemState;
            m_FieldContext.VM.RequestFieldTransition             -= OnSetFieldModuleArgs;
        }

        private void OnSetFieldModuleArgs(FieldArgs args)
        {
            m_FieldArgsStore.Set(args);
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
            FieldArgs fieldArgs = m_FieldArgsStore.Get;
            m_FieldDatabaseAsset = m_FieldDatabase.Get(fieldArgs.FieldId);

            await m_LocalisationService.LoadNewLocalisationDataAsync(m_FieldDatabaseAsset.LocalisationSheets);

            GameObject   fieldGameObject = await m_FieldPresentation.LoadAsync(m_FieldDatabaseAsset);
            SpawnPoint[] spawnPoints     = fieldGameObject.GetComponentsInChildren<SpawnPoint>();
            m_InitialPlayerSpawn = Array.Find(spawnPoints, sp => sp.Id == fieldArgs.SpawnId);

            FieldEntity[] entitiesInGameObject = fieldGameObject.GetComponentsInChildren<FieldEntity>();
            m_Entities = new Dictionary<int, FieldEntityComponents>(entitiesInGameObject.Length);

            return entitiesInGameObject;
        }

        private async Task LoadNewFieldAsync()
        {
            FieldEntity[] entitiesInGameObject = await PreLoadFieldAsync();

            m_CurrentModuleStore.SetModuleId(FieldConstants.MODULE_ID);

            FieldVM                  vm       = new FieldVM(m_MemoryService, m_TempMemoryArgs.TempBytes);
            List<FieldEntityRuntime> entities = new List<FieldEntityRuntime>(entitiesInGameObject.Length);

            foreach (FieldEntity entity in entitiesInGameObject)
            {
                FieldEntityComponents entityComponents = new FieldEntityComponents();
                entityComponents.SetEntity(entity);

                m_Entities.Add(entity.EntityId, entityComponents);
                FieldCollisionTrigger collisionTrigger = entity.GetComponentInChildren<FieldCollisionTrigger>();

                if (collisionTrigger != null)
                {
                    entityComponents.SetCollisionTrigger(collisionTrigger);
                    collisionTrigger.OnEntered        += OnCollisionTriggerEntered;
                    collisionTrigger.OnGatewayEntered += OnGatewayEntered;
                    collisionTrigger.OnLeft           += OnCollisionTriggerLeft;
                }

                FieldInteractionTrigger interactionTrigger = entity.GetComponentInChildren<FieldInteractionTrigger>();

                if (interactionTrigger != null)
                {
                    entityComponents.SetInteractionTrigger(interactionTrigger);
                    interactionTrigger.OnInteracted += OnInteractionTriggered;
                }

                List<ScriptEntry> scripts          = entity.ScriptDefinition.Scripts;
                int[]             scriptIdsByEvent = new int[scripts.Count];

                for (int i = 0; i < scripts.Count; i++)
                {
                    ScriptEntry scriptEntry = scripts[i];

                    scriptIdsByEvent[i] = scriptEntry.CompiledScript.ScriptId;

                    vm.RegisterScript(scriptEntry.CompiledScript.ScriptId, scriptEntry.CompiledScript);
                }

                FieldEntityRuntime fieldEntityRuntime = new FieldEntityRuntime(entity.EntityId, scriptIdsByEvent);

                entities.Add(fieldEntityRuntime);
                vm.RegisterEntity(entity.EntityId, fieldEntityRuntime);
            }

            m_FieldContext = new FieldContext(vm, entities);

            SubscribeVm();

            InitialiseFieldScripts();
            StartDefaultScripts(entitiesInGameObject);
            InitialisePlayer();

            await PostFieldLoadAsync();
        }

        /// <summary>
        /// Run every entity's init script to completion, before the field is shown or the player has
        /// control.
        /// </summary>
        private void InitialiseFieldScripts()
        {
            FieldVM vm = m_FieldContext.VM;

            foreach (FieldEntityRuntime entity in m_FieldContext.Entities)
            {
                ScriptRunOutcome outcome = entity.RunInitScript(vm);

#if UNITY_EDITOR
                ReportInitScriptDidNotReturn(entity, outcome);
#endif
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// An init script that did not return is one of two different mistakes, and the fix is different
        /// for each, so they are reported separately rather than as one "did not return".
        /// </summary>
        private static void ReportInitScriptDidNotReturn(FieldEntityRuntime entity, ScriptRunOutcome outcome)
        {
            if (outcome == ScriptRunOutcome.Ended)
            {
                return;
            }

            if (outcome == ScriptRunOutcome.Preempted)
            {
                Debug.LogError($"{nameof(FieldModule)}::{nameof(InitialiseFieldScripts)} Entity [{entity.EntityId}]'s init script requested a more urgent script on its own entity, so init was stopped there and nothing after the request ran. Request it from {nameof(FieldScriptType.Default)} instead");

                return;
            }

            if (outcome == ScriptRunOutcome.Waiting)
            {
                Debug.LogError($"{nameof(FieldModule)}::{nameof(InitialiseFieldScripts)} Entity [{entity.EntityId}]'s init script waited or yielded, so it was stopped there and nothing after that ran. Init runs straight through before the field is shown and cannot wait — move the wait, and whatever follows it, into a {nameof(FieldScriptType.Default)} script");

                return;
            }

            Debug.LogError($"{nameof(FieldModule)}::{nameof(InitialiseFieldScripts)} Entity [{entity.EntityId}]'s init script ran {FieldVM.INIT_INSTRUCTION_CEILING} instructions without returning, so it was stopped there. It most likely loops — init has to run straight through to its RETURN; move looping behaviour into a {nameof(FieldScriptType.Default)} script");
        }
#endif

        /// <summary>
        /// Start each entity's <see cref="FieldScriptType.Default" /> script, once every init has run.
        /// <br /><br />
        /// Default is where an entity's ongoing behaviour lives — a patrol route, an idle loop — and unlike
        /// init it is free to wait and to run for as long as the field does. It takes over init's slot,
        /// the least urgent, so a trigger firing preempts it however long it has been looping, and it
        /// resumes where it left off once the trigger's script returns.
        /// </summary>
        private void StartDefaultScripts(FieldEntity[] entitiesInGameObject)
        {
            foreach (FieldEntity entity in entitiesInGameObject)
            {
                entity.ScriptDefinition.TryGetScriptIndex(FieldScriptType.Default, out int eventId);

                m_FieldContext.VM.StartDefaultScript(entity.EntityId, eventId);
            }
        }

        private void InitialisePlayer()
        {
            FieldEntityRuntime playerEntity = m_FieldContext.PlayerEntity;

            if (playerEntity == null)
            {
                m_PlayerEntityId = FieldEntity.NO_ENTITY;
                SetCollisionTriggerPlayerEntityId(m_PlayerEntityId);
                return;
            }

            m_PlayerEntityId = playerEntity.EntityId;

            FieldEntity playerFieldEntity = m_Entities[m_PlayerEntityId].Entity;

            if (m_InitialPlayerSpawn == null)
            {
                FieldArgs fieldArgs = m_FieldArgsStore.Get;
                Debug.LogError($"{nameof(FieldModule)}::{nameof(InitialisePlayer)} No spawn point with id [{fieldArgs.SpawnId}] in this field, so the player keeps whatever position its init script gave it");
            }
            else
            {
                playerFieldEntity.transform.SetPositionAndRotation(m_InitialPlayerSpawn.Position, m_InitialPlayerSpawn.Rotation);
            }

            m_PlayerMovementDriver = GetMovementDriver(m_PlayerEntityId);

            SetCollisionTriggerPlayerEntityId(m_PlayerEntityId);
        }

        private async Task ResumeFieldAsync()
        {
            FieldEntity[] entitiesInGameObject = await PreLoadFieldAsync();

            foreach (FieldEntity entity in entitiesInGameObject)
            {
                FieldEntityComponents entityComponents = new FieldEntityComponents();
                entityComponents.SetEntity(entity);

                m_Entities.Add(entity.EntityId, entityComponents);
                FieldCollisionTrigger collisionTrigger = entity.GetComponentInChildren<FieldCollisionTrigger>();

                if (collisionTrigger != null)
                {
                    entityComponents.SetCollisionTrigger(collisionTrigger);
                    collisionTrigger.OnEntered        += OnCollisionTriggerEntered;
                    collisionTrigger.OnGatewayEntered += OnGatewayEntered;
                    collisionTrigger.OnLeft           += OnCollisionTriggerLeft;
                }

                FieldInteractionTrigger interactionTrigger = entity.GetComponentInChildren<FieldInteractionTrigger>();

                if (interactionTrigger != null)
                {
                    entityComponents.SetInteractionTrigger(interactionTrigger);
                    interactionTrigger.OnInteracted += OnInteractionTriggered;
                }
            }

            SubscribeVm();

            FieldEntityRuntime playerEntity = m_FieldContext.PlayerEntity;

            if (playerEntity != null)
            {
                m_PlayerEntityId       = playerEntity.EntityId;
                m_PlayerMovementDriver = GetMovementDriver(m_PlayerEntityId);
            }
            else
            {
                m_PlayerEntityId = FieldEntity.NO_ENTITY;
            }

            SetCollisionTriggerPlayerEntityId(m_PlayerEntityId);

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
                ShowOrHideEntity(entityId, true);
            }

            foreach (int entityId in m_FieldContext.HiddenEntityIds)
            {
                ShowOrHideEntity(entityId, false);
            }

            // After visibility, which switches interaction too: these hold the last value a script set.
            foreach ((int entityId, bool active) in m_FieldContext.InteractionsActive)
            {
                m_Entities[entityId].InteractionTrigger.SetActive(active);
            }

            foreach ((int entityId, float range) in m_FieldContext.InteractionRanges)
            {
                m_Entities[entityId].InteractionTrigger.SetInteractionRange(range);
            }

            foreach ((int entityId, float speed) in m_FieldContext.MovementSpeeds)
            {
                GetMovementDriver(entityId).SetMoveSpeed(speed);
            }

            foreach ((int entityId, bool active) in m_FieldContext.CollisionTriggersActive)
            {
                m_Entities[entityId].CollisionTrigger.SetActive(active);
            }

            await PostFieldLoadAsync();
        }

        private async Task PostFieldLoadAsync()
        {
            UpdateManager.RegisterUpdatable(this);
            UpdateManager.RegisterFixedUpdatable(this);

            m_ExplorationInputContext = new FieldExplorationInputContext(GetBestInteractionTrigger, OpenConfigMenu, OnMove);
            m_InputRouter.Push(m_ExplorationInputContext);

            if (m_FieldContext.IsInputLockedByScript)
            {
                OnRequestInputLock(true);
            }

            await m_ScreenFadeService.FadeInAsync();

            m_InputAdapter.Enable();
        }

        private async Task UnloadCurrentFieldAsync()
        {
            m_InputAdapter.Disable();

            if (m_FieldContext.IsInputLockedByScript)
            {
                OnRequestInputLock(false);
            }

            m_InputRouter.Pop(m_ExplorationInputContext);
            m_ExplorationInputContext = null;

            UpdateManager.UnregisterUpdatable(this);
            UpdateManager.UnregisterFixedUpdatable(this);

            UnsubscribeVm();

#if UNITY_EDITOR
            m_DebugOverlay?.RemoveFromHierarchy();
            m_DebugOverlay = null;
#endif

            foreach (KeyValuePair<int, FieldEntityComponents> entity in m_Entities)
            {
                if (entity.Value.InteractionTrigger != null)
                {
                    entity.Value.InteractionTrigger.OnInteracted -= OnInteractionTriggered;
                }

                if (entity.Value.CollisionTrigger != null)
                {
                    entity.Value.CollisionTrigger.OnEntered        -= OnCollisionTriggerEntered;
                    entity.Value.CollisionTrigger.OnGatewayEntered -= OnGatewayEntered;
                    entity.Value.CollisionTrigger.OnLeft           -= OnCollisionTriggerLeft;
                }
            }

            m_FieldContext = null;

            await m_ScreenFadeService.FadeOutAsync();

            await m_FieldPresentation.Unload();

            m_LocalisationService.UnloadLocalisationData(m_FieldDatabaseAsset.LocalisationSheets);
        }

        private bool IsDialogueWindowOpen(byte channel)
        {
            bool open = m_DialogueChannels[channel].Window != null;

            return open;
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

        private void OnRequestMusic(ulong nameHash, ulong stateNameHash)
        {
            m_MusicPlayer.PlayAsync(nameHash, stateNameHash).FireAndForget();
        }

        private void OnRequestMusicStemState(ulong stateNameHash, float fadeSeconds)
        {
            m_MusicPlayer.SetStemStateFadeAsync(stateNameHash, fadeSeconds).FireAndForget();
        }

        private void OnRequestSfx(ulong nameHash)
        {
            m_SfxPlayer.Play(nameHash);
        }

        private void OnRequestSetPlayerEntity(FieldEntityRuntime entity)
        {
            m_FieldContext.SetPlayerEntity(entity);

            m_PlayerMovementDriver?.SetMoveInput(Vector3.zero);

            m_PlayerEntityId       = entity.EntityId;
            m_PlayerMovementDriver = GetMovementDriver(m_PlayerEntityId);

            SetCollisionTriggerPlayerEntityId(m_PlayerEntityId);
        }

        private void SetCollisionTriggerPlayerEntityId(int entityId)
        {
            foreach (KeyValuePair<int, FieldEntityComponents> entity in m_Entities)
            {
                if (entity.Value.CollisionTrigger != null)
                {
                    entity.Value.CollisionTrigger.SetPlayerEntityId(entityId);
                }
            }
        }

        /// <summary>
        /// Showing or hiding an entity also switches whether it can be talked to and whether its enter and leave
        /// scripts run. Showing turns interaction back on even if a script had turned it off; the collision
        /// trigger keeps a script's switch apart, so showing does not undo <c>COLLISION_TRIGGER_ACTIVATION</c>.
        /// </summary>
        private void OnRequestSetEntityVisible(int entityId, bool visible)
        {
            m_FieldContext.SetEntityVisible(entityId, visible);

            if (m_Entities[entityId].InteractionTrigger != null)
            {
                m_FieldContext.SetInteractionActive(entityId, visible);
            }

            ShowOrHideEntity(entityId, visible);
        }

        private void ShowOrHideEntity(int entityId, bool visible)
        {
            FieldEntityComponents entity = m_Entities[entityId];

            entity.Entity.SetVisible(visible);

            if (entity.InteractionTrigger != null)
            {
                entity.InteractionTrigger.SetActive(visible);
            }

            if (entity.CollisionTrigger != null)
            {
                entity.CollisionTrigger.SetEntityShown(visible);
            }
        }

        private void OnCollisionTriggerEntered(int entityId, int eventId)
        {
            m_FieldContext.VM.RequestScript(entityId, eventId, (byte)FieldScriptPriority.Enter);
        }

        private void OnCollisionTriggerLeft(int entityId, int eventId)
        {
            m_FieldContext.VM.RequestScript(entityId, eventId, (byte)FieldScriptPriority.Leave);
        }

        private void OnInteractionTriggered(int entityId, int eventId)
        {
            m_FieldContext.VM.RequestScript(entityId, eventId, (byte)FieldScriptPriority.Interaction);
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
            if (m_FieldContext.PlayerEntity == null)
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

            foreach (FieldEntityComponents entity in m_Entities.Values)
            {
                FieldInteractionTrigger trigger = entity.InteractionTrigger;

                if (trigger == null || !trigger.IsActive || !IsInInteractionRange(playerPos, entity))
                {
                    continue;
                }

                int entityId = trigger.Entity.EntityId;

                if (!IsPlayerFacingEntity(entityId))
                {
                    continue;
                }

                if (!IsEntityFacingPlayer(entityId))
                {
                    continue;
                }

                Vector3 toEntity = entity.Entity.transform.position - playerPos;
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
                    best      = trigger;
                }
            }

            return best;
        }

        private bool IsInInteractionRange(Vector3 playerPos, FieldEntityComponents entity)
        {
            Vector3 toEntity = Vector3.ProjectOnPlane(entity.Entity.transform.position - playerPos, m_FieldModuleMonoBehaviour.Up);
            float   range    = entity.InteractionTrigger.InteractionRange;

            bool inRange = toEntity.sqrMagnitude <= range * range;

            return inRange;
        }

        // TODO: when we have the main menu/party menu, it should load that instead
        private void OpenConfigMenu()
        {
            if (!m_FieldContext.MainMenuAccessible)
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
            m_PlayerMovementDriver?.SetMoveInput(worldMove);
        }

        private void OnRequestSetGatewayTriggersActive(bool active)
        {
            m_FieldContext.SetGatewaysActive(active);
        }

        private void OnGatewayEntered(int entityId, int eventId)
        {
            if (!m_FieldContext.GatewaysActive)
            {
                return;
            }

            m_FieldContext.VM.RequestScript(entityId, eventId, (byte)FieldScriptPriority.Enter);
        }

        private void OnRequestSetCollisionTriggerActive(int entityId, bool active)
        {
            m_Entities[entityId].CollisionTrigger.SetActive(active);
            m_FieldContext.SetCollisionTriggerActive(entityId, active);
        }

        private void OnRequestSetInteractionTriggerActive(int entityId, bool active)
        {
            m_Entities[entityId].InteractionTrigger.SetActive(active);
            m_FieldContext.SetInteractionActive(entityId, active);
        }

        private void OnRequestSetInteractionRange(int entityId, float range)
        {
            m_Entities[entityId].InteractionTrigger.SetInteractionRange(range);
            m_FieldContext.SetInteractionRange(entityId, range);
        }

        /// <summary>
        /// A script's input lock is on or off: locking twice needs one unlock, and unlocking when unlocked
        /// does nothing. Dialogue holds input through its own context instead.
        /// </summary>
        private void OnRequestScriptInputLock(bool lockInput)
        {
            if (lockInput == m_FieldContext.IsInputLockedByScript)
            {
                return;
            }

            m_FieldContext.SetInputLockedByScript(lockInput);

            OnRequestInputLock(lockInput);
        }

        private void OnRequestInputLock(bool lockInput)
        {
            if (lockInput)
            {
                MovePlayer(Vector3.zero);
                m_ScriptInputLock = new BlockAllInputContext();
                m_InputRouter.Push(m_ScriptInputLock);
            }
            else
            {
                m_InputRouter.Pop(m_ScriptInputLock);
                m_ScriptInputLock = null;
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
            m_FieldContext.SetMovementSpeed(entityId, movementSpeed);
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
            m_FieldContext.SetMainMenuAccessible(enabled);
        }

        private void OnRequestCreateDialogueWindow(DialogueWindowArgs args)
        {
            FieldDialogueChannel channel = m_DialogueChannels[args.Channel];

            channel.HasRect = true;
            channel.Rect    = args.Rect;
        }

        private void OnRequestSetDialogueWindowStyle(byte channel, DialogueWindowStyle style)
        {
            m_DialogueChannels[channel].Style = style;
        }

        private void OnRequestSetMessageVariable(byte slot, int value)
        {
            m_MessageVariables[slot] = value;
        }

        private void OnRequestShowDialogueWindow(byte channel, ulong dialogueId, bool blockMovement)
        {
            if (!TryOpenDialogueWindow(channel, dialogueId, out IDialogueWindow window))
            {
                return;
            }

            string[] dialogues = new[] { m_LocalisationService.Get(dialogueId) };

            RunDialogueAsync(channel, window, new TextDialogueFlow(), dialogues, blockMovement, null).FireAndForget();
        }

        /// <summary>
        /// A window the player does not answer, up until a script closes it. It never touches input, so the player
        /// walks, talks to people and opens the menu while it plays out.
        /// </summary>
        private void OnRequestShowDialogueWindowNoWait(byte channel, ulong dialogueId)
        {
            if (!TryOpenDialogueWindow(channel, dialogueId, out IDialogueWindow window))
            {
                return;
            }

            string[] dialogues = new[] { m_LocalisationService.Get(dialogueId) };

            RunUnansweredDialogueAsync(channel, window, new UnansweredDialogueFlow(), dialogues).FireAndForget();
        }

        private void OnRequestCloseDialogueWindow(byte channel)
        {
            m_DialogueChannels[channel].Close?.Cancel();
        }

        private void OnRequestAskPlayerToMakeAChoice(byte channel, ulong dialogueId, ulong[] answerIds, Action<byte> storeChoice)
        {
            if (!TryOpenDialogueWindow(channel, dialogueId, out IDialogueWindow window))
            {
                return;
            }

            string[] dialogues = new string[answerIds.Length + 1];
            dialogues[0] = m_LocalisationService.Get(dialogueId);

            for (int i = 1; i < dialogues.Length; i++)
            {
                dialogues[i] = m_LocalisationService.Get(answerIds[i - 1]);
            }

            RunDialogueAsync(channel, window, new ChoiceDialogueFlow(), dialogues, true, storeChoice).FireAndForget();
        }

        /// <summary>
        /// Put a window on a channel, laid out and styled as the channel says. The channel counts as busy from
        /// here, before the window has finished opening, so the script that asked waits on it straight away.
        /// </summary>
        private bool TryOpenDialogueWindow(byte channel, ulong dialogueId, out IDialogueWindow window)
        {
            FieldDialogueChannel dialogueChannel = m_DialogueChannels[channel];

            if (!dialogueChannel.HasRect)
            {
#if UNITY_EDITOR
                // Export checks that something in the field sets this channel's rectangle, but not that it runs
                // first, which depends on the order scripts happen to run in.
                Debug.LogError($"{nameof(FieldModule)}::{nameof(TryOpenDialogueWindow)} Dialogue [{dialogueId}] was shown on channel [{channel}] before CREATE_DIALOGUE_WINDOW set that channel's rectangle, so it was not shown");
#endif
                window = null;
                return false;
            }

            window = m_DIResolver.Resolve<IDialogueWindow>();
            window.Init(m_RootElement);
            window.SetRect(dialogueChannel.Rect);
            window.SetStyle(dialogueChannel.Style);
            window.SetMessageVariables(m_MessageVariables);

            dialogueChannel.Window = window;
            dialogueChannel.Close  = new CancellationTokenSource();

            return true;
        }

        /// <summary>
        /// Run a window to the end. Every open window shares <see cref="m_DialogueInputContext" />, so one press is
        /// seen by all of them — each finishes typing or closes together, and windows may close in any order.
        /// </summary>
        /// <param name="blockOtherInput">
        /// Hold back movement and every button but confirm while this window is open: someone the player is talking
        /// to. Without it the player walks on and can open the menu, as for a conversation in the background.
        /// </param>
        private async Task RunDialogueAsync(byte channel, IDialogueWindow window, IDialogueFlow dialogueFlow, string[] dialogues, bool blockOtherInput, Action<byte> storeChoice)
        {
            if (m_OpenDialogueWindows++ == 0)
            {
                m_InputRouter.Push(m_DialogueInputContext);
            }

            if (blockOtherInput)
            {
                // Movement stops reaching the player from here, so it stops where it is rather than walking on under
                // the last input it was given.
                MovePlayer(Vector3.zero);
                m_DialogueInputContext.BlockOtherInput();
            }

            CancellationToken close = m_DialogueChannels[channel].Close.Token;

            await window.AnimateWindowOpenAsync();
            await window.RunAsync(dialogueFlow, dialogues, m_DialogueInputContext, close);

            // A question closed by a script was never answered, so its destination keeps what it held.
            if (storeChoice != null && !close.IsCancellationRequested)
            {
                storeChoice(window.GetSelectedChoice());
            }

            await CloseDialogueWindowAsync(channel, window);

            if (blockOtherInput)
            {
                m_DialogueInputContext.UnblockOtherInput();
            }

            if (--m_OpenDialogueWindows == 0)
            {
                m_InputRouter.Pop(m_DialogueInputContext);
            }
        }

        private async Task RunUnansweredDialogueAsync(byte channel, IDialogueWindow window, IDialogueFlow dialogueFlow, string[] dialogues)
        {
            CancellationToken close = m_DialogueChannels[channel].Close.Token;

            await window.AnimateWindowOpenAsync();
            await window.RunAsync(dialogueFlow, dialogues, null, close);
            await CloseDialogueWindowAsync(channel, window);
        }

        /// <summary>
        /// The channel stays busy until the window has finished closing, so what is shown on it next waits for that.
        /// </summary>
        private async Task CloseDialogueWindowAsync(byte channel, IDialogueWindow window)
        {
            await window.AnimateWindowClosedAsync();

            window.Destroy();

            FieldDialogueChannel dialogueChannel = m_DialogueChannels[channel];

            dialogueChannel.Close.Dispose();
            dialogueChannel.Close  = null;
            dialogueChannel.Window = null;
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