using System;
using System.Collections.Generic;
using RPGFramework.Battle.SharedTypes;
using RPGFramework.Core;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.BlockState;
using RPGFramework.Field.FieldVmArgs;
using RPGFramework.Field.SharedTypes;
using UnityEngine;

namespace RPGFramework.Field
{
    internal sealed class FieldVM
    {
        internal event Action<FieldArgs>                       RequestFieldTransition;
        internal event Action<ulong, ulong>                    RequestMusic;
        internal event Action<ulong, float>                    RequestMusicStemState;
        internal event Action<ulong>                           RequestSfx;
        internal event Action<FieldEntityRuntime>              RequestSetPlayerEntity;
        internal event Action<int, bool>                       RequestSetEntityVisible;
        internal event Action<bool>                            RequestSetGatewayTriggersActive;
        internal event Action<int, bool>                       RequestSetInteractionTriggerActive;
        internal event Action<int, float>                      RequestSetInteractionRange;
        internal event Action<bool>                            RequestInputLock;
        internal event Action<int, Vector3>                    RequestSetEntityPosition;
        internal event Action<int, Quaternion>                 RequestSetEntityRotation;
        internal event Action<int, SetEntityRotationAsyncArgs> RequestSetEntityRotationAsync;
        internal Func<int, bool>                               IsEntityRotating;
        internal event Action<int, int>                        RequestSetEntityToFaceEntity;
        internal event Action<int, float>                      RequestSetEntityMovementSpeed;
        internal event Action<bool>                            RequestSetMainMenuAccessibility;
        internal event Action<DialogueWindowArgs>              RequestCreateDialogueWindow;
        internal event Action<ulong, bool>                     RequestShowDialogueWindow;
        internal Func<ulong, bool>                             IsDialogueWindowOpen;
        internal event Action<ulong, ulong[], Action<byte>>    RequestAskPlayerToMakeAChoice;
        internal Func<ulong, bool>                             IsPlayerMakingAChoice;
        internal event Action<BattleArgs>                      RequestSetBattleModeOptions;
        internal event Action                                  RequestStartBattle;

        private delegate void OpcodeHandler(ScriptExecutionContext ctx);

        private readonly Dictionary<(int entityId, byte priority), ScriptExecutionContext> m_Contexts;
        private readonly Dictionary<int, FieldEntityRuntime>                               m_Entities;
        private readonly Dictionary<FieldScriptOpCode, OpcodeHandler>                      m_OpcodeHandlers;
        private readonly Dictionary<int, byte[]>                                           m_Scripts;

        private readonly IMemoryService m_MemoryService;
        private readonly int            m_TempBytes;

        private System.Random m_Random = new System.Random();

        internal FieldVM(IMemoryService memoryService, int tempBytes)
        {
            m_Contexts       = new Dictionary<(int entityId, byte priority), ScriptExecutionContext>();
            m_Entities       = new Dictionary<int, FieldEntityRuntime>();
            m_OpcodeHandlers = BuildOpcodeHandlersArray();
            m_Scripts        = new Dictionary<int, byte[]>();

            m_MemoryService = memoryService;
            m_TempBytes     = tempBytes;
        }
        
