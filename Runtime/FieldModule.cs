using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RPGFramework.Audio;
using RPGFramework.Audio.Music;
using RPGFramework.Battle.SharedTypes;
using RPGFramework.Battle.SharedTypes.Constants;
using RPGFramework.Battle.SharedTypes.Stores;
using RPGFramework.Core;
using RPGFramework.Core.Audio;
using RPGFramework.Core.Data;
using RPGFramework.Core.Dialogue;
using RPGFramework.Core.Dialogue.Flows;
using RPGFramework.Core.Input;
using RPGFramework.Core.Memory;
using RPGFramework.Core.PlayerLoop;
using RPGFramework.Core.SaveData;
using RPGFramework.Core.Settings;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Core.Store;
using RPGFramework.Core.Transitions;
using RPGFramework.DI;
using RPGFramework.Field.FieldVmArgs;
using RPGFramework.Field.SharedTypes;
using RPGFramework.Field.SharedTypes.Constants;
using RPGFramework.Field.SharedTypes.Stores;
using RPGFramework.Hashing;
using RPGFramework.Localisation;
using RPGFramework.Menu.SharedTypes;
using RPGFramework.Menu.SharedTypes.Constants;
using RPGFramework.Menu.SharedTypes.Stores;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

namespace RPGFramework.Field
{
    internal sealed partial class FieldModule : IFieldModule, IUpdatable, IFixedUpdatable
    {
        private const float MUSIC_RESUME_SECONDS = 1f;

        private readonly ICoreModule               m_CoreModule;
        private readonly IDIResolver               m_DIResolver;
        private readonly IInputRouter              m_InputRouter;
        private readonly IMusicPlayer              m_MusicPlayer;
        private readonly ISfxPlayer                m_SfxPlayer;
        private readonly IAudioIntentPlayer        m_AudioIntentPlayer;
        private readonly IFieldDatabase            m_FieldDatabase;
        private readonly IFieldPresentation        m_FieldPresentation;
        private readonly ILocalisationService      m_LocalisationService;
        private readonly FieldDialogueChannel[]    m_DialogueChannels;
        private readonly int[]                     m_MessageVariables;
        private readonly IMemoryService            m_MemoryService;
        private readonly ITempMemoryArgs           m_TempMemoryArgs;
        private readonly ITransitionService        m_Transitions;
        private readonly IBattleArgsStore          m_BattleArgsStore;
        private readonly IFieldArgsStore           m_FieldArgsStore;
        private readonly IMenuArgsStore            m_MenuArgsStore;
        private readonly ICurrentModuleStore       m_CurrentModuleStore;
        private readonly IFieldResumeDataStore     m_FieldResumeDataStore;
        private readonly SavedPlayerPose           m_SavedPlayerPose;
        private readonly FieldScreen               m_Screen;
        private readonly IMusicVolumeStore         m_MusicVolumeStore;
        private readonly ISettingsService          m_SettingsService;
        private readonly ISaveEnabledStore         m_SaveEnabledStore;
        private readonly ILocationNameStore        m_LocationNameStore;
        private readonly IBattleMusicStore         m_BattleMusicStore;
        private readonly IPausedMusicStore         m_PausedMusicStore;
        private readonly IBattleModeStore          m_BattleModeStore;
        private readonly IBattleCompleteStateStore m_BattleCompleteStateStore;
        private readonly DialogueInputContext      m_DialogueInputContext;
        private readonly IModuleScene              m_ModuleScene;
        private readonly BlockAllInputContext      m_SuspendedInputBlock;

        private Task                                   m_MusicVolumeFade;
        private FieldModuleMonoBehaviour               m_FieldModuleMonoBehaviour;
        private IInputContext                          m_ExplorationInputContext;
        private BlockAllInputContext                   m_ScriptInputLock;
        private int                                    m_OpenDialogueWindows;
        private TransformHandle                        m_CameraTransformHandle;
        private CinemachineBrain                       m_CinemachineBrain;
        private FollowPlayer[]                         m_PlayerCameras;
        private VisualElement                          m_RootElement;
        private InputAdapter                           m_InputAdapter;
        private FieldContext                           m_FieldContext;
        private SpawnPoint                             m_InitialPlayerSpawn;
        private Dictionary<int, FieldEntityComponents> m_Entities;
        private Dictionary<ulong, string>              m_AnimationNames;
        private FieldBlocker[]                         m_Blockers;
        private Dictionary<byte, FieldBlocker>         m_BlockersById;
        private FieldDimension                         m_Dimension;
        private Tilemap                                m_WalkableTilemap;
        private int                                    m_PlayerEntityId;
        private bool                                   m_FieldTransitionRequested;
        private FieldDefinition                        m_FieldDefinition;
        private bool                                   m_BattleTransitionRequested;
        private bool                                   m_MenuTransitionRequested;
        private bool                                   m_GameOverRequested;
        private IMovementDriver                        m_PlayerMovementDriver;
        private bool                                   m_Suspended;
        private float                                  m_TimeScaleBeforeSuspend;
        private bool                                   m_ExitFades;
        private bool                                   m_ClosingAllDialogueWindows;

        internal FieldModule(ICoreModule               coreModule,
                             IDIResolver               diResolver,
                             IInputRouter              inputRouter,
                             IMusicPlayer              musicPlayer,
                             ISfxPlayer                sfxPlayer,
                             IAudioIntentPlayer        audioIntentPlayer,
                             IFieldDatabase            fieldDatabase,
                             IFieldPresentation        fieldPresentation,
                             ILocalisationService      localisationService,
                             IMemoryService            memoryService,
                             ITempMemoryArgs           tempMemoryArgs,
                             ITransitionService        transitions,
                             ITransitionConfig         transitionConfig,
                             ITransitionDatabase       transitionDatabase,
                             IBattleArgsStore          battleArgsStore,
                             IFieldArgsStore           fieldArgsStore,
                             IMenuArgsStore            menuArgsStore,
                             ICurrentModuleStore       currentModuleStore,
                             IFieldResumeDataStore     fieldResumeDataStore,
                             IVariableMap              variableMap,
                             ISettingsService          settingsService,
                             ISaveEnabledStore         saveEnabledStore,
                             ILocationNameStore        locationNameStore,
                             IBattleMusicStore         battleMusicStore,
                             IMusicVolumeStore         musicVolumeStore,
                             IPausedMusicStore         pausedMusicStore,
                             IBattleModeStore          battleModeStore,
                             IBattleCompleteStateStore battleCompleteStateStore,
                             IModuleScene              moduleScene)
        {
            m_CoreModule               = coreModule;
            m_DIResolver               = diResolver;
            m_InputRouter              = inputRouter;
            m_MusicPlayer              = musicPlayer;
            m_SfxPlayer                = sfxPlayer;
            m_AudioIntentPlayer        = audioIntentPlayer;
            m_FieldDatabase            = fieldDatabase;
            m_FieldPresentation        = fieldPresentation;
            m_LocalisationService      = localisationService;
            m_MemoryService            = memoryService;
            m_TempMemoryArgs           = tempMemoryArgs;
            m_Transitions              = transitions;
            m_BattleArgsStore          = battleArgsStore;
            m_FieldArgsStore           = fieldArgsStore;
            m_MenuArgsStore            = menuArgsStore;
            m_CurrentModuleStore       = currentModuleStore;
            m_FieldResumeDataStore     = fieldResumeDataStore;
            m_SavedPlayerPose          = new SavedPlayerPose(memoryService, variableMap);
            m_Screen                   = new FieldScreen(transitionConfig, transitionDatabase, sfxPlayer, memoryService, variableMap);
            m_MusicVolumeStore         = musicVolumeStore;
            m_SettingsService          = settingsService;
            m_SaveEnabledStore         = saveEnabledStore;
            m_LocationNameStore        = locationNameStore;
            m_BattleMusicStore         = battleMusicStore;
            m_PausedMusicStore         = pausedMusicStore;
            m_BattleModeStore          = battleModeStore;
            m_BattleCompleteStateStore = battleCompleteStateStore;
            m_DialogueChannels         = new FieldDialogueChannel[ArgumentTypes.DIALOGUE_CHANNEL_COUNT];
            m_MessageVariables         = new int[DialogueMarkup.MESSAGE_VARIABLE_COUNT];
            m_MusicVolumeFade          = Task.CompletedTask;
            m_DialogueInputContext     = new DialogueInputContext();
            m_ModuleScene              = moduleScene;
            m_SuspendedInputBlock      = new BlockAllInputContext();
            m_ExitFades                = true;

            for (int i = 0; i < m_DialogueChannels.Length; i++)
            {
                m_DialogueChannels[i] = new FieldDialogueChannel();
            }
        }