        // TODO: once op codes are implemented, convert from dictionary to an array
        private Dictionary<FieldScriptOpCode, OpcodeHandler> BuildOpcodeHandlersArray()
        {
            return new Dictionary<FieldScriptOpCode, OpcodeHandler>
                   {
                       // Script flow and control
                       { FieldScriptOpCode.Return, ReturnOpcodeHandler },
                       { FieldScriptOpCode.RunAnotherEntityScriptUnlessBusy, RunAnotherEntityScriptUnlessBusyOpcodeHandler },
                       { FieldScriptOpCode.RunAnotherEntityScriptWaitUntilStarted, RunAnotherEntityScriptWaitUntilStartedOpcodeHandler },
                       { FieldScriptOpCode.RunAnotherEntityScriptWaitUntilFinished, RunAnotherEntityScriptWaitUntilFinishedOpcodeHandler },
                       { FieldScriptOpCode.ReturnToAnotherScript, ReturnToAnotherScriptOpcodeHandler },
                       { FieldScriptOpCode.GotoJump, GotoOpcodeHandler },
                       { FieldScriptOpCode.GotoDirectly, GotoDirectlyOpcodeHandler },
                       { FieldScriptOpCode.CompareTwoByteValues, CompareTwoByteValuesOpcodeHandler },
                       { FieldScriptOpCode.CompareTwoIntValues, CompareTwoIntValuesOpcodeHandler },
                       { FieldScriptOpCode.Yield, YieldOpcodeHandler },
                       { FieldScriptOpCode.WaitSeconds, WaitSecondsOpcodeHandler },
                       // { FieldScriptOpCode.IfKeyIsDown, IfKeyIsDownOpcodeHandler },
                       // { FieldScriptOpCode.IfKeyWasJustPressed, IfKeyWasJustPressedOpcodeHandler },
                       // { FieldScriptOpCode.IfKeyWasJustReleased, IfKeyWasJustReleasedOpcodeHandler },
                       { FieldScriptOpCode.DoNothing, DoNothingOpcodeHandler },
                       // { FieldScriptOpCode.IfCharacterIsInParty, IfCharacterIsInPartyOpcodeHandler },
                       // { FieldScriptOpCode.IfCharacterIsAvailable, IfCharacterIsAvailableOpcodeHandler },
                       // { FieldScriptOpCode.DebugLog, DebugLogOpcodeHandler },
                       { FieldScriptOpCode.CompareTwoBoolValues, CompareTwoBoolValuesOpcodeHandler },

                       // System and module control
                       // { FieldScriptOpCode.SpecialOp, SpecialOpOpcodeHandler },
                       // { FieldScriptOpCode.RunMinigame, RunMinigameOpcodeHandler },
                       { FieldScriptOpCode.SetBattleModeOptions, SetBattleModeOptionsOpcodeHandler },
                       // { FieldScriptOpCode.LoadResultOfLastBattle, LoadResultOfLastBattleOpcodeHandler },
                       // { FieldScriptOpCode.SetBattleEncounterTable, SetBattleEncounterTableOpcodeHandler },
                       { FieldScriptOpCode.JumpToAnotherMap, JumpToAnotherMapOpcodeHandler },
                       // { FieldScriptOpCode.GetLastFieldMap, GetLastFieldMapOpcodeHandler },
                       // { FieldScriptOpCode.SetJumpFieldID, SetJumpFieldIDOpcodeHandler },
                       { FieldScriptOpCode.StartBattle, StartBattleOpcodeHandler },
                       // { FieldScriptOpCode.RandomEncounters, RandomEncountersOpcodeHandler },
                       { FieldScriptOpCode.GatewayTriggerActivation, GatewayTriggerActivationOpcodeHandler },
                       // { FieldScriptOpCode.GameOver, GameOverOpcodeHandler },
                       // { FieldScriptOpCode.WorldMapJump, WorldMapJumpOpcodeHandler },
                       // { FieldScriptOpCode.SetSaveEnabled, SetSaveEnabledOpcodeHandler },

                       // Assignment and mathematics
                       { FieldScriptOpCode.AssignValue8Bit, AssignValue8BitOpcodeHandler },
                       { FieldScriptOpCode.AssignValue16Bit, AssignValue16BitOpcodeHandler },
                       { FieldScriptOpCode.SetBit, SetBitOpcodeHandler },
                       { FieldScriptOpCode.UnsetBit, UnsetBitOpcodeHandler },
                       { FieldScriptOpCode.Addition8Bit, Addition8BitOpcodeHandler },
                       { FieldScriptOpCode.Addition16Bit, Addition16BitOpcodeHandler },
                       { FieldScriptOpCode.Addition8BitClamped, Addition8BitClampedOpcodeHandler },
                       { FieldScriptOpCode.Addition16BitClamped, Addition16BitClampedOpcodeHandler },
                       { FieldScriptOpCode.Subtraction8Bit, Subtraction8BitOpcodeHandler },
                       { FieldScriptOpCode.Subtraction16Bit, Subtraction16BitOpcodeHandler },
                       { FieldScriptOpCode.Subtraction8BitClamped, Subtraction8BitClampedOpcodeHandler },
                       { FieldScriptOpCode.Subtraction16BitClamped, Subtraction16BitClampedOpcodeHandler },
                       { FieldScriptOpCode.Multiplication8Bit, Multiplication8BitOpcodeHandler },
                       { FieldScriptOpCode.Multiplication16Bit, Multiplication16BitOpcodeHandler },
                       { FieldScriptOpCode.Division8Bit, Division8BitOpcodeHandler },
                       { FieldScriptOpCode.Division16Bit, Division16BitOpcodeHandler },
                       { FieldScriptOpCode.Remainder8Bit, Remainder8BitOpcodeHandler },
                       { FieldScriptOpCode.Remainder16Bit, Remainder16BitOpcodeHandler },
                       { FieldScriptOpCode.BitwiseAnd8Bit, BitwiseAnd8BitOpcodeHandler },
                       { FieldScriptOpCode.BitwiseAnd16Bit, BitwiseAnd16BitOpcodeHandler },
                       { FieldScriptOpCode.BitwiseOr8Bit, BitwiseOr8BitOpcodeHandler },
                       { FieldScriptOpCode.BitwiseOr16Bit, BitwiseOr16BitOpcodeHandler },
                       { FieldScriptOpCode.BitwiseXor8Bit, BitwiseXor8BitOpcodeHandler },
                       { FieldScriptOpCode.BitwiseXor16Bit, BitwiseXor16BitOpcodeHandler },
                       { FieldScriptOpCode.Increment8Bit, Increment8BitOpcodeHandler },
                       { FieldScriptOpCode.Increment16Bit, Increment16BitOpcodeHandler },
                       { FieldScriptOpCode.Increment8BitClamped, Increment8BitClampedOpcodeHandler },
                       { FieldScriptOpCode.Increment16BitClamped, Increment16BitClampedOpcodeHandler },
                       { FieldScriptOpCode.Decrement8Bit, Decrement8BitOpcodeHandler },
                       { FieldScriptOpCode.Decrement16Bit, Decrement16BitOpcodeHandler },
                       { FieldScriptOpCode.Decrement8BitClamped, Decrement8BitClampedOpcodeHandler },
                       { FieldScriptOpCode.Decrement16BitClamped, Decrement16BitClampedOpcodeHandler },
                       { FieldScriptOpCode.GetRandomNumber, GetRandomNumberOpcodeHandler },
                       { FieldScriptOpCode.RandomNumberSeed, RandomNumberSeedOpcodeHandler },
                       // { FieldScriptOpCode.GetLowByte, GetLowByteOpcodeHandler },
                       // { FieldScriptOpCode.GetHighByte, GetHighByteOpcodeHandler },
                       // { FieldScriptOpCode.GetTwoBytes, GetTwoBytesOpcodeHandler },
                       // { FieldScriptOpCode.Sine, SineOpcodeHandler },
                       // { FieldScriptOpCode.Cosine, CosineOpcodeHandler },
                       { FieldScriptOpCode.AssignValueBool, AssignValueBoolOpcodeHandler },

                       // Windowing and menu
                       // { FieldScriptOpCode.RunTutorial, RunTutorialOpcodeHandler },
                       // { FieldScriptOpCode.CloseWindow, CloseWindowOpcodeHandler },
                       // { FieldScriptOpCode.ResizeWindow, ResizeWindowOpcodeHandler },
                       // { FieldScriptOpCode.CreateSpecialWindow, CreateSpecialWindowOpcodeHandler },
                       // { FieldScriptOpCode.SetNumberInWindow, SetNumberInWindowOpcodeHandler },
                       // { FieldScriptOpCode.SetTimeInWindow, SetTimeInWindowOpcodeHandler },
                       { FieldScriptOpCode.ShowDialogueWindow, ShowDialogueWindowOpcodeHandler },
                       // { FieldScriptOpCode.SetWindowTextValue, SetWindowTextValueOpcodeHandler },
                       // { FieldScriptOpCode.SetWindowTextValue16Bit, SetWindowTextValue16BitOpcodeHandler },
                       // { FieldScriptOpCode.SetMapNameInMenu, SetMapNameInMenuOpcodeHandler },
                       { FieldScriptOpCode.AskPlayerToMakeAChoice, AskPlayerToMakeAChoiceOpcodeHandler },
                       // { FieldScriptOpCode.MenuOperations, MenuOperationsOpcodeHandler },
                       { FieldScriptOpCode.MainMenuAccessibility, MainMenuAccessibilityOpcodeHandler },
                       { FieldScriptOpCode.CreateDialogueWindow, CreateDialogueWindowOpcodeHandler },
                       // { FieldScriptOpCode.SetWindowPosition, SetWindowPositionOpcodeHandler },
                       // { FieldScriptOpCode.SetWindowModes, SetWindowModesOpcodeHandler },
                       // { FieldScriptOpCode.ResetWindow, ResetWindowOpcodeHandler },
                       // { FieldScriptOpCode.SetNumberOfRowsInWindow, SetNumberOfRowsInWindowOpcodeHandler },
                       // { FieldScriptOpCode.SetMessageSpeed, SetMessageSpeedOpcodeHandler },

                       // Party and inventory
                       // { FieldScriptOpCode.ChangePartyMembers, ChangePartyMembersOpcodeHandler },
                       // { FieldScriptOpCode.StorePartyMembers, StorePartyMembersOpcodeHandler },
                       // { FieldScriptOpCode.IncreaseGil, IncreaseGilOpcodeHandler },
                       // { FieldScriptOpCode.DecreaseGil, DecreaseGilOpcodeHandler },
                       // { FieldScriptOpCode.GetGilAmount, GetGilAmountOpcodeHandler },
                       // { FieldScriptOpCode.RestoreHPMP, RestoreHPMPOpcodeHandler },
                       // { FieldScriptOpCode.IncreaseMP, IncreaseMPOpcodeHandler },
                       // { FieldScriptOpCode.DecreaseMP, DecreaseMPOpcodeHandler },
                       // { FieldScriptOpCode.IncreaseHP, IncreaseHPOpcodeHandler },
                       // { FieldScriptOpCode.DecreaseHP, DecreaseHPOpcodeHandler },
                       // { FieldScriptOpCode.AddItemToInventory, AddItemToInventoryOpcodeHandler },
                       // { FieldScriptOpCode.RemoveItemFromInventory, RemoveItemFromInventoryOpcodeHandler },
                       // { FieldScriptOpCode.GetItemCountFromInventory, GetItemCountFromInventoryOpcodeHandler },
                       // { FieldScriptOpCode.GetPartyMembersIdentity, GetPartyMembersIdentityOpcodeHandler },
                       // { FieldScriptOpCode.AddCharacterToParty, AddCharacterToPartyOpcodeHandler },
                       // { FieldScriptOpCode.RemoveCharacterFromParty, RemoveCharacterFromPartyOpcodeHandler },
                       // { FieldScriptOpCode.SetAllPartyCharacters, SetAllPartyCharactersOpcodeHandler },
                       // { FieldScriptOpCode.SetCharacterAvailability, SetCharacterAvailabilityOpcodeHandler },
                       // { FieldScriptOpCode.LockPartyMember, LockPartyMemberOpcodeHandler },
                       // { FieldScriptOpCode.UnlockPartyMember, UnlockPartyMemberOpcodeHandler },

                       // Field models and animation
                       // { FieldScriptOpCode.JoinPartyToLeader, JoinPartyToLeaderOpcodeHandler },
                       // { FieldScriptOpCode.SplitPartyFromLeader, SplitPartyFromLeaderOpcodeHandler },
                       // { FieldScriptOpCode.CharacterGraphicsOp, CharacterGraphicsOpOpcodeHandler },
                       // { FieldScriptOpCode.WaitForGraphicsOp, WaitForGraphicsOpOpcodeHandler },
                       // { FieldScriptOpCode.MoveToPartyMember, MoveToPartyMemberOpcodeHandler },
                       // { FieldScriptOpCode.SlipAgainstWalls, SlipAgainstWallsOpcodeHandler },
                       { FieldScriptOpCode.LockInput, LockInputOpcodeHandler },
                       // { FieldScriptOpCode.TurnToPartyMember, TurnToPartyMemberOpcodeHandler },
                       // { FieldScriptOpCode.CollisionDetection, CollisionDetectionOpcodeHandler },
                       // { FieldScriptOpCode.GetPartyMemberDirection, GetPartyMemberDirectionOpcodeHandler },
                       // { FieldScriptOpCode.GetPartyMemberPosition, GetPartyMemberPositionOpcodeHandler },
                       { FieldScriptOpCode.InteractionTriggerActivation, InteractabilityOpcodeHandler },
                       { FieldScriptOpCode.InitAsCharacter, InitAsCharacterOpcodeHandler },
                       // { FieldScriptOpCode.PlayAnimationLooping, PlayAnimationLoopingOpcodeHandler },
                       // { FieldScriptOpCode.PlayAnimationOnceAndWait, PlayAnimationOnceAndWaitOpcodeHandler },
                       { FieldScriptOpCode.Visibility, VisibilityOpcodeHandler },
                       { FieldScriptOpCode.SetEntityPosition, SetEntityPositionOpcodeHandler },
                       // { FieldScriptOpCode.MoveEntityToXYWalkAnimation, MoveEntityToXYWalkAnimationOpcodeHandler },
                       // { FieldScriptOpCode.MoveEntityToXYNoAnimation, MoveEntityToXYNoAnimationOpcodeHandler },
                       // { FieldScriptOpCode.MoveEntityToAnotherEntity, MoveEntityToAnotherEntityOpcodeHandler },
                       // { FieldScriptOpCode.TurnEntityToAnotherEntity, TurnEntityToAnotherEntityOpcodeHandler },
                       // { FieldScriptOpCode.WaitForAnimation, WaitForAnimationOpcodeHandler },
                       // { FieldScriptOpCode.MoveFieldObject, MoveFieldObjectOpcodeHandler },
                       // { FieldScriptOpCode.PlayAnimationAsync, PlayAnimationAsyncOpcodeHandler },
                       // { FieldScriptOpCode.PlayAnimationOnceAsync, PlayAnimationOnceAsyncOpcodeHandler },
                       // { FieldScriptOpCode.PlayPartialAnimation, PlayPartialAnimationOpcodeHandler },
                       { FieldScriptOpCode.SetMovementSpeed, SetMovementSpeedOpcodeHandler },
                       { FieldScriptOpCode.SetEntityRotation, SetEntityRotationOpcodeHandler },
                       { FieldScriptOpCode.SetEntityRotationAsync, SetEntityRotationAsyncOpcodeHandler },
                       { FieldScriptOpCode.SetDirectionToFaceEntity, SetDirectionToFaceEntityOpcodeHandler },
                       // { FieldScriptOpCode.GetEntityDirection, GetEntityDirectionOpcodeHandler },
                       // { FieldScriptOpCode.PlayAnimationStopOnLastFrameWait, PlayAnimationStopOnLastFrameWaitOpcodeHandler },
                       // { FieldScriptOpCode.SetAnimationSpeed, SetAnimationSpeedOpcodeHandler },
                       // { FieldScriptOpCode.SetEntityAsControllableCharacter, SetEntityAsControllableCharacterOpcodeHandler },
                       // { FieldScriptOpCode.MakeEntityJump, MakeEntityJumpOpcodeHandler },
                       // { FieldScriptOpCode.GetEntityPosition, GetEntityPositionXYZIOpcodeHandler },
                       // { FieldScriptOpCode.ClimbLadder, ClimbLadderOpcodeHandler },
                       // { FieldScriptOpCode.TransposeObjectVisualizationOnly, TransposeObjectVisualizationOnlyOpcodeHandler },
                       // { FieldScriptOpCode.WaitForTranspose, WaitForTransposeOpcodeHandler },
                       { FieldScriptOpCode.SetInteractionRange, SetInteractionRangeOpcodeHandler },
                       // { FieldScriptOpCode.SetCollisionRadius, SetCollisionRadiusOpcodeHandler },
                       // { FieldScriptOpCode.Collidability, CollidabilityOpcodeHandler },
                       // { FieldScriptOpCode.LineTriggerInitialization, LineTriggerInitializationOpcodeHandler },
                       // { FieldScriptOpCode.LineTriggerActivation, LineTriggerActivationOpcodeHandler },
                       // { FieldScriptOpCode.SetLine, SetLineOpcodeHandler },
                       // { FieldScriptOpCode.FixFacingForward, FixFacingForwardOpcodeHandler },
                       // { FieldScriptOpCode.SetAnimationID, SetAnimationIDOpcodeHandler },
                       // { FieldScriptOpCode.StopAnimation, StopAnimationOpcodeHandler },
                       // { FieldScriptOpCode.FlushMovement, FlushMovementOpcodeHandler },
                       // { FieldScriptOpCode.SetRunningEnabled, SetRunningEnabledOpcodeHandler },
                       // { FieldScriptOpCode.SetFootstepSound, SetFootstepSoundOpcodeHandler },
                       // { FieldScriptOpCode.LockWalkmeshRegion, LockWalkmeshRegionOpcodeHandler },
                       // { FieldScriptOpCode.UnlockWalkmeshRegion, UnlockWalkmeshRegionOpcodeHandler },
                       // { FieldScriptOpCode.InitialiseHeadFacing, InitialiseHeadFacingOpcodeHandler },
                       // { FieldScriptOpCode.SetHeadFacingEntity, SetHeadFacingEntityOpcodeHandler },
                       // { FieldScriptOpCode.SetHeadFacingPlayer, SetHeadFacingPlayerOpcodeHandler },
                       // { FieldScriptOpCode.SetHeadFacingLimit, SetHeadFacingLimitOpcodeHandler },
                       // { FieldScriptOpCode.SetHeadPose, SetHeadPoseOpcodeHandler },
                       // { FieldScriptOpCode.StopHeadFacing, StopHeadFacingOpcodeHandler },

                       // Background and screen tint
                       // { FieldScriptOpCode.SetBackgroundDepth, SetBackgroundDepthOpcodeHandler },
                       // { FieldScriptOpCode.ScrollBackground, ScrollBackgroundOpcodeHandler },
                       // { FieldScriptOpCode.BackgroundOn, BackgroundOnOpcodeHandler },
                       // { FieldScriptOpCode.BackgroundOff, BackgroundOffOpcodeHandler },
                       // { FieldScriptOpCode.BackgroundRollForward, BackgroundRollForwardOpcodeHandler },
                       // { FieldScriptOpCode.BackgroundRollBackward, BackgroundRollBackwardOpcodeHandler },
                       // { FieldScriptOpCode.BackgroundClear, BackgroundClearOpcodeHandler },
                       // { FieldScriptOpCode.SetShadeLevel, SetShadeLevelOpcodeHandler },
                       // { FieldScriptOpCode.SubtractiveScreenFade, SubtractiveScreenFadeOpcodeHandler },

                       // Camera and screen movement
                       // { FieldScriptOpCode.FadeScreen, FadeScreenOpcodeHandler },
                       // { FieldScriptOpCode.FadeScreenWait, FadeScreenWaitOpcodeHandler },
                       // { FieldScriptOpCode.WaitForFade, WaitForFadeOpcodeHandler },
                       // { FieldScriptOpCode.ShakeScreen, ShakeScreenOpcodeHandler },
                       // { FieldScriptOpCode.ScrollScreen, ScrollScreenOpcodeHandler },
                       // { FieldScriptOpCode.ScrollScreenToEntity, ScrollScreenToEntityOpcodeHandler },
                       // { FieldScriptOpCode.ScrollScreenToPosition, ScrollScreenToPositionOpcodeHandler },
                       // { FieldScriptOpCode.ScrollScreenToLeader, ScrollScreenToLeaderOpcodeHandler },
                       // { FieldScriptOpCode.ScrollToPartyMember, ScrollToPartyMemberOpcodeHandler },
                       // { FieldScriptOpCode.StartTheScreenToPositionEaseInOut, StartTheScreenToPositionEaseInOutOpcodeHandler },
                       // { FieldScriptOpCode.StartTheScreenToPositionLinear, StartTheScreenToPositionLinearOpcodeHandler },
                       // { FieldScriptOpCode.WaitForScrolling, WaitForScrollingOpcodeHandler },

                       // Audio
                       // { FieldScriptOpCode.MusicOperation, MusicOperationOpcodeHandler },
                       { FieldScriptOpCode.PlayMusic, PlayMusicOpcodeHandler },
                       { FieldScriptOpCode.PlaySound, PlaySoundOpcodeHandler },
                       // { FieldScriptOpCode.MusicLockMode, MusicLockModeOpcodeHandler },
                       // { FieldScriptOpCode.SetBattleMusic, SetBattleMusicOpcodeHandler },
                       // { FieldScriptOpCode.CheckIfMusicIsPlaying, CheckIfMusicIsPlayingOpcodeHandler },
                       // { FieldScriptOpCode.PlayAmbientLoop, PlayAmbientLoopOpcodeHandler },
                       // { FieldScriptOpCode.StopSound, StopSoundOpcodeHandler },
                       // { FieldScriptOpCode.PreserveSoundChannel, PreserveSoundChannelOpcodeHandler },
                       // { FieldScriptOpCode.SetSoundVolume, SetSoundVolumeOpcodeHandler },
                       // { FieldScriptOpCode.FadeSoundVolume, FadeSoundVolumeOpcodeHandler },
                       // { FieldScriptOpCode.SetSoundPan, SetSoundPanOpcodeHandler },
                       // { FieldScriptOpCode.FadeSoundPan, FadeSoundPanOpcodeHandler },
                       // { FieldScriptOpCode.SetAllSoundVolume, SetAllSoundVolumeOpcodeHandler },
                       // { FieldScriptOpCode.FadeAllSoundVolume, FadeAllSoundVolumeOpcodeHandler },
                       // { FieldScriptOpCode.SetAllSoundPan, SetAllSoundPanOpcodeHandler },
                       // { FieldScriptOpCode.FadeAllSoundPan, FadeAllSoundPanOpcodeHandler },
                       { FieldScriptOpCode.SetMusicStemState, SetMusicStemStateOpcodeHandler },

                       // Video
                       // { FieldScriptOpCode.PrepareMovie, PrepareMovieOpcodeHandler },
                       // { FieldScriptOpCode.PlayMovie, PlayMovieOpcodeHandler },
                       // { FieldScriptOpCode.WaitForMovie, WaitForMovieOpcodeHandler },

                       // Timer
                       // { FieldScriptOpCode.SetCountdownTimer, SetCountdownTimerOpcodeHandler },
                       // { FieldScriptOpCode.ShowCountdownTimer, ShowCountdownTimerOpcodeHandler },

                       // Input and haptics
                       // { FieldScriptOpCode.SetVibration, SetVibrationOpcodeHandler },
                       // { FieldScriptOpCode.SetKeyEnabled, SetKeyEnabledOpcodeHandler },
                   };
        }

        internal void RegisterEntity(int entityId, FieldEntityRuntime entity)
        {
            m_Entities.Add(entityId, entity);
        }

        /// <summary>
        /// Take a compiled script, unless it was compiled for a different bytecode layout.<br /><br />
        /// A stale script is refused rather than run. Its slot then finds no script and clears, so the
        /// entity goes quiet instead of executing the wrong instructions — see
        /// <see cref="FieldCompiledScript.CURRENT_FORMAT_VERSION" /> for why that matters.
        /// </summary>
        internal void RegisterScript(int scriptId, FieldCompiledScript script)
        {
            if (script.FormatVersion != FieldCompiledScript.CURRENT_FORMAT_VERSION)
            {
                Debug.LogError($"{nameof(FieldVM)}::{nameof(RegisterScript)} [{script.name}] was compiled for bytecode format [{script.FormatVersion}] but this build reads format [{FieldCompiledScript.CURRENT_FORMAT_VERSION}], so it has not been loaded. Recompile it. Format [0] means it was compiled before the format carried a version");

                return;
            }

            m_Scripts.Add(scriptId, script.Bytecode);
        }

        /// <summary>
        /// Start one of an entity's scripts by event id, for something the engine initiates rather than
        /// another script — a gateway or an interaction trigger. Refused if the slot is busy.
        /// </summary>
        internal void RequestScript(int entityId, int eventId, byte priority)
        {
            FieldEntityRuntime entity = m_Entities[entityId];

            if (!entity.TryGetScriptId(eventId, out int scriptId))
            {
                return;
            }

            entity.TryRequestScript(scriptId, priority);
        }

        internal const int INSTRUCTIONS_PER_SLOT_PER_FRAME = 16;