        async Task IModule.OnEnterAsync(byte entry)
        {
            m_FieldModuleMonoBehaviour = m_ModuleScene.Find<FieldModuleMonoBehaviour>();

            m_InputAdapter = m_FieldModuleMonoBehaviour.InputAdapter;
            m_DIResolver.InjectInto(m_InputAdapter);

            UIDocument uiDoc = m_FieldModuleMonoBehaviour.UIDocument;
            m_RootElement = uiDoc.rootVisualElement.Q("Root");

            m_Screen.Enter(m_RootElement);

            m_CameraTransformHandle = m_FieldModuleMonoBehaviour.CameraTransformHandle;

            m_CinemachineBrain              = m_FieldModuleMonoBehaviour.CinemachineBrain;
            m_CinemachineBrain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;

            FieldContext fieldContext = m_FieldResumeDataStore.Get<FieldContext>();
            bool         musicPaused  = m_PausedMusicStore.TryTake(out MusicSnapshot pausedMusic);

            if (entry != ModuleEntries.RETURN || fieldContext == null)
            {
                await LoadNewFieldAsync();
                return;
            }

            m_FieldContext = fieldContext;

            if (musicPaused)
            {
                m_MusicPlayer.ResumeAsync(pausedMusic, MUSIC_RESUME_SECONDS, m_MusicVolumeStore.MusicVolume).FireAndForget();
            }

            await ResumeFieldAsync();
        }

        async Task IModule.OnExitAsync()
        {
            if (m_Suspended)
            {
                Unfreeze();
            }

            await UnloadCurrentFieldAsync();

            m_Screen.Exit();
            m_InputRouter.Clear();
        }

        // The field stays loaded under the module opened over it, frozen by the time scale, so it carries on exactly as
        // it was. Its world is left on: an Animator reset by being disabled, or a game's component told OnDisable, would
        // lose what this keeps.
        Task IModule.OnSuspendAsync()
        {
            m_InputAdapter.Disable();
            m_InputRouter.Push(m_SuspendedInputBlock);

            UpdateManager.UnregisterUpdatable(this);
            UpdateManager.UnregisterFixedUpdatable(this);

            Freeze();

            // One set of sheets resident at a time: what opens over the field loads its own, which may share some.
            m_LocalisationService.UnloadLocalisationData(m_FieldDefinition.LocalisationSheets);

            m_FieldModuleMonoBehaviour.UIDocument.rootVisualElement.style.display = DisplayStyle.None;

            m_FieldModuleMonoBehaviour.Camera.enabled = m_FieldModuleMonoBehaviour.VisibleWhileSuspended;

            return Task.CompletedTask;
        }

        async Task IModule.OnResumeAsync()
        {
            await m_LocalisationService.LoadNewLocalisationDataAsync(m_FieldDefinition.LocalisationSheets);

            m_FieldModuleMonoBehaviour.Camera.enabled                             = true;
            m_FieldModuleMonoBehaviour.UIDocument.rootVisualElement.style.display = DisplayStyle.Flex;

            Unfreeze();
            SetAnimationsPaused(false);

            m_InputRouter.Pop(m_SuspendedInputBlock);

            UpdateManager.RegisterUpdatable(this);
            UpdateManager.RegisterFixedUpdatable(this);
        }

        void IModule.OnRevealed()
        {
            m_InputAdapter.Enable();
        }

        private void Freeze()
        {
            m_Suspended              = true;
            m_TimeScaleBeforeSuspend = Time.timeScale;
            Time.timeScale           = 0f;

            Application.quitting += Unfreeze;
        }

        // Also as the game quits, play mode included, so a time scale of zero never outlives the session.
        private void Unfreeze()
        {
            Application.quitting -= Unfreeze;

            Time.timeScale = m_TimeScaleBeforeSuspend;
            m_Suspended    = false;
        }

        private void RequestMenuModule(byte menuId)
        {
            MenuArgs args = new MenuArgs(menuId);
            m_MenuArgsStore.Set(args);

            m_MenuTransitionRequested = true;
        }