        /// <summary>
        /// Init runs until it returns, however many instructions that takes. This only stops one that
        /// never returns, which would otherwise hang the field load.
        /// </summary>
        internal const int INIT_INSTRUCTION_CEILING = 1024;

        /// <summary>
        /// Replace an entity's init script with its Main script, whether or not init returned. An entity
        /// with no Main script is left with the slot empty.
        /// </summary>
        internal void StartMainScript(int entityId, int mainEventId)
        {
            FieldEntityRuntime entity = m_Entities[entityId];

            m_Contexts.Remove((entityId, FieldEntityRuntime.MAIN_PRIORITY));

            if (entity.TryGetScriptId(mainEventId, out int scriptId))
            {
                entity.ReplaceScriptInSlot(scriptId, FieldEntityRuntime.MAIN_PRIORITY);
                return;
            }

            entity.ClearSlot(FieldEntityRuntime.MAIN_PRIORITY);
        }

        internal ScriptRunOutcome Execute(int entityId, byte priority, int scriptId, FieldEntityRuntime entity, int instructionBudget)
        {
            (int entityId, byte priority) key = (entityId, priority);

            // A slot outlives the scripts that pass through it, so a context is reused only while it is
            // still running the script the slot currently holds.
            if (!m_Contexts.TryGetValue(key, out ScriptExecutionContext ctx) || ctx.ScriptId != scriptId)
            {
                if (!m_Scripts.TryGetValue(scriptId, out byte[] bytecode))
                {
                    entity.ClearSlot(priority);
                    return ScriptRunOutcome.Ended;
                }

                ctx = new ScriptExecutionContext
                      {
                          EntityId           = entityId,
                          Priority           = priority,
                          ScriptId           = scriptId,
                          InstructionPointer = 0,
                          Bytecode           = bytecode
                      };
                m_Contexts[key] = ctx;
            }

            // The frame a block completes on is spent, so the next instruction runs next frame.
            if (ctx.IsBlocked())
            {
                ctx.UpdateBlock(Time.deltaTime);
                return ScriptRunOutcome.Waiting;
            }

            int instructionsRun = 0;

            while (!ctx.IsBlocked())
            {
                if (instructionsRun == instructionBudget)
                {
                    return ScriptRunOutcome.OutOfInstructions;
                }

                instructionsRun++;

                FieldScriptOpCode opcode = FetchOpcode(ctx);

                if (!m_OpcodeHandlers.TryGetValue(opcode, out OpcodeHandler opcodeHandler))
                {
                    m_Contexts.Remove(key);
                    entity.ClearSlot(priority);
                    return ScriptRunOutcome.Ended;
                }

                opcodeHandler(ctx);

                if (opcode == FieldScriptOpCode.Return)
                {
                    m_Contexts.Remove(key);
                    entity.ClearSlot(priority);
                    return ScriptRunOutcome.Ended;
                }

                if (ctx.YieldRequested)
                {
                    ctx.YieldRequested = false;
                    return ScriptRunOutcome.Waiting;
                }
            }

            return ScriptRunOutcome.Waiting;
        }

        private static FieldScriptOpCode FetchOpcode(ScriptExecutionContext ctx)
        {
            FieldScriptOpCode opcode = (FieldScriptOpCode)ReadUshort(ctx);

            return opcode;
        }

        // Argument sources are packed two to a byte: the high nibble selects where the first argument
        // (the destination, for anything that writes) comes from, the low nibble the second.
        //
        //   0 = immediate, the value follows inline in the bytecode
        //   1 = Persistent bank  2 = Session bank      3 = Temp
        //
        // Bank addresses are ushort, matching IMemoryService, so a script can reach any variable the
        // variable map declares.
        private const byte ARGUMENT_IMMEDIATE = 0;

        private static byte GetFirstArgumentSource(byte sources)
        {
            byte source = (byte)(sources >> 4);

            return source;
        }

        private static byte GetSecondArgumentSource(byte sources)
        {
            byte source = (byte)(sources & 0x0F);

            return source;
        }

        private static MemoryBank ToMemoryBank(byte source)
        {
            if (source == ARGUMENT_IMMEDIATE)
            {
                throw new InvalidOperationException($"{nameof(FieldVM)}::{nameof(ToMemoryBank)} Argument source [{source}] is an immediate value, not a bank. A destination argument must name a bank");
            }

            MemoryBank bank = (MemoryBank)(source - 1);

            return bank;
        }

        /// <summary>
        /// Read an 8 bit argument: either an inline literal byte, or a ushort address into a bank.
        /// </summary>
        private byte ReadArgumentByte(ScriptExecutionContext ctx, byte source)
        {
            if (source == ARGUMENT_IMMEDIATE)
            {
                byte immediate = ReadByte(ctx);

                return immediate;
            }

            ushort address = ReadUshort(ctx);
            byte   value   = ReadVariableByte(ctx, ToMemoryBank(source), address);

            return value;
        }

        /// <summary>
        /// Read a 16 bit argument: either an inline literal ushort, or a ushort address into a bank.
        /// </summary>
        private ushort ReadArgumentUshort(ScriptExecutionContext ctx, byte source)
        {
            if (source == ARGUMENT_IMMEDIATE)
            {
                ushort immediate = ReadUshort(ctx);

                return immediate;
            }

            ushort address = ReadUshort(ctx);
            ushort value   = ReadVariableUshort(ctx, ToMemoryBank(source), address);

            return value;
        }

        /// <summary>
        /// Read an int argument: either an inline literal int, or a ushort address into a bank.
        /// </summary>
        private int ReadArgumentInt(ScriptExecutionContext ctx, byte source)
        {
            if (source == ARGUMENT_IMMEDIATE)
            {
                int immediate = ReadInt(ctx);

                return immediate;
            }

            ushort address = ReadUshort(ctx);
            int    value   = ReadVariableInt(ctx, ToMemoryBank(source), address);

            return value;
        }

        private float ReadArgumentFloat(ScriptExecutionContext ctx, byte source)
        {
            if (source == ARGUMENT_IMMEDIATE)
            {
                float immediate = ReadFloat(ctx);

                return immediate;
            }

            ushort address = ReadUshort(ctx);
            float  value   = ReadVariableFloat(ctx, ToMemoryBank(source), address);

            return value;
        }

        private bool ReadArgumentBool(ScriptExecutionContext ctx, byte source)
        {
            if (source == ARGUMENT_IMMEDIATE)
            {
                bool immediate = ReadBool(ctx);

                return immediate;
            }

            ushort address = ReadUshort(ctx);
            bool   value   = ReadVariableBool(ctx, ToMemoryBank(source), address);

            return value;
        }

        /// <summary>
        /// The sources of a <see cref="ArgumentLayout.Sequential" /> opcode's arguments. A sources byte precedes
        /// every pair of arguments that can come from a variable, so the second argument of a pair uses the low
        /// nibble left over from the first.
        /// </summary>
        private struct SequentialSources
        {
            internal bool HasPending;
            internal byte Pending;
        }

        private static byte NextSource(ScriptExecutionContext ctx, ref SequentialSources sources)
        {
            if (sources.HasPending)
            {
                sources.HasPending = false;

                return sources.Pending;
            }

            byte packed = ReadByte(ctx);

            sources.Pending    = GetSecondArgumentSource(packed);
            sources.HasPending = true;

            byte source = GetFirstArgumentSource(packed);

            return source;
        }

        private byte ReadArgumentByte(ScriptExecutionContext ctx, ref SequentialSources sources)
        {
            byte value = ReadArgumentByte(ctx, NextSource(ctx, ref sources));

            return value;
        }

        private ushort ReadArgumentUshort(ScriptExecutionContext ctx, ref SequentialSources sources)
        {
            ushort value = ReadArgumentUshort(ctx, NextSource(ctx, ref sources));

            return value;
        }

        private int ReadArgumentInt(ScriptExecutionContext ctx, ref SequentialSources sources)
        {
            int value = ReadArgumentInt(ctx, NextSource(ctx, ref sources));

            return value;
        }

        private float ReadArgumentFloat(ScriptExecutionContext ctx, ref SequentialSources sources)
        {
            float value = ReadArgumentFloat(ctx, NextSource(ctx, ref sources));

            return value;
        }

        private bool ReadArgumentBool(ScriptExecutionContext ctx, ref SequentialSources sources)
        {
            bool value = ReadArgumentBool(ctx, NextSource(ctx, ref sources));

            return value;
        }

        private TempMemory TempOf(ScriptExecutionContext ctx)
        {
            // Created on first use: most scripts never touch temp memory.
            TempMemory temp = ctx.Temp ??= new TempMemory(m_TempBytes);

            return temp;
        }

        // Temp variables live in the running script's own memory; Persistent and Session in the memory service.
        private byte ReadVariableByte(ScriptExecutionContext ctx, MemoryBank bank, ushort address)
        {
            byte value = bank == MemoryBank.Temp ? TempOf(ctx).ReadByte(address) : m_MemoryService.ReadByte(bank, address);

            return value;
        }

        private void WriteVariableByte(ScriptExecutionContext ctx, MemoryBank bank, ushort address, byte value)
        {
            if (bank == MemoryBank.Temp)
            {
                TempOf(ctx).WriteByte(address, value);
                return;
            }

            m_MemoryService.WriteByte(bank, address, value);
        }

        private bool ReadVariableBool(ScriptExecutionContext ctx, MemoryBank bank, ushort address)
        {
            bool value = bank == MemoryBank.Temp ? TempOf(ctx).ReadBool(address) : m_MemoryService.ReadBool(bank, address);

            return value;
        }

        private void WriteVariableBool(ScriptExecutionContext ctx, MemoryBank bank, ushort address, bool value)
        {
            if (bank == MemoryBank.Temp)
            {
                TempOf(ctx).WriteBool(address, value);
                return;
            }

            m_MemoryService.WriteBool(bank, address, value);
        }

        private ushort ReadVariableUshort(ScriptExecutionContext ctx, MemoryBank bank, ushort address)
        {
            ushort value = bank == MemoryBank.Temp ? TempOf(ctx).ReadUshort(address) : m_MemoryService.ReadUshort(bank, address);

            return value;
        }

        private void WriteVariableUshort(ScriptExecutionContext ctx, MemoryBank bank, ushort address, ushort value)
        {
            if (bank == MemoryBank.Temp)
            {
                TempOf(ctx).WriteUshort(address, value);
                return;
            }

            m_MemoryService.WriteUshort(bank, address, value);
        }

        private int ReadVariableInt(ScriptExecutionContext ctx, MemoryBank bank, ushort address)
        {
            int value = bank == MemoryBank.Temp ? TempOf(ctx).ReadInt(address) : m_MemoryService.ReadInt(bank, address);

            return value;
        }

        private float ReadVariableFloat(ScriptExecutionContext ctx, MemoryBank bank, ushort address)
        {
            float value = bank == MemoryBank.Temp ? TempOf(ctx).ReadFloat(address) : m_MemoryService.ReadFloat(bank, address);

            return value;
        }

        /// <summary>
        /// Decode a destination variable and one byte argument: sources byte, destination address, then the
        /// argument as an immediate or a bank address.
        /// </summary>
        private void ReadBinaryArgumentsByte(ScriptExecutionContext ctx, out MemoryBank destinationBank, out ushort destinationAddress, out byte argument)
        {
            byte sources = ReadByte(ctx);

            destinationBank    = ToMemoryBank(GetFirstArgumentSource(sources));
            destinationAddress = ReadUshort(ctx);
            argument           = ReadArgumentByte(ctx, GetSecondArgumentSource(sources));
        }

        private void ReadBinaryArgumentsUshort(ScriptExecutionContext ctx, out MemoryBank destinationBank, out ushort destinationAddress, out ushort argument)
        {
            byte sources = ReadByte(ctx);

            destinationBank    = ToMemoryBank(GetFirstArgumentSource(sources));
            destinationAddress = ReadUshort(ctx);
            argument           = ReadArgumentUshort(ctx, GetSecondArgumentSource(sources));
        }

        /// <summary>
        /// Decode an opcode that only names a destination, such as an increment.
        /// </summary>
        private static void ReadDestination(ScriptExecutionContext ctx, out MemoryBank destinationBank, out ushort destinationAddress)
        {
            byte sources = ReadByte(ctx);

            destinationBank    = ToMemoryBank(GetFirstArgumentSource(sources));
            destinationAddress = ReadUshort(ctx);
        }

        /// <summary>
        /// Decode the arguments shared by the three request opcodes and resolve the target entity.
        /// </summary>
        private bool TryReadScriptRequest(ScriptExecutionContext ctx, out FieldEntityRuntime target, out int targetScriptId, out byte priority)
        {
            SequentialSources sources        = default;
            byte              targetEntityId = ReadArgumentByte(ctx, ref sources);
            ushort            targetEventId;

            priority      = ReadArgumentByte(ctx, ref sources);
            targetEventId = ReadArgumentUshort(ctx, ref sources);

            targetScriptId = 0;

            if (!m_Entities.TryGetValue(targetEntityId, out target))
            {
                return false;
            }

            if (priority >= FieldEntityRuntime.PRIORITY_COUNT)
            {
                target = null;
                return false;
            }

            // The request names an event id relative to the target entity, which the entity resolves to
            // the field-wide script id the VM holds bytecode under.
            if (!target.TryGetScriptId(targetEventId, out targetScriptId))
            {
                target = null;
                return false;
            }

            return true;
        }

        private static byte ReadByte(ScriptExecutionContext ctx)
        {
            ReadOnlySpan<byte> bytecode = ctx.Bytecode.AsSpan();

            byte value = bytecode[ctx.InstructionPointer];
            ctx.InstructionPointer += sizeof(byte);

            return value;
        }

        private static bool ReadBool(ScriptExecutionContext ctx)
        {
            ReadOnlySpan<byte> bytecode = ctx.Bytecode.AsSpan();

            byte value = bytecode[ctx.InstructionPointer];
            ctx.InstructionPointer += sizeof(byte);

            return value != 0;
        }

        private static ushort ReadUshort(ScriptExecutionContext ctx)
        {
            ReadOnlySpan<byte> bytecode = ctx.Bytecode.AsSpan();

            ushort value = (ushort)(bytecode[ctx.InstructionPointer] | bytecode[ctx.InstructionPointer + 1] << 8);

            ctx.InstructionPointer += sizeof(ushort);

            return value;
        }

        private static int ReadInt(ScriptExecutionContext ctx)
        {
            ReadOnlySpan<byte> bytecode = ctx.Bytecode.AsSpan();

            int value = bytecode[ctx.InstructionPointer]           |
                        bytecode[ctx.InstructionPointer + 1] << 8  |
                        bytecode[ctx.InstructionPointer + 2] << 16 |
                        bytecode[ctx.InstructionPointer + 3] << 24;

            ctx.InstructionPointer += sizeof(int);

            return value;
        }

        private static float ReadFloat(ScriptExecutionContext ctx)
        {
            int   bits  = ReadInt(ctx);
            float value = BitConverter.Int32BitsToSingle(bits);

            return value;
        }

        private static ulong ReadUlong(ScriptExecutionContext ctx)
        {
            ReadOnlySpan<byte> bytecode = ctx.Bytecode.AsSpan();

            ulong value = bytecode[ctx.InstructionPointer]                  |
                          (ulong)bytecode[ctx.InstructionPointer + 1] << 8  |
                          (ulong)bytecode[ctx.InstructionPointer + 2] << 16 |
                          (ulong)bytecode[ctx.InstructionPointer + 3] << 24 |
                          (ulong)bytecode[ctx.InstructionPointer + 4] << 32 |
                          (ulong)bytecode[ctx.InstructionPointer + 5] << 40 |
                          (ulong)bytecode[ctx.InstructionPointer + 6] << 48 |
                          (ulong)bytecode[ctx.InstructionPointer + 7] << 56;

            ctx.InstructionPointer += sizeof(ulong);

            return value;
        }

        /// <summary>
        /// End the script. Execute frees the slot once this has run, so there is nothing to do here.
        /// </summary>
        private static void ReturnOpcodeHandler(ScriptExecutionContext ctx)
        {
            // noop
        }

        /// <summary>
        /// Queue a script in another entity's priority slot and carry on. If the slot is busy the
        /// request is refused and the caller does not learn of it — use the waiting variants when the
        /// request must land.
        /// </summary>
        private void RunAnotherEntityScriptUnlessBusyOpcodeHandler(ScriptExecutionContext ctx)
        {
            if (!TryReadScriptRequest(ctx, out FieldEntityRuntime target, out int targetScriptId, out byte priority))
            {
                return;
            }

            target.TryRequestScript(targetScriptId, priority);
        }

        /// <summary>
        /// Retry until the slot accepts the script, then continue without waiting for it to finish.
        /// </summary>
        private void RunAnotherEntityScriptWaitUntilStartedOpcodeHandler(ScriptExecutionContext ctx)
        {
            if (!TryReadScriptRequest(ctx, out FieldEntityRuntime target, out int targetScriptId, out byte priority))
            {
                return;
            }

            ctx.Block(new RequestScriptBlock(target, targetScriptId, priority, false));
        }

        /// <summary>
        /// Retry until the slot accepts the script, then wait for it to run to its return before
        /// continuing.
        /// </summary>
        private void RunAnotherEntityScriptWaitUntilFinishedOpcodeHandler(ScriptExecutionContext ctx)
        {
            if (!TryReadScriptRequest(ctx, out FieldEntityRuntime target, out int targetScriptId, out byte priority))
            {
                return;
            }

            ctx.Block(new RequestScriptBlock(target, targetScriptId, priority, true));
        }

        /// <summary>
        /// Hand this slot over to another script. The script being replaced is the one asking, so unlike
        /// a request from outside this replaces rather than being refused.
        /// </summary>
        private void ReturnToAnotherScriptOpcodeHandler(ScriptExecutionContext ctx)
        {
            SequentialSources sources       = default;
            ushort            targetEventId = ReadArgumentUshort(ctx, ref sources);

            FieldEntityRuntime entity = m_Entities[ctx.EntityId];

            if (!entity.TryGetScriptId(targetEventId, out int targetScriptId))
            {
                return;
            }

            m_Contexts.Remove((ctx.EntityId, ctx.Priority));

            entity.ReplaceScriptInSlot(targetScriptId, ctx.Priority);
        }

        /// <summary>
        /// Move the instruction pointer by a signed offset, counted from the end of this instruction.
        /// </summary>
        private static void GotoOpcodeHandler(ScriptExecutionContext ctx)
        {
            int offset = ReadInt(ctx);
            ctx.InstructionPointer += offset;
        }

        /// <summary>
        /// Move the instruction pointer to an absolute position in the script.
        /// </summary>
        private static void GotoDirectlyOpcodeHandler(ScriptExecutionContext ctx)
        {
            int target = ReadInt(ctx);
            ctx.InstructionPointer = target;
        }