        void IUpdatable.Update()
        {
            IReadOnlyList<FieldEntityRuntime> runtimes = m_FieldContext.Entities;

            for (int i = 0; i < runtimes.Count; i++)
            {
                runtimes[i].Update(m_FieldContext.VM);
            }

            ReadPlayerMove();

            float   deltaTime = Time.deltaTime;
            Vector3 north     = m_FieldModuleMonoBehaviour.North;
            Vector3 east      = Vector3.ProjectOnPlane(Vector3.right, m_FieldModuleMonoBehaviour.Up).normalized;

            foreach (FieldEntityComponents entity in m_Entities.Values)
            {
                UpdateMove(entity);

                entity.MovementDriver?.Tick(deltaTime);

                Vector3 velocity = IsMovingWithoutWalking(entity) ? Vector3.zero : entity.MovementDriver?.CurrentVelocity ?? Vector3.zero;

                if (entity.AnimationDriver != null)
                {
                    Vector3 facing = FacingOf(entity);

                    entity.AnimationDriver.Tick(velocity, new Vector2(Vector3.Dot(facing, east), Vector3.Dot(facing, north)));
                }
            }

            m_CinemachineBrain.ManualUpdate();

#if UNITY_EDITOR
            DrawInteractionDebug();
#endif

#if ENABLE_RPG_FRAMEWORK_DEBUG
            if (DebugRoomShortcut.IsHeld(m_InputAdapter))
            {
                LeaveForDebugRoomAsync().FireAndForget();
                return;
            }
#endif

            if (m_GameOverRequested)
            {
                TriggerGameOverAsync().FireAndForget();
                return;
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

        void IFixedUpdatable.FixedUpdate()
        {
            float fixedDeltaTime = Time.fixedDeltaTime;

            foreach (FieldEntityComponents entity in m_Entities.Values)
            {
                entity.MovementDriver?.PhysicsTick(fixedDeltaTime);
            }

            RaisePushes();
        }

        private void SubscribeVm()
        {
            m_FieldContext.VM.RequestFieldTransition             += OnSetFieldModuleArgs;
            m_FieldContext.VM.RequestMusic                       += OnRequestMusic;
            m_FieldContext.VM.RequestMusicCrossfade              += OnRequestMusicCrossfade;
            m_FieldContext.VM.RequestStopMusic                   += OnRequestStopMusic;
            m_FieldContext.VM.RequestMusicVolume                 += OnRequestMusicVolume;
            m_FieldContext.VM.RequestMusicVolumeFrom             += OnRequestMusicVolumeFrom;
            m_FieldContext.VM.RequestMusicStemState              += OnRequestMusicStemState;
            m_FieldContext.VM.RequestBattleMusic                 += OnRequestBattleMusic;
            m_FieldContext.VM.RequestReverbPreset                += OnRequestReverbPreset;
            m_FieldContext.VM.RequestReverbVolume                += OnRequestReverbVolume;
            m_FieldContext.VM.RequestSfx                         += OnRequestSfx;
            m_FieldContext.VM.RequestSetPlayerEntity             += OnRequestSetPlayerEntity;
            m_FieldContext.VM.RequestSetEntityVisible            += OnRequestSetEntityVisible;
            m_FieldContext.VM.RequestSetEntitySolid              += OnRequestSetEntitySolid;
            m_FieldContext.VM.RequestSetPushScriptActive         += OnRequestSetPushScriptActive;
            m_FieldContext.VM.RequestSetGatewayTriggersActive    += OnRequestSetGatewayTriggersActive;
            m_FieldContext.VM.RequestSetInteractionTriggerActive += OnRequestSetInteractionTriggerActive;
            m_FieldContext.VM.RequestSetCollisionTriggerActive   += OnRequestSetCollisionTriggerActive;
            m_FieldContext.VM.RequestSetBlockerActive            += OnRequestSetBlockerActive;
            m_FieldContext.VM.RequestFieldExitFade               += OnRequestFieldExitFade;
            m_FieldContext.VM.RequestScreenFade                  += OnRequestScreenFade;
            m_FieldContext.VM.IsScreenFading                     =  IsScreenFading;
            m_FieldContext.VM.RequestScreenColour                += OnRequestScreenColour;
            m_FieldContext.VM.IsScreenColourFading               =  IsScreenColourFading;
            m_FieldContext.VM.RequestSetInteractionRange         += OnRequestSetInteractionRange;
            m_FieldContext.VM.RequestInputLock                   += OnRequestScriptInputLock;
            m_FieldContext.VM.RequestSetEntityPosition           += OnRequestSetEntityPosition;
            m_FieldContext.VM.RequestSetEntityRotation           += OnRequestSetEntityRotation;
            m_FieldContext.VM.RequestSetEntityRotationAsync      += OnRequestSetEntityRotationAsync;
            m_FieldContext.VM.IsEntityRotating                   =  IsEntityRotating;
            m_FieldContext.VM.RequestSetEntityToFaceEntity       += OnRequestSetEntityToFaceEntity;
            m_FieldContext.VM.RequestSetEntityMovementSpeed      += OnRequestSetEntityMovementSpeed;
            m_FieldContext.VM.RequestMoveEntity                  += OnRequestMoveEntity;
            m_FieldContext.VM.IsEntityMoving                     =  IsEntityMoving;
            m_FieldContext.VM.IsMusicVolumeFading                =  IsMusicVolumeFading;
            m_FieldContext.VM.IsMusicPlaying                     =  IsMusicPlaying;
            m_FieldContext.VM.RequestStopMovement                += OnRequestStopMovement;
            m_FieldContext.VM.RequestSetBaseAnimation            += OnRequestSetBaseAnimation;
            m_FieldContext.VM.RequestPlayAnimation               += OnRequestPlayAnimation;
            m_FieldContext.VM.IsEntityAnimating                  =  IsEntityAnimating;
            m_FieldContext.VM.RequestPushAnimationState          += OnRequestPushAnimationState;
            m_FieldContext.VM.RequestPopAnimationState           += OnRequestPopAnimationState;
            m_FieldContext.VM.RequestSetAnimationSpeed           += OnRequestSetAnimationSpeed;
            m_FieldContext.VM.RequestStopAnimation               += OnRequestStopAnimation;
            m_FieldContext.VM.RequestSetMainMenuAccessibility    += OnRequestSetMainMenuAccessibility;
            m_FieldContext.VM.RequestSetSaveEnabled              += OnRequestSetSaveEnabled;
            m_FieldContext.VM.RequestOpenSaveMenu                += OnRequestOpenSaveMenu;
            m_FieldContext.VM.RequestCreateDialogueWindow        += OnRequestCreateDialogueWindow;
            m_FieldContext.VM.RequestShowDialogueWindow          += OnRequestShowDialogueWindow;
            m_FieldContext.VM.RequestShowDialogueWindowNoWait    += OnRequestShowDialogueWindowNoWait;
            m_FieldContext.VM.RequestCloseDialogueWindow         += OnRequestCloseDialogueWindow;
            m_FieldContext.VM.RequestSetDialogueWindowStyle      += OnRequestSetDialogueWindowStyle;
            m_FieldContext.VM.RequestSetMessageVariable          += OnRequestSetMessageVariable;
            m_FieldContext.VM.IsDialogueWindowOpen               =  IsDialogueWindowOpen;
            m_FieldContext.VM.RequestAskPlayerToMakeAChoice      += OnRequestAskPlayerToMakeAChoice;
            m_FieldContext.VM.RequestSetBattleMode               += OnRequestSetBattleMode;
            m_FieldContext.VM.RequestStartBattle                 += OnRequestStartBattle;
            m_FieldContext.VM.GetLastBattleResult                =  GetLastBattleResult;
            m_FieldContext.VM.RequestGameOver                    += OnRequestGameOver;
            m_FieldContext.VM.StartWaitingForButton              =  StartWaitingForButton;
        }

        private void UnsubscribeVm()
        {
            m_FieldContext.VM.StartWaitingForButton              =  null;
            m_FieldContext.VM.RequestGameOver                    -= OnRequestGameOver;
            m_FieldContext.VM.GetLastBattleResult                =  null;
            m_FieldContext.VM.RequestStartBattle                 -= OnRequestStartBattle;
            m_FieldContext.VM.RequestSetBattleMode               -= OnRequestSetBattleMode;
            m_FieldContext.VM.RequestAskPlayerToMakeAChoice      -= OnRequestAskPlayerToMakeAChoice;
            m_FieldContext.VM.IsDialogueWindowOpen               =  null;
            m_FieldContext.VM.RequestShowDialogueWindow          -= OnRequestShowDialogueWindow;
            m_FieldContext.VM.RequestShowDialogueWindowNoWait    -= OnRequestShowDialogueWindowNoWait;
            m_FieldContext.VM.RequestCloseDialogueWindow         -= OnRequestCloseDialogueWindow;
            m_FieldContext.VM.RequestSetDialogueWindowStyle      -= OnRequestSetDialogueWindowStyle;
            m_FieldContext.VM.RequestSetMessageVariable          -= OnRequestSetMessageVariable;
            m_FieldContext.VM.RequestCreateDialogueWindow        -= OnRequestCreateDialogueWindow;
            m_FieldContext.VM.RequestOpenSaveMenu                -= OnRequestOpenSaveMenu;
            m_FieldContext.VM.RequestSetSaveEnabled              -= OnRequestSetSaveEnabled;
            m_FieldContext.VM.RequestSetMainMenuAccessibility    -= OnRequestSetMainMenuAccessibility;
            m_FieldContext.VM.RequestStopAnimation               -= OnRequestStopAnimation;
            m_FieldContext.VM.RequestSetAnimationSpeed           -= OnRequestSetAnimationSpeed;
            m_FieldContext.VM.RequestPopAnimationState           -= OnRequestPopAnimationState;
            m_FieldContext.VM.RequestPushAnimationState          -= OnRequestPushAnimationState;
            m_FieldContext.VM.IsEntityAnimating                  =  null;
            m_FieldContext.VM.RequestPlayAnimation               -= OnRequestPlayAnimation;
            m_FieldContext.VM.RequestSetBaseAnimation            -= OnRequestSetBaseAnimation;
            m_FieldContext.VM.RequestStopMovement                -= OnRequestStopMovement;
            m_FieldContext.VM.IsEntityMoving                     =  null;
            m_FieldContext.VM.IsMusicVolumeFading                =  null;
            m_FieldContext.VM.IsMusicPlaying                     =  null;
            m_FieldContext.VM.RequestMoveEntity                  -= OnRequestMoveEntity;
            m_FieldContext.VM.RequestSetEntityMovementSpeed      -= OnRequestSetEntityMovementSpeed;
            m_FieldContext.VM.RequestSetEntityToFaceEntity       -= OnRequestSetEntityToFaceEntity;
            m_FieldContext.VM.IsEntityRotating                   =  null;
            m_FieldContext.VM.RequestSetEntityRotationAsync      -= OnRequestSetEntityRotationAsync;
            m_FieldContext.VM.RequestSetEntityRotation           -= OnRequestSetEntityRotation;
            m_FieldContext.VM.RequestSetEntityPosition           -= OnRequestSetEntityPosition;
            m_FieldContext.VM.RequestInputLock                   -= OnRequestScriptInputLock;
            m_FieldContext.VM.RequestSetInteractionRange         -= OnRequestSetInteractionRange;
            m_FieldContext.VM.IsScreenColourFading               =  null;
            m_FieldContext.VM.RequestScreenColour                -= OnRequestScreenColour;
            m_FieldContext.VM.IsScreenFading                     =  null;
            m_FieldContext.VM.RequestScreenFade                  -= OnRequestScreenFade;
            m_FieldContext.VM.RequestFieldExitFade               -= OnRequestFieldExitFade;
            m_FieldContext.VM.RequestSetBlockerActive            -= OnRequestSetBlockerActive;
            m_FieldContext.VM.RequestSetCollisionTriggerActive   -= OnRequestSetCollisionTriggerActive;
            m_FieldContext.VM.RequestSetInteractionTriggerActive -= OnRequestSetInteractionTriggerActive;
            m_FieldContext.VM.RequestSetGatewayTriggersActive    -= OnRequestSetGatewayTriggersActive;
            m_FieldContext.VM.RequestSetPushScriptActive         -= OnRequestSetPushScriptActive;
            m_FieldContext.VM.RequestSetEntitySolid              -= OnRequestSetEntitySolid;
            m_FieldContext.VM.RequestSetEntityVisible            -= OnRequestSetEntityVisible;
            m_FieldContext.VM.RequestSetPlayerEntity             -= OnRequestSetPlayerEntity;
            m_FieldContext.VM.RequestSfx                         -= OnRequestSfx;
            m_FieldContext.VM.RequestMusic                       -= OnRequestMusic;
            m_FieldContext.VM.RequestMusicCrossfade              -= OnRequestMusicCrossfade;
            m_FieldContext.VM.RequestStopMusic                   -= OnRequestStopMusic;
            m_FieldContext.VM.RequestMusicVolume                 -= OnRequestMusicVolume;
            m_FieldContext.VM.RequestMusicVolumeFrom             -= OnRequestMusicVolumeFrom;
            m_FieldContext.VM.RequestMusicStemState              -= OnRequestMusicStemState;
            m_FieldContext.VM.RequestBattleMusic                 -= OnRequestBattleMusic;
            m_FieldContext.VM.RequestReverbPreset                -= OnRequestReverbPreset;
            m_FieldContext.VM.RequestReverbVolume                -= OnRequestReverbVolume;
            m_FieldContext.VM.RequestFieldTransition             -= OnSetFieldModuleArgs;
        }

        private void OnSetFieldModuleArgs(FieldArgs args)
        {
            m_FieldArgsStore.Set(args);
            m_FieldTransitionRequested  = true;
            m_BattleTransitionRequested = false;
        }

        private async Task TriggerFieldTransitionAsync()
        {
            m_FieldTransitionRequested = false;

            StopRunning();

            if (m_ExitFades)
            {
                await m_Transitions.CoverAsync(m_FieldDefinition.ExitTransition);
            }

            await UnloadCurrentFieldAsync();
            await LoadNewFieldAsync();
            await m_Transitions.RevealAsync(m_FieldDefinition.EntryTransition);

            m_InputAdapter.Enable();
        }

        private Task TriggerMenuTransitionAsync()
        {
            m_MenuTransitionRequested = false;

            StopRunning();

            // The menu is where a save is made, so this is the pose a save holds.
            if (m_PlayerEntityId != FieldEntity.NO_ENTITY)
            {
                FieldEntityComponents player = m_Entities[m_PlayerEntityId];

                m_SavedPlayerPose.Write(player.Entity.transform.position, FacingOf(player), m_FieldModuleMonoBehaviour.Up, m_FieldModuleMonoBehaviour.North);
            }

            return m_CoreModule.RequestModuleChangeAsync(FieldOutcomes.OPEN_MENU);
        }

        private Task TriggerBattleTransitionAsync()
        {
            m_BattleTransitionRequested = false;

            StopRunning();
            StoreToTempMemory();

            return m_CoreModule.RequestModuleChangeAsync(FieldOutcomes.START_BATTLE);
        }

        private Task TriggerGameOverAsync()
        {
            m_GameOverRequested = false;

            StopRunning();

            return m_CoreModule.RequestModuleChangeAsync(FieldOutcomes.GAME_OVER);
        }

#if ENABLE_RPG_FRAMEWORK_DEBUG
        // Whatever the field was doing, as a cheat's forced jump would leave it.
        private Task LeaveForDebugRoomAsync()
        {
            StopRunning();

            return m_CoreModule.RequestModuleChangeAsync(DebugFieldOutcomes.DEBUG_ROOM);
        }
#endif

        // The screen is covered over the field before a change reaches it, so the field stops where it is as it asks
        // for one: nothing moves on, no walk cycle plays in place, and no script runs past the one that asked.
        private void StopRunning()
        {
            m_InputAdapter.Disable();

            UpdateManager.UnregisterUpdatable(this);
            UpdateManager.UnregisterFixedUpdatable(this);

            SetAnimationsPaused(true);
        }

        private void SetAnimationsPaused(bool paused)
        {
            foreach (FieldEntityComponents entity in m_Entities.Values)
            {
                entity.AnimationDriver?.SetPaused(paused);
            }
        }

        private async Task<FieldEntities> PreLoadFieldAsync()
        {
            FieldArgs fieldArgs = m_FieldArgsStore.Args;
            m_FieldDefinition = m_FieldDatabase.Get(fieldArgs.FieldId);

            await m_LocalisationService.LoadNewLocalisationDataAsync(m_FieldDefinition.LocalisationSheets);

            GameObject   fieldGameObject = await m_FieldPresentation.LoadAsync(m_FieldDefinition);
            SpawnPoint[] spawnPoints     = fieldGameObject.GetComponentsInChildren<SpawnPoint>();
            m_InitialPlayerSpawn = Array.Find(spawnPoints, sp => sp.Id == fieldArgs.SpawnId);

            m_Blockers     = fieldGameObject.GetComponentsInChildren<FieldBlocker>(true);
            m_BlockersById = new Dictionary<byte, FieldBlocker>(m_Blockers.Length);

            for (int i = 0; i < m_Blockers.Length; i++)
            {
                FieldBlocker blocker = m_Blockers[i];

                blocker.Bind();
                m_BlockersById.Add(blocker.Id, blocker);
            }

            FieldEntities fieldEntities = fieldGameObject.GetComponent<FieldEntities>();
            m_Dimension       = fieldEntities.Dimension;
            m_WalkableTilemap = FindWalkableTilemap(fieldGameObject);
            m_PlayerCameras   = fieldGameObject.GetComponentsInChildren<FollowPlayer>(true);
            m_Entities        = new Dictionary<int, FieldEntityComponents>(fieldEntities.Compiled.Count);
            m_AnimationNames  = BuildAnimationNames(fieldEntities);

            for (int i = 0; i < fieldEntities.Compiled.Count; i++)
            {
                CompiledFieldEntity record = fieldEntities.Compiled[i];

                AddEntityComponents(record);
            }

            return fieldEntities;
        }

        /// <summary>
        /// Scripts name an animation by its hash; an Animator wants the name. Export keeps the names the hashes came
        /// from, so the field rebuilds the way back on load.
        /// </summary>
        private static Dictionary<ulong, string> BuildAnimationNames(FieldEntities fieldEntities)
        {
            Dictionary<ulong, string> animationNames = new Dictionary<ulong, string>(fieldEntities.AnimationNames.Count);

            for (int i = 0; i < fieldEntities.AnimationNames.Count; i++)
            {
                string animationName = fieldEntities.AnimationNames[i];

                animationNames[Fnv1a64.Hash(animationName)] = animationName;
            }

            return animationNames;
        }

        /// <summary>
        /// An entity's body, bound to its record, with its triggers wired. An entity with no body still gets an entry,
        /// so every id resolves; there is simply nothing in it.
        /// </summary>
        private void AddEntityComponents(CompiledFieldEntity record)
        {
            FieldEntityComponents entityComponents = new FieldEntityComponents();
            m_Entities.Add(record.EntityId, entityComponents);

            FieldEntity body = record.Body;

            if (body == null)
            {
                return;
            }

            body.Bind(record);
            entityComponents.SetEntity(body);

            FieldCollisionTrigger collisionTrigger = body.GetComponentInChildren<FieldCollisionTrigger>();

            if (collisionTrigger != null)
            {
                entityComponents.SetCollisionTrigger(collisionTrigger);
                collisionTrigger.OnEntered        += OnCollisionTriggerEntered;
                collisionTrigger.OnGatewayEntered += OnGatewayEntered;
                collisionTrigger.OnLeft           += OnCollisionTriggerLeft;
            }

            FieldInteractionTrigger interactionTrigger = body.GetComponentInChildren<FieldInteractionTrigger>();

            if (interactionTrigger != null)
            {
                entityComponents.SetInteractionTrigger(interactionTrigger);
                interactionTrigger.OnInteracted += OnInteractionTriggered;
            }
        }

        private async Task LoadNewFieldAsync()
        {
            FieldEntities fieldEntities = await PreLoadFieldAsync();

            m_CurrentModuleStore.SetModuleId(FieldConstants.MODULE_ID);

            m_SaveEnabledStore.ResetSaveEnabled();
            m_LocationNameStore.SetLocationName(m_FieldDefinition.LocationName);

            FieldVM              vm       = new FieldVM(m_MemoryService, m_TempMemoryArgs.TempBytes);
            FieldEntityRuntime[] entities = new FieldEntityRuntime[fieldEntities.Compiled.Count];

            for (int j = 0; j < fieldEntities.Compiled.Count; j++)
            {
                CompiledFieldEntity record = fieldEntities.Compiled[j];

                int[]  scriptIdsByEvent = new int[record.Scripts.Count];
                byte[] slotsByEvent     = new byte[record.Scripts.Count];

                for (int i = 0; i < record.Scripts.Count; i++)
                {
                    CompiledFieldScript script = record.Scripts[i];

                    scriptIdsByEvent[i] = script.ScriptId;
                    slotsByEvent[i]     = script.Slot;

                    vm.RegisterScript(script.ScriptId, fieldEntities.FormatVersion, script.Bytecode);
                }

                FieldEntityRuntime fieldEntityRuntime = new FieldEntityRuntime(record.EntityId, scriptIdsByEvent, slotsByEvent);

                entities[j] = fieldEntityRuntime;
                vm.RegisterEntity(record.EntityId, fieldEntityRuntime);
            }

            byte[] blockerIds = new byte[m_Blockers.Length];

            for (int i = 0; i < m_Blockers.Length; i++)
            {
                blockerIds[i] = m_Blockers[i].Id;
            }

            m_FieldContext = new FieldContext(vm, entities, blockerIds);

            SubscribeVm();

            InitialiseFieldScripts();
            StartDefaultScripts(fieldEntities);
            InitialisePlayer();

            PostFieldLoad();
        }

        /// <summary>
        /// Run every entity's init script to completion, before the field is shown or the player has
        /// control.
        /// </summary>
        private void InitialiseFieldScripts()
        {
            FieldVM vm = m_FieldContext.VM;

            for (int i = 0; i < m_FieldContext.Entities.Count; i++)
            {
                FieldEntityRuntime entity = m_FieldContext.Entities[i];

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
        private void StartDefaultScripts(FieldEntities fieldEntities)
        {
            for (int i = 0; i < fieldEntities.Compiled.Count; i++)
            {
                CompiledFieldEntity record = fieldEntities.Compiled[i];

                record.TryGetScriptIndex(FieldScriptType.Default, out int eventId);

                m_FieldContext.VM.StartDefaultScript(record.EntityId, eventId);
            }
        }

        private void InitialisePlayer()
        {
            bool loaded = m_SavedPlayerPose.TryTakeAfterLoad(m_FieldModuleMonoBehaviour.Up, out Vector3 savedPosition, out Quaternion savedRotation);

            FieldEntityRuntime playerEntity = m_FieldContext.PlayerEntity;

            if (playerEntity == null)
            {
                m_PlayerEntityId = FieldEntity.NO_ENTITY;
                SetCollisionTriggerPlayerEntityId(m_PlayerEntityId);
                return;
            }

            m_PlayerEntityId = playerEntity.EntityId;

            FieldEntity playerFieldEntity = m_Entities[m_PlayerEntityId].Entity;

            m_PlayerMovementDriver = GetMovementDriver(m_PlayerEntityId);

            if (loaded)
            {
                playerFieldEntity.transform.position = savedPosition;
                m_PlayerMovementDriver.SetRotation(savedRotation);
            }
            else if (m_InitialPlayerSpawn == null)
            {
                FieldArgs fieldArgs = m_FieldArgsStore.Args;
                Debug.LogError($"{nameof(FieldModule)}::{nameof(InitialisePlayer)} No spawn point with id [{fieldArgs.SpawnId}] in this field, so the player keeps whatever position its init script gave it");
            }
            else
            {
                playerFieldEntity.transform.position = m_InitialPlayerSpawn.Position;
                m_PlayerMovementDriver.SetRotation(m_InitialPlayerSpawn.Rotation);
            }

            SetCollisionTriggerPlayerEntityId(m_PlayerEntityId);
        }

        private async Task ResumeFieldAsync()
        {
            await PreLoadFieldAsync();

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

            IReadOnlyList<FieldEntityRuntime> entities = m_FieldContext.Entities;

            // Where everything stood first, as a move toward another entity aims at where that entity is.
            for (int i = 0; i < entities.Count; i++)
            {
                ApplyEntityPose(entities[i].EntityId);
            }

            for (int i = 0; i < entities.Count; i++)
            {
                int entityId = entities[i].EntityId;

                ApplyEntityState(entityId, m_FieldContext.EntityState(entityId).Set);
            }

            for (int i = 0; i < m_FieldContext.BlockerCount; i++)
            {
                FieldBlockerState blocker = m_FieldContext.BlockerStateAt(i);

                if (blocker.Set)
                {
                    m_BlockersById[blocker.BlockerId].SetBlocking(blocker.Active);
                }
            }

            PostFieldLoad();
        }

        private void PostFieldLoad()
        {
            PointCamerasAtPlayer();

            m_CinemachineBrain.ManualUpdate(Time.frameCount, -1f);

            UpdateManager.RegisterUpdatable(this);
            UpdateManager.RegisterFixedUpdatable(this);

            m_ExplorationInputContext = new FieldExplorationInputContext(GetBestInteractionTrigger, OpenPartyMenu);
            m_InputRouter.Push(m_ExplorationInputContext);

            if (m_FieldContext.IsInputLockedByScript)
            {
                OnRequestInputLock(true);
            }

            RaiseButtonWaits();
        }

        private async Task UnloadCurrentFieldAsync()
        {
            await CloseAllDialogueWindowsAsync();

            m_InputAdapter.Disable();

            if (m_FieldContext.IsInputLockedByScript)
            {
                OnRequestInputLock(false);
            }

            m_InputRouter.Pop(m_ExplorationInputContext);
            m_ExplorationInputContext = null;

            for (int i = 0; i < m_FieldContext.ButtonWaits.Count; i++)
            {
                m_InputRouter.Pop(m_FieldContext.ButtonWaits[i]);
            }

            UpdateManager.UnregisterUpdatable(this);
            UpdateManager.UnregisterFixedUpdatable(this);

            SetAnimationsPaused(true);

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

            await m_FieldPresentation.Unload();

            m_LocalisationService.UnloadLocalisationData(m_FieldDefinition.LocalisationSheets);
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
            m_MusicPlayer.PlayAsync(nameHash, stateNameHash, 0f, m_MusicVolumeStore.MusicVolume).FireAndForget();
        }

        // The crossfade's volume is for that track alone, and the field's music volume stays as it was.
        private void OnRequestMusicCrossfade(ulong nameHash, ulong stateNameHash, float seconds, float volume)
        {
            m_MusicPlayer.CrossfadeAsync(nameHash, stateNameHash, seconds, volume).FireAndForget();
        }

        private void OnRequestStopMusic()
        {
            m_MusicPlayer.StopAsync(0f).FireAndForget();
        }

        private void OnRequestMusicVolume(float volume, float seconds)
        {
            m_MusicVolumeStore.SetMusicVolume(Mathf.Clamp01(volume));

            m_MusicVolumeFade = m_MusicPlayer.SetSongVolumeAsync(volume, seconds);
            m_MusicVolumeFade.FireAndForget();
        }

        private void OnRequestMusicVolumeFrom(float from, float volume, float seconds)
        {
            m_MusicPlayer.SetSongVolumeAsync(from).FireAndForget();

            OnRequestMusicVolume(volume, seconds);
        }

        private bool IsMusicPlaying()
        {
            bool playing = m_MusicPlayer.IsPlaying();

            return playing;
        }

        private bool IsMusicVolumeFading()
        {
            bool fading = !m_MusicVolumeFade.IsCompleted;

            return fading;
        }

        private void OnRequestMusicStemState(ulong stateNameHash, float fadeSeconds)
        {
            m_MusicPlayer.SetStemStateFadeAsync(stateNameHash, fadeSeconds).FireAndForget();
        }

        private void OnRequestSfx(ulong nameHash)
        {
            m_SfxPlayer.PlayAsync(nameHash).FireAndForget();
        }

        private void OnRequestBattleMusic(ulong nameHash)
        {
            m_BattleMusicStore.SetBattleMusic(nameHash);
        }

        private void OnRequestReverbPreset(byte preset)
        {
            m_MusicPlayer.SetReverbPreset((ReverbPreset)preset);
        }

        private void OnRequestReverbVolume(float volume)
        {
            m_MusicPlayer.SetReverbVolume(volume);
        }

        private void OnRequestSetPlayerEntity(FieldEntityRuntime entity)
        {
            m_FieldContext.SetPlayerEntity(entity);

            m_PlayerMovementDriver?.SetMoveInput(Vector3.zero);

            m_PlayerEntityId       = entity.EntityId;
            m_PlayerMovementDriver = GetMovementDriver(m_PlayerEntityId);

            SetCollisionTriggerPlayerEntityId(m_PlayerEntityId);
            PointCamerasAtPlayer();
        }

        private void PointCamerasAtPlayer()
        {
            Transform player = m_PlayerEntityId != FieldEntity.NO_ENTITY ? m_Entities[m_PlayerEntityId].Entity.transform : null;

            for (int i = 0; i < m_PlayerCameras.Length; i++)
            {
                m_PlayerCameras[i].Follow(player);
            }
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
            ref FieldEntityState state = ref m_FieldContext.EntityState(entityId);

            state.SetVisible(visible);

            if (m_Entities[entityId].InteractionTrigger != null)
            {
                state.SetInteractionActive(visible);
            }

            ApplyEntityState(entityId, FieldEntityValues.Visible);
        }

        private void OnRequestSetEntitySolid(int entityId, bool solid)
        {
            m_FieldContext.EntityState(entityId).SetSolid(solid);

            ApplyEntityState(entityId, FieldEntityValues.Solid);
        }

        private void OnRequestSetPushScriptActive(int entityId, bool active)
        {
            m_FieldContext.EntityState(entityId).SetPushActive(active);

            ApplyEntityState(entityId, FieldEntityValues.PushActive);
        }

        // Where an entity stood and how it was turning as the field was left.
        private void ApplyEntityPose(int entityId)
        {
            FieldEntityState state = m_FieldContext.EntityState(entityId);

            if ((state.Set & FieldEntityValues.Pose) != 0)
            {
                OnRequestSetEntityPosition(entityId, state.Position);
                OnRequestSetEntityRotation(entityId, state.Rotation);
            }

            if ((state.Set & FieldEntityValues.RotationState) != 0)
            {
                GetMovementDriver(entityId).ResumeRotation(state.RotationState);
            }
        }

        /// <summary>
        /// Puts the named values of the entity's record on its components, those it holds. Visibility first, since
        /// showing and hiding switch interaction too, and the recorded interaction state after it.
        /// </summary>
        private void ApplyEntityState(int entityId, FieldEntityValues values)
        {
            FieldEntityState      state  = m_FieldContext.EntityState(entityId);
            FieldEntityComponents entity = m_Entities[entityId];
            FieldEntityValues     apply  = values & state.Set;

            if ((apply & FieldEntityValues.Visible) != 0)
            {
                ShowOrHideEntity(entityId, state.Visible);
            }

            if ((apply & FieldEntityValues.InteractionActive) != 0)
            {
                entity.InteractionTrigger.SetActive(state.InteractionActive);
            }

            if ((apply & FieldEntityValues.Solid) != 0)
            {
                entity.Entity.SetSolid(state.Solid);
            }

            if ((apply & FieldEntityValues.PushActive) != 0)
            {
                entity.Entity.SetPushActive(state.PushActive);
            }

            if ((apply & FieldEntityValues.InteractionRange) != 0)
            {
                entity.InteractionTrigger.SetInteractionRange(state.InteractionRange);
            }

            if ((apply & FieldEntityValues.MovementSpeed) != 0)
            {
                GetMovementDriver(entityId).SetMoveSpeed(state.MovementSpeed);
            }

            if ((apply & FieldEntityValues.Move) != 0)
            {
                OnRequestMoveEntity(entityId, state.Move);
            }

            if ((apply & FieldEntityValues.BaseAnimation) != 0)
            {
                GetAnimationDriver(entityId).SetBaseAnimation(m_AnimationNames[state.BaseAnimation]);
            }

            if ((apply & FieldEntityValues.AnimationSpeed) != 0)
            {
                GetAnimationDriver(entityId).SetSpeedMultiplier(state.AnimationSpeed);
            }

            if ((apply & FieldEntityValues.PlayedAnimation) != 0)
            {
                PlayAnimation(entityId, state.PlayedAnimation);
            }

            if ((apply & FieldEntityValues.CollisionTriggerActive) != 0)
            {
                entity.CollisionTrigger.SetActive(state.CollisionTriggerActive);
            }
        }

        private void RaisePushes()
        {
            if (m_PlayerMovementDriver == null || m_FieldContext.IsInputLockedByScript)
            {
                return;
            }

            IReadOnlyList<FieldEntity> pushedEntities = m_PlayerMovementDriver.Pushed;

            for (int i = 0; i < pushedEntities.Count; i++)
            {
                FieldEntity pushed = pushedEntities[i];

                if (pushed.RaisesPush && pushed.TryGetScriptIndex(FieldScriptType.OnPush, out int eventId))
                {
                    m_FieldContext.VM.RequestScript(pushed.EntityId, eventId);
                }
            }
        }

        private void ShowOrHideEntity(int entityId, bool visible)
        {
            FieldEntityComponents entity = m_Entities[entityId];

            entity.Entity.SetShown(visible);

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
            m_FieldContext.VM.RequestScript(entityId, eventId);
        }

        private void OnCollisionTriggerLeft(int entityId, int eventId)
        {
            m_FieldContext.VM.RequestScript(entityId, eventId);
        }

        private void OnInteractionTriggered(int entityId, int eventId)
        {
            m_FieldContext.VM.RequestScript(entityId, eventId);
        }

        private bool IsPlayerFacingEntity(int entityId)
        {
            FieldEntityComponents player = m_Entities[m_FieldContext.PlayerEntity.EntityId];
            FieldEntity           entity = m_Entities[entityId].Entity;

            Vector3 playerPos = player.Entity.transform.position;
            Vector3 entityPos = entity.transform.position;

            return IsFacing(playerPos, FacingOf(player), entityPos, m_FieldModuleMonoBehaviour.PlayerInteractionAngle);
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

            return IsFacing(entityPos, FacingOf(entity), playerPos, entity.InteractionTrigger.InteractionAngle);
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

            FieldEntityComponents player = m_Entities[m_FieldContext.PlayerEntity.EntityId];

            Vector3 playerPos     = player.Entity.transform.position;
            Vector3 playerForward = Vector3.ProjectOnPlane(FacingOf(player), m_FieldModuleMonoBehaviour.Up).normalized;

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

        private void OpenPartyMenu()
        {
            if (!m_FieldContext.MainMenuAccessible)
            {
                return;
            }

            byte type = (byte)MenuType.Party;

            RequestMenuModule(type);
        }

        private void StoreToTempMemory()
        {
            IReadOnlyList<FieldEntityRuntime> entities = m_FieldContext.Entities;

            for (int i = 0; i < entities.Count; i++)
            {
                ref FieldEntityState  state  = ref m_FieldContext.EntityStateAt(i);
                FieldEntityComponents entity = m_Entities[entities[i].EntityId];

                state.ClearMove();

                if (entity.Entity == null)
                {
                    continue;
                }

                Transform transform = entity.Entity.transform;

                state.SetPose(transform.position, RotationOf(entity));

                if (entity.MovementDriver != null)
                {
                    state.SetRotationState(entity.MovementDriver.GetRotationState());
                }

                if (entity.HasMove && entity.MovementDriver.IsMovingToTarget)
                {
                    state.SetMove(entity.Move);
                }
            }

            m_FieldResumeDataStore.Set(m_FieldContext);
        }

        // Read each tick, after the scripts, rather than as it changes, so whatever is held counts the moment movement
        // reaches the field again — back from another module, a conversation closed, a script giving control back — and a
        // script taking control this tick stops the player this tick.
        private void ReadPlayerMove()
        {
            Vector2 move = m_InputRouter.MovementReaches(m_ExplorationInputContext) ? m_InputAdapter.Movement : Vector2.zero;
            Vector3 up   = m_FieldModuleMonoBehaviour.Up;

            Vector3 forward = Vector3.ProjectOnPlane(m_CameraTransformHandle.forward, up);
            Vector3 right   = Vector3.ProjectOnPlane(m_CameraTransformHandle.right,   up).normalized;

            if (forward.sqrMagnitude < 1e-6f)
            {
                forward = Vector3.ProjectOnPlane(m_CameraTransformHandle.up, up);
            }

            forward = forward.normalized;

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

        private void OnRequestSetBlockerActive(byte blockerId, bool active)
        {
            m_FieldContext.BlockerState(blockerId).SetActive(active);

            m_BlockersById[blockerId].SetBlocking(active);
        }

        private void OnRequestFieldExitFade(bool fades)
        {
            m_ExitFades = fades;
        }

        // A fade naming no transition is the field's own: its exit transition fading out, its entry transition in.
        private void OnRequestScreenFade(bool fadeOut, bool atOnce, ulong transition)
        {
            ulong own = fadeOut ? m_FieldDefinition.ExitTransition : m_FieldDefinition.EntryTransition;

            m_Screen.Fade(fadeOut, transition != 0 ? transition : own, atOnce);
        }

        private bool IsScreenFading()
        {
            bool fading = m_Screen.IsFading;

            return fading;
        }

        private void OnRequestScreenColour(Color colour, bool add, float seconds)
        {
            m_Screen.FadeColour(colour, add, seconds);
        }

        private bool IsScreenColourFading()
        {
            bool fading = m_Screen.IsColourFading;

            return fading;
        }

        private void OnGatewayEntered(int entityId, int eventId)
        {
            if (!m_FieldContext.GatewaysActive)
            {
                return;
            }

            m_FieldContext.VM.RequestScript(entityId, eventId);
        }

        private void OnRequestSetCollisionTriggerActive(int entityId, bool active)
        {
            m_FieldContext.EntityState(entityId).SetCollisionTriggerActive(active);

            ApplyEntityState(entityId, FieldEntityValues.CollisionTriggerActive);
        }

        private void OnRequestSetInteractionTriggerActive(int entityId, bool active)
        {
            m_FieldContext.EntityState(entityId).SetInteractionActive(active);

            ApplyEntityState(entityId, FieldEntityValues.InteractionActive);
        }

        private void OnRequestSetInteractionRange(int entityId, float range)
        {
            m_FieldContext.EntityState(entityId).SetInteractionRange(range);

            ApplyEntityState(entityId, FieldEntityValues.InteractionRange);
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

            // An init script's lock is only recorded: the field locks itself as it sets up its input, once.
            if (m_ExplorationInputContext == null)
            {
                return;
            }

            OnRequestInputLock(lockInput);
        }

        private void OnRequestInputLock(bool lockInput)
        {
            if (lockInput)
            {
                m_ScriptInputLock = new BlockAllInputContext();
                m_InputRouter.Push(m_ScriptInputLock);

                RaiseButtonWaits();
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

            GetMovementDriver(entityId).Face(direction);
        }

        private void OnRequestSetEntityMovementSpeed(int entityId, float movementSpeed)
        {
            m_FieldContext.EntityState(entityId).SetMovementSpeed(movementSpeed);

            ApplyEntityState(entityId, FieldEntityValues.MovementSpeed);
        }

        private void OnRequestSetBaseAnimation(int entityId, ulong stateNameHash)
        {
            m_FieldContext.EntityState(entityId).SetBaseAnimation(stateNameHash);

            ApplyEntityState(entityId, FieldEntityValues.BaseAnimation);
        }

        private void OnRequestSetAnimationSpeed(int entityId, float multiplier)
        {
            m_FieldContext.EntityState(entityId).SetAnimationSpeed(multiplier);

            ApplyEntityState(entityId, FieldEntityValues.AnimationSpeed);
        }

        // Played whatever its mode; recorded only if it is still showing when the field is left.
        private void OnRequestPlayAnimation(int entityId, PlayAnimationArgs args)
        {
            m_FieldContext.EntityState(entityId).SetPlayedAnimation(args);

            PlayAnimation(entityId, args);
        }

        private void PlayAnimation(int entityId, PlayAnimationArgs args)
        {
            GetAnimationDriver(entityId).Play(m_AnimationNames[args.NameHash], args.Mode, args.From, args.To);
        }

        private bool IsEntityAnimating(int entityId)
        {
            FieldEntityComponents entity = m_Entities[entityId];

            if (entity.AnimationDriver == null)
            {
                return false;
            }

            bool isAnimating = entity.AnimationDriver.IsPlaying;

            return isAnimating;
        }

        private void OnRequestPushAnimationState(int entityId)
        {
            GetAnimationDriver(entityId).PushState();
        }

        private void OnRequestPopAnimationState(int entityId)
        {
            GetAnimationDriver(entityId).PopState();
        }

        private void OnRequestStopAnimation(int entityId)
        {
            m_FieldContext.EntityState(entityId).ClearPlayedAnimation();

            GetAnimationDriver(entityId).ReturnToBase();
        }

        private IAnimationDriver GetAnimationDriver(int entityId)
        {
            FieldEntityComponents entity          = m_Entities[entityId];
            IAnimationDriver      animationDriver = entity.AnimationDriver;

            if (animationDriver == null)
            {
                animationDriver = AnimationDriverFactory.Create(entity.Entity.gameObject);
                entity.SetAnimationDriver(animationDriver);
            }

            return animationDriver;
        }

        private void OnRequestMoveEntity(int entityId, MoveEntityArgs args)
        {
            FieldEntityComponents entity = m_Entities[entityId];

            entity.SetMove(args);
            GetMovementDriver(entityId).MoveTo(MoveTarget(args), args.StopDistance, args.Style != MoveStyle.Slide);
        }

        private bool IsEntityMoving(int entityId)
        {
            IMovementDriver movementDriver = m_Entities[entityId].MovementDriver;

            bool isMoving = movementDriver != null && movementDriver.IsMovingToTarget;

            return isMoving;
        }

        private void OnRequestStopMovement(int entityId)
        {
            FieldEntityComponents entity = m_Entities[entityId];

            entity.MovementDriver?.StopMove();
            entity.ClearMove();
        }

        private void UpdateMove(FieldEntityComponents entity)
        {
            if (!entity.HasMove)
            {
                return;
            }

            if (!entity.MovementDriver.IsMovingToTarget)
            {
                entity.ClearMove();

                return;
            }

            if (entity.Move.FollowsEntity)
            {
                entity.MovementDriver.MoveTo(MoveTarget(entity.Move), entity.Move.StopDistance, entity.Move.Style != MoveStyle.Slide);
            }
        }

        private Vector3 MoveTarget(MoveEntityArgs args)
        {
            Vector3 target = args.FollowsEntity ? m_Entities[args.TargetEntityId].Entity.transform.position : args.Target;

            return target;
        }

        private static bool IsMovingWithoutWalking(FieldEntityComponents entity)
        {
            bool isMovingWithoutWalking = entity.HasMove && entity.Move.Style != MoveStyle.Walk && entity.MovementDriver.IsMovingToTarget;

            return isMovingWithoutWalking;
        }

        private IMovementDriver GetMovementDriver(int entityId)
        {
            FieldEntityComponents entity         = m_Entities[entityId];
            IMovementDriver       movementDriver = entity.MovementDriver;

            if (entity.MovementDriver == null)
            {
                movementDriver = MovementDriverFactory.Create(entity.Entity.gameObject, 3f, m_Blockers, m_Dimension, m_WalkableTilemap);
                entity.SetMovementDriver(movementDriver);
            }

            return movementDriver;
        }

        private static Tilemap FindWalkableTilemap(GameObject field)
        {
            WalkableTilemap walkable = field.GetComponentInChildren<WalkableTilemap>(true);

            return walkable != null ? walkable.Tilemap : null;
        }

        private Vector3 FacingOf(FieldEntityComponents entity)
        {
            if (entity.MovementDriver != null)
            {
                return entity.MovementDriver.Facing;
            }

            return m_Dimension == FieldDimension.TwoD ? SpriteFacing.Default : Vector3.ProjectOnPlane(entity.Entity.transform.forward, m_FieldModuleMonoBehaviour.Up).normalized;
        }

        private Quaternion RotationOf(FieldEntityComponents entity)
        {
            if (entity.MovementDriver != null)
            {
                return entity.MovementDriver.Rotation;
            }

            return m_Dimension == FieldDimension.TwoD ? SpriteFacing.RotationFacing(SpriteFacing.Default) : entity.Entity.transform.rotation;
        }

        private void OnRequestSetMainMenuAccessibility(bool enabled)
        {
            m_FieldContext.SetMainMenuAccessible(enabled);
        }

        private void OnRequestSetSaveEnabled(bool enabled)
        {
            m_SaveEnabledStore.SetSaveEnabled(enabled);
        }

        private void OnRequestOpenSaveMenu()
        {
            RequestMenuModule((byte)MenuType.Save);
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
            Task     running   = RunDialogueAsync(channel, window, new TextDialogueFlow(), dialogues, blockMovement, null);

            m_DialogueChannels[channel].Running = running;
            running.FireAndForget();
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
            Task     running   = RunUnansweredDialogueAsync(channel, window, new UnansweredDialogueFlow(), dialogues);

            m_DialogueChannels[channel].Running = running;
            running.FireAndForget();
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

            Task running = RunDialogueAsync(channel, window, new ChoiceDialogueFlow(m_AudioIntentPlayer, AudioContext.Field), dialogues, true, storeChoice);

            m_DialogueChannels[channel].Running = running;
            running.FireAndForget();
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
            window.SetMessageSpeed(GetFieldMessageSpeed());

            dialogueChannel.Window = window;
            dialogueChannel.Close  = new CancellationTokenSource();

            return true;
        }

        private float GetFieldMessageSpeed()
        {
            m_SettingsService.TryGetSection(FrameworkSettingsSections.CONFIG_DATA, out SaveSection<ConfigData_V1> configData);

            float messageSpeed = configData.Data.FieldMessageSpeed;

            return messageSpeed;
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
                m_DialogueInputContext.BlockOtherInput();
            }

            CancellationToken close = m_DialogueChannels[channel].Close.Token;

            await window.AnimateWindowOpenAsync();
            await dialogueFlow.RunAsync(window, dialogues, m_DialogueInputContext, close);

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
            await dialogueFlow.RunAsync(window, dialogues, null, close);
            await CloseDialogueWindowAsync(channel, window);
        }

        /// <summary>
        /// The channel stays busy until the window has finished closing, so what is shown on it next waits for that.
        /// </summary>
        private async Task CloseDialogueWindowAsync(byte channel, IDialogueWindow window)
        {
            // Closing them all is for a field unloading, behind a covered screen.
            if (!m_ClosingAllDialogueWindows)
            {
                await window.AnimateWindowClosedAsync();
            }

            window.Destroy();

            FieldDialogueChannel dialogueChannel = m_DialogueChannels[channel];

            dialogueChannel.Close.Dispose();
            dialogueChannel.Close  = null;
            dialogueChannel.Window = null;
        }

        /// <summary>
        /// Close every window as <c>CLOSE_DIALOGUE_WINDOW</c> would, wait for each to finish, and give every channel back
        /// its defaults, so the next field starts with none open and sets its own rectangles and styles.
        /// </summary>
        private async Task CloseAllDialogueWindowsAsync()
        {
            m_ClosingAllDialogueWindows = true;

            for (int i = 0; i < m_DialogueChannels.Length; i++)
            {
                m_DialogueChannels[i].Close?.Cancel();
            }

            for (int i = 0; i < m_DialogueChannels.Length; i++)
            {
                Task running = m_DialogueChannels[i].Running;

                if (running != null)
                {
                    await running;
                }

                m_DialogueChannels[i] = new FieldDialogueChannel();
            }

            m_ClosingAllDialogueWindows = false;
        }

        private void OnRequestSetBattleMode(BattleFlags mode)
        {
            m_BattleModeStore.SetMode(mode);
        }

        /// <summary>
        /// A field change outranks a battle whichever was asked for first, so a battle is refused while one is pending
        /// and dropped when one is asked for.
        /// </summary>
        private void OnRequestStartBattle(BattleArgs args)
        {
            if (m_FieldTransitionRequested)
            {
                return;
            }

            m_BattleArgsStore.Set(args);

            m_BattleTransitionRequested = true;
        }

        private BattleCompleteState GetLastBattleResult()
        {
            BattleCompleteState result = m_BattleCompleteStateStore.State;

            return result;
        }

        private void OnRequestGameOver()
        {
            m_GameOverRequested = true;
        }

        // Above the script's input lock, which would otherwise swallow the press, as it may be locked before or after.
        private Func<bool> StartWaitingForButton(ControlSlot button)
        {
            ButtonWaitInputContext wait = new ButtonWaitInputContext(button, OnButtonWaitPressed);

            m_FieldContext.AddButtonWait(wait);
            m_InputRouter.Push(wait);

            Func<bool> pressed = () => wait.Pressed;

            return pressed;
        }

        private void OnButtonWaitPressed(ButtonWaitInputContext wait)
        {
            m_InputRouter.Pop(wait);
            m_FieldContext.RemoveButtonWait(wait);
        }

        private void RaiseButtonWaits()
        {
            for (int i = 0; i < m_FieldContext.ButtonWaits.Count; i++)
            {
                ButtonWaitInputContext wait = m_FieldContext.ButtonWaits[i];

                m_InputRouter.Pop(wait);
                m_InputRouter.Push(wait);
            }
        }
    }
}