        /// <summary>
        /// Compare two bytes, skipping the IF body when the comparison fails.
        /// </summary>
        private void CompareTwoByteValuesOpcodeHandler(ScriptExecutionContext ctx)
        {
            byte sources = ReadByte(ctx);

            byte a = ReadArgumentByte(ctx, GetFirstArgumentSource(sources));
            byte b = ReadArgumentByte(ctx, GetSecondArgumentSource(sources));

            byte comparisonType = ReadByte(ctx);
            byte jumpAmount     = ReadByte(ctx);

            bool result = (ScriptComparison)comparisonType switch
                          {
                              ScriptComparison.Equal              => a              == b,
                              ScriptComparison.NotEqual           => a              != b,
                              ScriptComparison.GreaterThan        => a              > b,
                              ScriptComparison.LessThan           => a              < b,
                              ScriptComparison.GreaterThanOrEqual => a              >= b,
                              ScriptComparison.LessThanOrEqual    => a              <= b,
                              ScriptComparison.AnyBitInCommon     => (a & b)        != 0,
                              ScriptComparison.AnyBitDifferent    => (a ^ b)        != 0,
                              ScriptComparison.AnyBitSet          => (a | b)        != 0,
                              ScriptComparison.BitIsSet           => (a & (1 << b)) != 0,
                              ScriptComparison.BitIsClear         => (a & (1 << b)) == 0,
                              _                                   => throw new InvalidOperationException($"{nameof(FieldVM)}::{nameof(CompareTwoByteValuesOpcodeHandler)} Unknown comparison [{comparisonType}]")
                          };

            if (!result)
            {
                ctx.InstructionPointer += jumpAmount;
            }
        }

        /// <summary>
        /// Compare two ints, skipping the IF body when the comparison fails.
        /// </summary>
        private void CompareTwoIntValuesOpcodeHandler(ScriptExecutionContext ctx)
        {
            byte sources = ReadByte(ctx);

            int a = ReadArgumentInt(ctx, GetFirstArgumentSource(sources));
            int b = ReadArgumentInt(ctx, GetSecondArgumentSource(sources));

            byte comparisonType = ReadByte(ctx);
            byte jumpAmount     = ReadByte(ctx);

            bool result = (ScriptComparison)comparisonType switch
                          {
                              ScriptComparison.Equal              => a              == b,
                              ScriptComparison.NotEqual           => a              != b,
                              ScriptComparison.GreaterThan        => a              > b,
                              ScriptComparison.LessThan           => a              < b,
                              ScriptComparison.GreaterThanOrEqual => a              >= b,
                              ScriptComparison.LessThanOrEqual    => a              <= b,
                              ScriptComparison.AnyBitInCommon     => (a & b)        != 0,
                              ScriptComparison.AnyBitDifferent    => (a ^ b)        != 0,
                              ScriptComparison.AnyBitSet          => (a | b)        != 0,
                              ScriptComparison.BitIsSet           => (a & (1 << b)) != 0,
                              ScriptComparison.BitIsClear         => (a & (1 << b)) == 0,
                              _                                   => throw new InvalidOperationException($"{nameof(FieldVM)}::{nameof(CompareTwoIntValuesOpcodeHandler)} Unknown comparison [{comparisonType}]")
                          };

            if (!result)
            {
                ctx.InstructionPointer += jumpAmount;
            }
        }

        /// <summary>
        /// Stop running this script for the rest of the frame; it carries on from the next instruction next
        /// frame.
        /// </summary>
        private static void YieldOpcodeHandler(ScriptExecutionContext ctx)
        {
            ctx.YieldRequested = true;
        }

        /// <summary>
        /// Block this script for a number of seconds.
        /// </summary>
        private void WaitSecondsOpcodeHandler(ScriptExecutionContext ctx)
        {
            SequentialSources sources = default;
            float             seconds = ReadArgumentFloat(ctx, ref sources);
            ctx.Block(new WaitSecondsBlock(seconds));
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        private static void DoNothingOpcodeHandler(ScriptExecutionContext ctx)
        {
            // noop
        }

        /// <summary>
        /// Compare two bools with == or !=, skipping the IF body when the comparison fails.
        /// </summary>
        private void CompareTwoBoolValuesOpcodeHandler(ScriptExecutionContext ctx)
        {
            byte sources = ReadByte(ctx);

            bool a = ReadArgumentBool(ctx, GetFirstArgumentSource(sources));
            bool b = ReadArgumentBool(ctx, GetSecondArgumentSource(sources));

            byte comparisonType = ReadByte(ctx);
            byte jumpAmount     = ReadByte(ctx);

            bool result = (ScriptComparison)comparisonType switch
                          {
                              ScriptComparison.Equal    => a == b,
                              ScriptComparison.NotEqual => a != b,
                              _ => throw new InvalidOperationException($"{nameof(FieldVM)}::{nameof(CompareTwoBoolValuesOpcodeHandler)} Unknown comparison [{comparisonType}]")
                          };

            if (!result)
            {
                ctx.InstructionPointer += jumpAmount;
            }
        }

        /// <summary>
        /// Set the arena, enemy group, flags and enemy level for the next battle.
        /// </summary>
        private void SetBattleModeOptionsOpcodeHandler(ScriptExecutionContext ctx)
        {
            SequentialSources sources     = default;
            ushort            arena       = ReadArgumentUshort(ctx, ref sources);
            ushort            enemyGroup  = ReadArgumentUshort(ctx, ref sources);
            ushort            battleFlags = ReadArgumentUshort(ctx, ref sources);
            byte              enemyLevel  = ReadArgumentByte(ctx, ref sources);

            BattleArgs args = new BattleArgs(arena, enemyGroup, (BattleFlags)battleFlags, enemyLevel);

            RequestSetBattleModeOptions?.Invoke(args);
        }

        /// <summary>
        /// Ask for a transition to another field, entering at one of its spawn points. The script stops here:
        /// nothing after a map jump runs.
        /// </summary>
        private void JumpToAnotherMapOpcodeHandler(ScriptExecutionContext ctx)
        {
            SequentialSources sources       = default;
            ulong             fieldNameHash = ReadUlong(ctx);
            int               spawnId       = ReadArgumentInt(ctx, ref sources);

            FieldArgs args = new FieldArgs(fieldNameHash, spawnId);
            RequestFieldTransition?.Invoke(args);

            ctx.Block(new WaitUntilBlock(() => false));
        }

        /// <summary>
        /// Ask for the battle set up by SET_BATTLE_MODE_OPTIONS to begin.
        /// </summary>
        private void StartBattleOpcodeHandler(ScriptExecutionContext ctx)
        {
            RequestStartBattle?.Invoke();
        }

        /// <summary>
        /// Turn every gateway trigger in the field on or off.
        /// </summary>
        private void GatewayTriggerActivationOpcodeHandler(ScriptExecutionContext ctx)
        {
            SequentialSources sources = default;
            bool              enabled = ReadArgumentBool(ctx, ref sources);
            RequestSetGatewayTriggersActive?.Invoke(enabled);
        }

        /// <summary>
        /// Write a byte to a variable.
        /// </summary>
        private void AssignValue8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsByte(ctx, out MemoryBank bank, out ushort address, out byte argument);

            WriteVariableByte(ctx, bank, address, argument);
        }

        /// <summary>
        /// Write a ushort to a variable.
        /// </summary>
        private void AssignValue16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsUshort(ctx, out MemoryBank bank, out ushort address, out ushort argument);

            WriteVariableUshort(ctx, bank, address, argument);
        }

        /// <summary>
        /// Set one bit of a byte variable. A bit index above 7 is logged and the variable left unchanged.
        /// </summary>
        private void SetBitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsByte(ctx, out MemoryBank bank, out ushort address, out byte bitIndex);

            if (bitIndex > 7)
            {
                Debug.LogError($"{nameof(FieldVM)}::{nameof(SetBitOpcodeHandler)} Bit index [{bitIndex}] is out of range for a byte at [{bank}:{address}]");
                return;
            }

            byte current = ReadVariableByte(ctx, bank, address);
            byte result  = (byte)(current | (1 << bitIndex));

            WriteVariableByte(ctx, bank, address, result);
        }

        /// <summary>
        /// Clear one bit of a byte variable. A bit index above 7 is logged and the variable left unchanged.
        /// </summary>
        private void UnsetBitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsByte(ctx, out MemoryBank bank, out ushort address, out byte bitIndex);

            if (bitIndex > 7)
            {
                Debug.LogError($"{nameof(FieldVM)}::{nameof(UnsetBitOpcodeHandler)} Bit index [{bitIndex}] is out of range for a byte at [{bank}:{address}]");
                return;
            }

            byte current = ReadVariableByte(ctx, bank, address);
            byte result  = (byte)(current & ~(1 << bitIndex));

            WriteVariableByte(ctx, bank, address, result);
        }

        /// <summary>
        /// Add to a byte variable, wrapping on overflow.
        /// </summary>
        private void Addition8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsByte(ctx, out MemoryBank bank, out ushort address, out byte argument);

            byte current = ReadVariableByte(ctx, bank, address);
            byte result  = (byte)(current + argument);

            WriteVariableByte(ctx, bank, address, result);
        }

        /// <summary>
        /// Add to a ushort variable, wrapping on overflow.
        /// </summary>
        private void Addition16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsUshort(ctx, out MemoryBank bank, out ushort address, out ushort argument);

            ushort current = ReadVariableUshort(ctx, bank, address);
            ushort result  = (ushort)(current + argument);

            WriteVariableUshort(ctx, bank, address, result);
        }

        /// <summary>
        /// Add to a byte variable, stopping at 255.
        /// </summary>
        private void Addition8BitClampedOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsByte(ctx, out MemoryBank bank, out ushort address, out byte argument);

            int  current = ReadVariableByte(ctx, bank, address);
            byte result  = (byte)Math.Min(current + argument, byte.MaxValue);

            WriteVariableByte(ctx, bank, address, result);
        }

        /// <summary>
        /// Add to a ushort variable, stopping at 65535.
        /// </summary>
        private void Addition16BitClampedOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsUshort(ctx, out MemoryBank bank, out ushort address, out ushort argument);

            int    current = ReadVariableUshort(ctx, bank, address);
            ushort result  = (ushort)Math.Min(current + argument, ushort.MaxValue);

            WriteVariableUshort(ctx, bank, address, result);
        }

        /// <summary>
        /// Subtract from a byte variable, wrapping below zero.
        /// </summary>
        private void Subtraction8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsByte(ctx, out MemoryBank bank, out ushort address, out byte argument);

            byte current = ReadVariableByte(ctx, bank, address);
            byte result  = (byte)(current - argument);

            WriteVariableByte(ctx, bank, address, result);
        }

        /// <summary>
        /// Subtract from a ushort variable, wrapping below zero.
        /// </summary>
        private void Subtraction16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsUshort(ctx, out MemoryBank bank, out ushort address, out ushort argument);

            ushort current = ReadVariableUshort(ctx, bank, address);
            ushort result  = (ushort)(current - argument);

            WriteVariableUshort(ctx, bank, address, result);
        }

        /// <summary>
        /// Subtract from a byte variable, stopping at 0.
        /// </summary>
        private void Subtraction8BitClampedOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsByte(ctx, out MemoryBank bank, out ushort address, out byte argument);

            int  current = ReadVariableByte(ctx, bank, address);
            byte result  = (byte)Math.Max(current - argument, 0);

            WriteVariableByte(ctx, bank, address, result);
        }

        /// <summary>
        /// Subtract from a ushort variable, stopping at 0.
        /// </summary>
        private void Subtraction16BitClampedOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsUshort(ctx, out MemoryBank bank, out ushort address, out ushort argument);

            int    current = ReadVariableUshort(ctx, bank, address);
            ushort result  = (ushort)Math.Max(current - argument, 0);

            WriteVariableUshort(ctx, bank, address, result);
        }

        /// <summary>
        /// Multiply a byte variable, wrapping on overflow.
        /// </summary>
        private void Multiplication8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsByte(ctx, out MemoryBank bank, out ushort address, out byte argument);

            byte current = ReadVariableByte(ctx, bank, address);
            byte result  = (byte)(current * argument);

            WriteVariableByte(ctx, bank, address, result);
        }

        /// <summary>
        /// Multiply a ushort variable, wrapping on overflow.
        /// </summary>
        private void Multiplication16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsUshort(ctx, out MemoryBank bank, out ushort address, out ushort argument);

            ushort current = ReadVariableUshort(ctx, bank, address);
            ushort result  = (ushort)(current * argument);

            WriteVariableUshort(ctx, bank, address, result);
        }

        /// <summary>
        /// Divide a byte variable. Dividing by zero is logged and the variable left unchanged.
        /// </summary>
        private void Division8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsByte(ctx, out MemoryBank bank, out ushort address, out byte argument);

            if (argument == 0)
            {
                Debug.LogError($"{nameof(FieldVM)}::{nameof(Division8BitOpcodeHandler)} Divide by zero at [{bank}:{address}], leaving the value unchanged");
                return;
            }

            byte current = ReadVariableByte(ctx, bank, address);
            byte result  = (byte)(current / argument);

            WriteVariableByte(ctx, bank, address, result);
        }

        /// <summary>
        /// Divide a ushort variable. Dividing by zero is logged and the variable left unchanged.
        /// </summary>
        private void Division16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsUshort(ctx, out MemoryBank bank, out ushort address, out ushort argument);

            if (argument == 0)
            {
                Debug.LogError($"{nameof(FieldVM)}::{nameof(Division16BitOpcodeHandler)} Divide by zero at [{bank}:{address}], leaving the value unchanged");
                return;
            }

            ushort current = ReadVariableUshort(ctx, bank, address);
            ushort result  = (ushort)(current / argument);

            WriteVariableUshort(ctx, bank, address, result);
        }

        /// <summary>
        /// Replace a byte variable with its remainder. A zero divisor is logged and the variable left unchanged.
        /// </summary>
        private void Remainder8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsByte(ctx, out MemoryBank bank, out ushort address, out byte argument);

            if (argument == 0)
            {
                Debug.LogError($"{nameof(FieldVM)}::{nameof(Remainder8BitOpcodeHandler)} Modulo by zero at [{bank}:{address}], leaving the value unchanged");
                return;
            }

            byte current = ReadVariableByte(ctx, bank, address);
            byte result  = (byte)(current % argument);

            WriteVariableByte(ctx, bank, address, result);
        }

        /// <summary>
        /// Replace a ushort variable with its remainder. A zero divisor is logged and the variable left unchanged.
        /// </summary>
        private void Remainder16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsUshort(ctx, out MemoryBank bank, out ushort address, out ushort argument);

            if (argument == 0)
            {
                Debug.LogError($"{nameof(FieldVM)}::{nameof(Remainder16BitOpcodeHandler)} Modulo by zero at [{bank}:{address}], leaving the value unchanged");
                return;
            }

            ushort current = ReadVariableUshort(ctx, bank, address);
            ushort result  = (ushort)(current % argument);

            WriteVariableUshort(ctx, bank, address, result);
        }

        /// <summary>
        /// Bitwise AND a byte variable with the argument.
        /// </summary>
        private void BitwiseAnd8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsByte(ctx, out MemoryBank bank, out ushort address, out byte argument);

            byte current = ReadVariableByte(ctx, bank, address);
            byte result  = (byte)(current & argument);

            WriteVariableByte(ctx, bank, address, result);
        }

        /// <summary>
        /// Bitwise AND a ushort variable with the argument.
        /// </summary>
        private void BitwiseAnd16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsUshort(ctx, out MemoryBank bank, out ushort address, out ushort argument);

            ushort current = ReadVariableUshort(ctx, bank, address);
            ushort result  = (ushort)(current & argument);

            WriteVariableUshort(ctx, bank, address, result);
        }

        /// <summary>
        /// Bitwise OR a byte variable with the argument.
        /// </summary>
        private void BitwiseOr8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsByte(ctx, out MemoryBank bank, out ushort address, out byte argument);

            byte current = ReadVariableByte(ctx, bank, address);
            byte result  = (byte)(current | argument);

            WriteVariableByte(ctx, bank, address, result);
        }

        /// <summary>
        /// Bitwise OR a ushort variable with the argument.
        /// </summary>
        private void BitwiseOr16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsUshort(ctx, out MemoryBank bank, out ushort address, out ushort argument);

            ushort current = ReadVariableUshort(ctx, bank, address);
            ushort result  = (ushort)(current | argument);

            WriteVariableUshort(ctx, bank, address, result);
        }

        /// <summary>
        /// Bitwise XOR a byte variable with the argument.
        /// </summary>
        private void BitwiseXor8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsByte(ctx, out MemoryBank bank, out ushort address, out byte argument);

            byte current = ReadVariableByte(ctx, bank, address);
            byte result  = (byte)(current ^ argument);

            WriteVariableByte(ctx, bank, address, result);
        }

        /// <summary>
        /// Bitwise XOR a ushort variable with the argument.
        /// </summary>
        private void BitwiseXor16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryArgumentsUshort(ctx, out MemoryBank bank, out ushort address, out ushort argument);

            ushort current = ReadVariableUshort(ctx, bank, address);
            ushort result  = (ushort)(current ^ argument);

            WriteVariableUshort(ctx, bank, address, result);
        }

        /// <summary>
        /// Add one to a byte variable, wrapping on overflow.
        /// </summary>
        private void Increment8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            byte current = ReadVariableByte(ctx, bank, address);
            byte result  = (byte)(current + 1);

            WriteVariableByte(ctx, bank, address, result);
        }

        /// <summary>
        /// Add one to a ushort variable, wrapping on overflow.
        /// </summary>
        private void Increment16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            ushort current = ReadVariableUshort(ctx, bank, address);
            ushort result  = (ushort)(current + 1);

            WriteVariableUshort(ctx, bank, address, result);
        }

        /// <summary>
        /// Add one to a byte variable, stopping at 255.
        /// </summary>
        private void Increment8BitClampedOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            byte current = ReadVariableByte(ctx, bank, address);
            byte result  = current == byte.MaxValue ? current : (byte)(current + 1);

            WriteVariableByte(ctx, bank, address, result);
        }

        /// <summary>
        /// Add one to a ushort variable, stopping at 65535.
        /// </summary>
        private void Increment16BitClampedOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            ushort current = ReadVariableUshort(ctx, bank, address);
            ushort result  = current == ushort.MaxValue ? current : (ushort)(current + 1);

            WriteVariableUshort(ctx, bank, address, result);
        }

        /// <summary>
        /// Subtract one from a byte variable, wrapping below zero.
        /// </summary>
        private void Decrement8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            byte current = ReadVariableByte(ctx, bank, address);
            byte result  = (byte)(current - 1);

            WriteVariableByte(ctx, bank, address, result);
        }

        /// <summary>
        /// Subtract one from a ushort variable, wrapping below zero.
        /// </summary>
        private void Decrement16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            ushort current = ReadVariableUshort(ctx, bank, address);
            ushort result  = (ushort)(current - 1);

            WriteVariableUshort(ctx, bank, address, result);
        }

        /// <summary>
        /// Subtract one from a byte variable, stopping at 0.
        /// </summary>
        private void Decrement8BitClampedOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            byte current = ReadVariableByte(ctx, bank, address);
            byte result  = current == byte.MinValue ? current : (byte)(current - 1);

            WriteVariableByte(ctx, bank, address, result);
        }

        /// <summary>
        /// Subtract one from a ushort variable, stopping at 0.
        /// </summary>
        private void Decrement16BitClampedOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            ushort current = ReadVariableUshort(ctx, bank, address);
            ushort result  = current == ushort.MinValue ? current : (ushort)(current - 1);

            WriteVariableUshort(ctx, bank, address, result);
        }

        /// <summary>
        /// Write a random byte, 0 to 255, to the destination.
        /// </summary>
        private void GetRandomNumberOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            byte result = (byte)m_Random.Next(0, byte.MaxValue + 1);

            WriteVariableByte(ctx, bank, address, result);
        }

        /// <summary>
        /// Reseed the VM's random sequence, so a script can be made deterministic.
        /// </summary>
        private void RandomNumberSeedOpcodeHandler(ScriptExecutionContext ctx)
        {
            byte sources = ReadByte(ctx);
            int  seed    = ReadArgumentInt(ctx, GetSecondArgumentSource(sources));

            m_Random = new System.Random(seed);
        }

        /// <summary>
        /// Write a bool to a variable.
        /// </summary>
        private void AssignValueBoolOpcodeHandler(ScriptExecutionContext ctx)
        {
            byte sources = ReadByte(ctx);

            MemoryBank bank    = ToMemoryBank(GetFirstArgumentSource(sources));
            ushort     address = ReadUshort(ctx);
            bool       value   = ReadArgumentBool(ctx, GetSecondArgumentSource(sources));

            WriteVariableBool(ctx, bank, address, value);
        }

        /// <summary>
        /// Show a dialogue window and block until it closes.
        /// </summary>
        private void ShowDialogueWindowOpcodeHandler(ScriptExecutionContext ctx)
        {
            SequentialSources sources       = default;
            ulong             dialogueId    = ReadUlong(ctx);
            bool              blockMovement = ReadArgumentBool(ctx, ref sources);

            RequestShowDialogueWindow?.Invoke(dialogueId, blockMovement);

            ctx.Block(new WaitUntilBlock(() => !IsDialogueWindowOpen(dialogueId)));
        }

        /// <summary>
        /// Ask the player to pick one of several answers and block until they have. The listener stores the
        /// answer through the callback, so a temp destination is this script's own.
        /// </summary>
        private void AskPlayerToMakeAChoiceOpcodeHandler(ScriptExecutionContext ctx)
        {
            byte    bank                 = ReadByte(ctx);
            ushort  addressToStoreChoice = ReadUshort(ctx);
            ulong   dialogueId           = ReadUlong(ctx);
            byte    answerCount          = ReadByte(ctx);
            ulong[] answerIds            = new ulong[answerCount];

            for (int i = 0; i < answerCount; i++)
            {
                answerIds[i] = ReadUlong(ctx);
            }

            MemoryBank choiceBank = (MemoryBank)bank;

            RequestAskPlayerToMakeAChoice?.Invoke(dialogueId, answerIds, choice => WriteVariableByte(ctx, choiceBank, addressToStoreChoice, choice));

            ctx.Block(new WaitUntilBlock(() => !IsPlayerMakingAChoice(dialogueId)));
        }

        /// <summary>
        /// Allow or block opening the main menu.
        /// </summary>
        private void MainMenuAccessibilityOpcodeHandler(ScriptExecutionContext ctx)
        {
            SequentialSources sources = default;
            bool              enabled = ReadArgumentBool(ctx, ref sources);
            RequestSetMainMenuAccessibility?.Invoke(enabled);
        }

        /// <summary>
        /// Create a dialogue window at a position and size, without showing it.
        /// </summary>
        private void CreateDialogueWindowOpcodeHandler(ScriptExecutionContext ctx)
        {
            SequentialSources sources    = default;
            ulong             dialogueId = ReadUlong(ctx);
            int               x          = ReadArgumentInt(ctx, ref sources);
            int               y          = ReadArgumentInt(ctx, ref sources);
            int               width      = ReadArgumentInt(ctx, ref sources);
            int               height     = ReadArgumentInt(ctx, ref sources);
            RectInt           rect       = new RectInt(x, y, width, height);
            RequestCreateDialogueWindow?.Invoke(new DialogueWindowArgs(dialogueId, rect));
        }

        /// <summary>
        /// Lock or unlock player input.
        /// </summary>
        private void LockInputOpcodeHandler(ScriptExecutionContext ctx)
        {
            SequentialSources sources     = default;
            bool              inputLocked = ReadArgumentBool(ctx, ref sources);
            RequestInputLock?.Invoke(inputLocked);
        }

        /// <summary>
        /// Turn this entity's interaction trigger on or off.
        /// </summary>
        private void InteractabilityOpcodeHandler(ScriptExecutionContext ctx)
        {
            SequentialSources sources = default;
            bool              enabled = ReadArgumentBool(ctx, ref sources);
            RequestSetInteractionTriggerActive?.Invoke(ctx.EntityId, enabled);
        }

        /// <summary>
        /// Make this entity the player character.
        /// </summary>
        private void InitAsCharacterOpcodeHandler(ScriptExecutionContext ctx)
        {
            RequestSetPlayerEntity?.Invoke(m_Entities[ctx.EntityId]);
        }

        /// <summary>
        /// Show or hide this entity.
        /// </summary>
        private void VisibilityOpcodeHandler(ScriptExecutionContext ctx)
        {
            SequentialSources sources   = default;
            bool              isVisible = ReadArgumentBool(ctx, ref sources);
            RequestSetEntityVisible?.Invoke(ctx.EntityId, isVisible);
        }

        /// <summary>
        /// Place this entity at a position immediately.
        /// </summary>
        private void SetEntityPositionOpcodeHandler(ScriptExecutionContext ctx)
        {
            SequentialSources sources  = default;
            Vector3           position = new Vector3(ReadArgumentFloat(ctx, ref sources), ReadArgumentFloat(ctx, ref sources), ReadArgumentFloat(ctx, ref sources));
            RequestSetEntityPosition?.Invoke(ctx.EntityId, position);
        }

        /// <summary>
        /// Set how fast this entity moves.
        /// </summary>
        private void SetMovementSpeedOpcodeHandler(ScriptExecutionContext ctx)
        {
            SequentialSources sources       = default;
            float             movementSpeed = ReadArgumentFloat(ctx, ref sources);
            RequestSetEntityMovementSpeed?.Invoke(ctx.EntityId, movementSpeed);
        }

        /// <summary>
        /// Set this entity's rotation immediately, from Euler angles.
        /// </summary>
        private void SetEntityRotationOpcodeHandler(ScriptExecutionContext ctx)
        {
            SequentialSources sources = default;
            float             x       = ReadArgumentFloat(ctx, ref sources);
            float             y       = ReadArgumentFloat(ctx, ref sources);
            float             z       = ReadArgumentFloat(ctx, ref sources);

            RequestSetEntityRotation?.Invoke(ctx.EntityId, Quaternion.Euler(x, y, z));
        }

        /// <summary>
        /// Turn this entity to a rotation over time and block until it finishes.
        /// </summary>
        private void SetEntityRotationAsyncOpcodeHandler(ScriptExecutionContext ctx)
        {
            SequentialSources     sources      = default;
            float                 x            = ReadArgumentFloat(ctx, ref sources);
            float                 y            = ReadArgumentFloat(ctx, ref sources);
            float                 z            = ReadArgumentFloat(ctx, ref sources);
            RotationDirection     direction    = (RotationDirection)ReadArgumentByte(ctx, ref sources);
            float                 duration     = ReadArgumentFloat(ctx, ref sources);
            RotationInterpolation rotationType = (RotationInterpolation)ReadArgumentByte(ctx, ref sources);

            SetEntityRotationAsyncArgs args = new SetEntityRotationAsyncArgs(Quaternion.Euler(x, y, z), direction, duration, rotationType);

            RequestSetEntityRotationAsync?.Invoke(ctx.EntityId, args);

            ctx.Block(new WaitUntilBlock(() => !IsEntityRotating(ctx.EntityId)));
        }

        /// <summary>
        /// Turn this entity to face another entity.
        /// </summary>
        private void SetDirectionToFaceEntityOpcodeHandler(ScriptExecutionContext ctx)
        {
            SequentialSources sources        = default;
            byte              targetEntityId = ReadArgumentByte(ctx, ref sources);
            RequestSetEntityToFaceEntity?.Invoke(ctx.EntityId, targetEntityId);
        }

        /// <summary>
        /// Set the radius around this entity within which the player can interact with it.
        /// </summary>
        private void SetInteractionRangeOpcodeHandler(ScriptExecutionContext ctx)
        {
            SequentialSources sources = default;
            float             radius  = ReadArgumentFloat(ctx, ref sources);
            RequestSetInteractionRange?.Invoke(ctx.EntityId, radius);
        }

        /// <summary>
        /// Play a music track, starting in one of its stem states.
        /// </summary>
        private void PlayMusicOpcodeHandler(ScriptExecutionContext ctx)
        {
            ulong nameHash      = ReadUlong(ctx);
            ulong stateNameHash = ReadUlong(ctx);

            RequestMusic?.Invoke(nameHash, stateNameHash);
        }

        /// <summary>
        /// Play a sound effect once.
        /// </summary>
        private void PlaySoundOpcodeHandler(ScriptExecutionContext ctx)
        {
            ulong nameHash = ReadUlong(ctx);
            RequestSfx?.Invoke(nameHash);
        }

        /// <summary>
        /// Switch the playing music to one of its stem states, fading over the given number of seconds.
        /// </summary>
        private void SetMusicStemStateOpcodeHandler(ScriptExecutionContext ctx)
        {
            SequentialSources sources       = default;
            ulong             stateNameHash = ReadUlong(ctx);
            float             fadeSeconds   = ReadArgumentFloat(ctx, ref sources);

            RequestMusicStemState?.Invoke(stateNameHash, fadeSeconds);
        }
    }
}