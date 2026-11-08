using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using RPGFramework.Battle.SharedTypes;
using RPGFramework.Core;
using RPGFramework.Core.Dialogue;
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
        internal event Action<ulong, ulong, float, float>      RequestMusicCrossfade;
        internal event Action                                  RequestStopMusic;
        internal event Action<int>                             RequestAddMoney;
        internal Func<bool>                                    IsMusicPlaying;
        internal event Action<float, float>                    RequestMusicVolume;
        internal event Action<float, float, float>             RequestMusicVolumeFrom;
        internal Func<bool>                                    IsMusicVolumeFading;
        internal event Action<ulong, float>                    RequestMusicStemState;
        internal event Action<ulong>                           RequestBattleMusic;
        internal event Action<ulong, float, float, byte>       RequestSfx;
        internal event Action<byte>                            RequestStopSound;
        internal event Action<byte, float, float>              RequestFadeSoundVolume;
        internal event Action<byte, float, float>              RequestFadeSoundPan;
        internal event Action<float, float>                    RequestFadeAllSoundVolume;
        internal event Action<byte>                            RequestReverbPreset;
        internal event Action<float>                           RequestReverbVolume;
        internal event Action<FieldEntityRuntime>              RequestSetPlayerEntity;
        internal event Action<int, bool>                       RequestSetEntityVisible;
        internal event Action<int, bool>                       RequestSetEntitySolid;
        internal event Action<int, bool>                       RequestSetPushScriptActive;
        internal event Action<bool>                            RequestSetGatewayTriggersActive;
        internal event Action<int, bool>                       RequestSetInteractionTriggerActive;
        internal event Action<int, bool>                       RequestSetCollisionTriggerActive;
        internal event Action<byte, bool>                      RequestSetBlockerActive;
        internal event Action<bool>                            RequestFieldExitFade;
        internal event Action<bool, bool, ulong>               RequestScreenFade;
        internal Func<bool>                                    IsScreenFading;
        internal event Action<Color, bool, float>              RequestScreenColour;
        internal Func<bool>                                    IsScreenColourFading;
        internal event Action<Vector3, float, Easing>          RequestScrollScreenToPosition;
        internal event Action<int, float, Easing>              RequestScrollScreenToEntity;
        internal Func<bool>                                    IsScreenScrolling;
        internal event Action<byte, float, Easing>             RequestSetCamera;
        internal event Action<int, float>                      RequestSetInteractionRange;
        internal event Action<bool>                            RequestInputLock;
        internal event Action<int, Vector3>                    RequestSetEntityPosition;
        internal event Action<int, Quaternion>                 RequestSetEntityRotation;
        internal event Action<int, SetEntityRotationAsyncArgs> RequestSetEntityRotationAsync;
        internal Func<int, bool>                               IsEntityRotating;
        internal event Action<int, int>                        RequestSetEntityToFaceEntity;
        internal event Action<int, Vector3>                    RequestSetEntityToFacePosition;
        internal event Action<int, int, float, Easing>         RequestTurnEntityToEntity;
        internal event Action<int, int>                        RequestCopyEntityPose;
        internal event Action<int, bool>                       RequestSetEntityInField;
        internal Func<int, Vector3>                            GetEntityPosition;
        internal Func<int, float>                              GetEntityDirection;
        internal Func<int, int, bool>                          IsEntityInRange;
        internal event Action<int, Vector3, float, Easing>     RequestSetEntityDrawOffset;
        internal Func<int, bool>                               IsEntityDrawOffsetMoving;
        internal event Action<int, Vector3, float, float>      RequestJumpEntity;
        internal Func<int, bool>                               IsEntityJumping;
        internal event Action<int, LadderClimbArgs>            RequestClimbLadder;
        internal Func<int, bool>                               IsEntityClimbing;
        internal event Action<int, ulong, ulong, ulong>        RequestSetLadderAnimations;
        internal event Action<int, float>                      RequestSetEntityMovementSpeed;
        internal event Action<int, MoveEntityArgs>             RequestMoveEntity;
        internal Func<int, bool>                               IsEntityMoving;
        internal event Action<int>                             RequestStopMovement;
        internal event Action<int, ulong>                      RequestSetBaseAnimation;
        internal event Action<int, PlayAnimationArgs>          RequestPlayAnimation;
        internal Func<int, bool>                               IsEntityAnimating;
        internal event Action<int>                             RequestPushAnimationState;
        internal event Action<int>                             RequestPopAnimationState;
        internal event Action<int, float>                      RequestSetAnimationSpeed;
        internal event Action<int>                             RequestStopAnimation;
        internal event Action<bool>                            RequestSetMainMenuAccessibility;
        internal event Action<bool>                            RequestSetSaveEnabled;
        internal event Action                                  RequestOpenSaveMenu;
        internal event Action<DialogueWindowArgs>              RequestCreateDialogueWindow;
        internal event Action<byte, Vector2Int>                RequestFitDialogueWindow;
        internal event Action<byte, ulong, bool>               RequestShowDialogueWindow;
        internal event Action<byte, ulong, bool>               RequestShowDialogueWindowNoWait;
        internal event Action<byte>                            RequestCloseDialogueWindow;
        internal event Action<byte>                            RequestWaitForDialogueWindow;
        internal Func<byte, bool>                              IsDialogueWindowOpen;
        internal event Action<QuestionArgs, Action<byte>>      RequestAskPlayerToMakeAChoice;
        internal event Action<byte, DialogueWindowStyle>       RequestSetDialogueWindowStyle;
        internal event Action<byte, int>                       RequestSetMessageVariable;
        internal event Action<BattleFlags>                     RequestSetBattleMode;
        internal event Action<BattleArgs>                      RequestStartBattle;
        internal Func<BattleCompleteState>                     GetLastBattleResult;
        internal event Action                                  RequestGameOver;
        internal event Action<int>                             RequestSetCountdown;
        internal event Action<int, int>                        RequestShowCountdown;
        internal event Action                                  RequestHideCountdown;
        internal Func<int>                                     GetCountdown;
        internal Func<ulong, Func<bool>>                       StartWaitingForButton;

        private readonly Dictionary<SlotKey, FieldScriptContext> m_Contexts;
        private readonly Dictionary<int, FieldEntityRuntime>     m_Entities;
        private readonly Dictionary<int, byte[]>                 m_Scripts;
        private readonly FieldOpCodeHandler[]                    m_OpcodeHandlers;
        private readonly IMemoryService                          m_MemoryService;
        private readonly int                                     m_TempBytes;

        private System.Random m_Random;

        internal FieldVM(IMemoryService memoryService, int tempBytes, IFieldGameOpCodeDatabase gameOpCodes)
        {
            m_Contexts = new Dictionary<SlotKey, FieldScriptContext>();
            m_Entities = new Dictionary<int, FieldEntityRuntime>();
            m_Scripts  = new Dictionary<int, byte[]>();

            m_MemoryService  = memoryService;
            m_TempBytes      = tempBytes;
            m_Random         = new System.Random();
            m_OpcodeHandlers = BuildOpcodeHandlers(gameOpCodes.Handlers);
        }

        /// <summary>
        /// Drops the field's scripts, its entities and the scripts running on them, as the field is left for another,
        /// keeping the table of handlers and the random numbers: the next field registers its own into the same VM.
        /// </summary>
        internal void Reset()
        {
            m_Contexts.Clear();
            m_Entities.Clear();
            m_Scripts.Clear();
        }

        private FieldOpCodeHandler[] BuildOpcodeHandlers(IReadOnlyDictionary<ushort, FieldOpCodeHandler> game)
        {
            Dictionary<FieldScriptOpCode, FieldOpCodeHandler> engine = EngineHandlers();

            int length = 0;

            foreach (FieldScriptOpCode opCode in engine.Keys)
            {
                length = Math.Max(length, (ushort)opCode + 1);
            }

            foreach (ushort opCode in game.Keys)
            {
                length = Math.Max(length, opCode + 1);
            }

            FieldOpCodeHandler[] handlers = new FieldOpCodeHandler[length];

            foreach (KeyValuePair<FieldScriptOpCode, FieldOpCodeHandler> handler in engine)
            {
                handlers[(ushort)handler.Key] = handler.Value;
            }

            foreach (KeyValuePair<ushort, FieldOpCodeHandler> handler in game)
            {
                handlers[handler.Key] = handler.Value;
            }

            return handlers;
        }

        private Dictionary<FieldScriptOpCode, FieldOpCodeHandler> EngineHandlers()
        {
            return new Dictionary<FieldScriptOpCode, FieldOpCodeHandler>
                   {
                       // Script flow and control
                       { FieldScriptOpCode.Return, ReturnOpcodeHandler },
                       { FieldScriptOpCode.RunAnotherEntityScriptUnlessBusy, RunAnotherEntityScriptUnlessBusyOpcodeHandler },
                       { FieldScriptOpCode.RunAnotherEntityScriptWaitUntilStarted, RunAnotherEntityScriptWaitUntilStartedOpcodeHandler },
                       { FieldScriptOpCode.RunAnotherEntityScriptWaitUntilFinished, RunAnotherEntityScriptWaitUntilFinishedOpcodeHandler },
                       // { FieldScriptOpCode.RunPartyMemberScriptUnlessBusy, RunPartyMemberScriptUnlessBusyOpcodeHandler },
                       // { FieldScriptOpCode.RunPartyMemberScriptWaitUntilStarted, RunPartyMemberScriptWaitUntilStartedOpcodeHandler },
                       // { FieldScriptOpCode.RunPartyMemberScriptWaitUntilFinished, RunPartyMemberScriptWaitUntilFinishedOpcodeHandler },
                       { FieldScriptOpCode.ReturnToAnotherScript, ReturnToAnotherScriptOpcodeHandler },
                       { FieldScriptOpCode.CallAnotherScript, CallAnotherScriptOpcodeHandler },
                       { FieldScriptOpCode.ReturnFromCall, ReturnFromCallOpcodeHandler },
                       { FieldScriptOpCode.GotoJump, GotoOpcodeHandler },
                       { FieldScriptOpCode.GotoDirectly, GotoDirectlyOpcodeHandler },
                       { FieldScriptOpCode.CompareBool, ctx => CompareOpcodeHandler(ctx,   VariableWidth.Bool) },
                       { FieldScriptOpCode.CompareSByte, ctx => CompareOpcodeHandler(ctx,  VariableWidth.SByte) },
                       { FieldScriptOpCode.CompareByte, ctx => CompareOpcodeHandler(ctx,   VariableWidth.Byte) },
                       { FieldScriptOpCode.CompareShort, ctx => CompareOpcodeHandler(ctx,  VariableWidth.Short) },
                       { FieldScriptOpCode.CompareUShort, ctx => CompareOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.CompareInt, ctx => CompareOpcodeHandler(ctx,    VariableWidth.Int) },
                       { FieldScriptOpCode.CompareUInt, ctx => CompareOpcodeHandler(ctx,   VariableWidth.UInt) },
                       { FieldScriptOpCode.CompareLong, ctx => CompareOpcodeHandler(ctx,   VariableWidth.Long) },
                       { FieldScriptOpCode.CompareULong, ctx => CompareOpcodeHandler(ctx,  VariableWidth.ULong) },
                       { FieldScriptOpCode.CompareFloat, ctx => CompareOpcodeHandler(ctx,  VariableWidth.Float) },
                       { FieldScriptOpCode.Yield, YieldOpcodeHandler },
                       { FieldScriptOpCode.WaitSeconds, WaitSecondsOpcodeHandler },
                       { FieldScriptOpCode.DoNothing, DoNothingOpcodeHandler },
                       { FieldScriptOpCode.ReturnToPriority, ReturnToPriorityOpcodeHandler },

                       // System and module control
                       { FieldScriptOpCode.JumpToAnotherMap, JumpToAnotherMapOpcodeHandler },
                       { FieldScriptOpCode.GatewayTriggerActivation, GatewayTriggerActivationOpcodeHandler },
                       { FieldScriptOpCode.SetFieldExitFade, SetFieldExitFadeOpcodeHandler },
                       // { FieldScriptOpCode.WorldMapJump, WorldMapJumpOpcodeHandler },
                       { FieldScriptOpCode.SetBattleMode, SetBattleModeOpcodeHandler },
                       { FieldScriptOpCode.StartBattle, StartBattleOpcodeHandler },
                       { FieldScriptOpCode.GetLastBattleResult, GetLastBattleResultOpcodeHandler },
                       // { FieldScriptOpCode.RandomEncounters, RandomEncountersOpcodeHandler },
                       { FieldScriptOpCode.GameOver, GameOverOpcodeHandler },
                       { FieldScriptOpCode.SaveAccessibility, SaveAccessibilityOpcodeHandler },

                       // Assignment and mathematics
                       { FieldScriptOpCode.SetBool, ctx => SetOpcodeHandler(ctx,   VariableWidth.Bool) },
                       { FieldScriptOpCode.SetSByte, ctx => SetOpcodeHandler(ctx,  VariableWidth.SByte) },
                       { FieldScriptOpCode.SetByte, ctx => SetOpcodeHandler(ctx,   VariableWidth.Byte) },
                       { FieldScriptOpCode.SetShort, ctx => SetOpcodeHandler(ctx,  VariableWidth.Short) },
                       { FieldScriptOpCode.SetUShort, ctx => SetOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.SetInt, ctx => SetOpcodeHandler(ctx,    VariableWidth.Int) },
                       { FieldScriptOpCode.SetUInt, ctx => SetOpcodeHandler(ctx,   VariableWidth.UInt) },
                       { FieldScriptOpCode.SetLong, ctx => SetOpcodeHandler(ctx,   VariableWidth.Long) },
                       { FieldScriptOpCode.SetULong, ctx => SetOpcodeHandler(ctx,  VariableWidth.ULong) },
                       { FieldScriptOpCode.SetFloat, ctx => SetOpcodeHandler(ctx,  VariableWidth.Float) },
                       { FieldScriptOpCode.AddSByte, ctx => AddOpcodeHandler(ctx,  VariableWidth.SByte) },
                       { FieldScriptOpCode.AddByte, ctx => AddOpcodeHandler(ctx,   VariableWidth.Byte) },
                       { FieldScriptOpCode.AddShort, ctx => AddOpcodeHandler(ctx,  VariableWidth.Short) },
                       { FieldScriptOpCode.AddUShort, ctx => AddOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.AddInt, ctx => AddOpcodeHandler(ctx,    VariableWidth.Int) },
                       { FieldScriptOpCode.AddUInt, ctx => AddOpcodeHandler(ctx,   VariableWidth.UInt) },
                       { FieldScriptOpCode.AddLong, ctx => AddOpcodeHandler(ctx,   VariableWidth.Long) },
                       { FieldScriptOpCode.AddULong, ctx => AddOpcodeHandler(ctx,  VariableWidth.ULong) },
                       { FieldScriptOpCode.AddFloat, ctx => AddOpcodeHandler(ctx,  VariableWidth.Float) },
                       { FieldScriptOpCode.AddSByteClamped, ctx => AddClampedOpcodeHandler(ctx,  VariableWidth.SByte) },
                       { FieldScriptOpCode.AddByteClamped, ctx => AddClampedOpcodeHandler(ctx,   VariableWidth.Byte) },
                       { FieldScriptOpCode.AddShortClamped, ctx => AddClampedOpcodeHandler(ctx,  VariableWidth.Short) },
                       { FieldScriptOpCode.AddUShortClamped, ctx => AddClampedOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.AddIntClamped, ctx => AddClampedOpcodeHandler(ctx,    VariableWidth.Int) },
                       { FieldScriptOpCode.AddUIntClamped, ctx => AddClampedOpcodeHandler(ctx,   VariableWidth.UInt) },
                       { FieldScriptOpCode.AddLongClamped, ctx => AddClampedOpcodeHandler(ctx,   VariableWidth.Long) },
                       { FieldScriptOpCode.AddULongClamped, ctx => AddClampedOpcodeHandler(ctx,  VariableWidth.ULong) },
                       { FieldScriptOpCode.SubtractSByte, ctx => SubtractOpcodeHandler(ctx,  VariableWidth.SByte) },
                       { FieldScriptOpCode.SubtractByte, ctx => SubtractOpcodeHandler(ctx,   VariableWidth.Byte) },
                       { FieldScriptOpCode.SubtractShort, ctx => SubtractOpcodeHandler(ctx,  VariableWidth.Short) },
                       { FieldScriptOpCode.SubtractUShort, ctx => SubtractOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.SubtractInt, ctx => SubtractOpcodeHandler(ctx,    VariableWidth.Int) },
                       { FieldScriptOpCode.SubtractUInt, ctx => SubtractOpcodeHandler(ctx,   VariableWidth.UInt) },
                       { FieldScriptOpCode.SubtractLong, ctx => SubtractOpcodeHandler(ctx,   VariableWidth.Long) },
                       { FieldScriptOpCode.SubtractULong, ctx => SubtractOpcodeHandler(ctx,  VariableWidth.ULong) },
                       { FieldScriptOpCode.SubtractFloat, ctx => SubtractOpcodeHandler(ctx,  VariableWidth.Float) },
                       { FieldScriptOpCode.SubtractSByteClamped, ctx => SubtractClampedOpcodeHandler(ctx,  VariableWidth.SByte) },
                       { FieldScriptOpCode.SubtractByteClamped, ctx => SubtractClampedOpcodeHandler(ctx,   VariableWidth.Byte) },
                       { FieldScriptOpCode.SubtractShortClamped, ctx => SubtractClampedOpcodeHandler(ctx,  VariableWidth.Short) },
                       { FieldScriptOpCode.SubtractUShortClamped, ctx => SubtractClampedOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.SubtractIntClamped, ctx => SubtractClampedOpcodeHandler(ctx,    VariableWidth.Int) },
                       { FieldScriptOpCode.SubtractUIntClamped, ctx => SubtractClampedOpcodeHandler(ctx,   VariableWidth.UInt) },
                       { FieldScriptOpCode.SubtractLongClamped, ctx => SubtractClampedOpcodeHandler(ctx,   VariableWidth.Long) },
                       { FieldScriptOpCode.SubtractULongClamped, ctx => SubtractClampedOpcodeHandler(ctx,  VariableWidth.ULong) },
                       { FieldScriptOpCode.MultiplySByte, ctx => MultiplyOpcodeHandler(ctx,  VariableWidth.SByte) },
                       { FieldScriptOpCode.MultiplyByte, ctx => MultiplyOpcodeHandler(ctx,   VariableWidth.Byte) },
                       { FieldScriptOpCode.MultiplyShort, ctx => MultiplyOpcodeHandler(ctx,  VariableWidth.Short) },
                       { FieldScriptOpCode.MultiplyUShort, ctx => MultiplyOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.MultiplyInt, ctx => MultiplyOpcodeHandler(ctx,    VariableWidth.Int) },
                       { FieldScriptOpCode.MultiplyUInt, ctx => MultiplyOpcodeHandler(ctx,   VariableWidth.UInt) },
                       { FieldScriptOpCode.MultiplyLong, ctx => MultiplyOpcodeHandler(ctx,   VariableWidth.Long) },
                       { FieldScriptOpCode.MultiplyULong, ctx => MultiplyOpcodeHandler(ctx,  VariableWidth.ULong) },
                       { FieldScriptOpCode.MultiplyFloat, ctx => MultiplyOpcodeHandler(ctx,  VariableWidth.Float) },
                       { FieldScriptOpCode.DivideSByte, ctx => DivideOpcodeHandler(ctx,  VariableWidth.SByte) },
                       { FieldScriptOpCode.DivideByte, ctx => DivideOpcodeHandler(ctx,   VariableWidth.Byte) },
                       { FieldScriptOpCode.DivideShort, ctx => DivideOpcodeHandler(ctx,  VariableWidth.Short) },
                       { FieldScriptOpCode.DivideUShort, ctx => DivideOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.DivideInt, ctx => DivideOpcodeHandler(ctx,    VariableWidth.Int) },
                       { FieldScriptOpCode.DivideUInt, ctx => DivideOpcodeHandler(ctx,   VariableWidth.UInt) },
                       { FieldScriptOpCode.DivideLong, ctx => DivideOpcodeHandler(ctx,   VariableWidth.Long) },
                       { FieldScriptOpCode.DivideULong, ctx => DivideOpcodeHandler(ctx,  VariableWidth.ULong) },
                       { FieldScriptOpCode.DivideFloat, ctx => DivideOpcodeHandler(ctx,  VariableWidth.Float) },
                       { FieldScriptOpCode.RemainderSByte, ctx => RemainderOpcodeHandler(ctx,  VariableWidth.SByte) },
                       { FieldScriptOpCode.RemainderByte, ctx => RemainderOpcodeHandler(ctx,   VariableWidth.Byte) },
                       { FieldScriptOpCode.RemainderShort, ctx => RemainderOpcodeHandler(ctx,  VariableWidth.Short) },
                       { FieldScriptOpCode.RemainderUShort, ctx => RemainderOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.RemainderInt, ctx => RemainderOpcodeHandler(ctx,    VariableWidth.Int) },
                       { FieldScriptOpCode.RemainderUInt, ctx => RemainderOpcodeHandler(ctx,   VariableWidth.UInt) },
                       { FieldScriptOpCode.RemainderLong, ctx => RemainderOpcodeHandler(ctx,   VariableWidth.Long) },
                       { FieldScriptOpCode.RemainderULong, ctx => RemainderOpcodeHandler(ctx,  VariableWidth.ULong) },
                       { FieldScriptOpCode.IncrementSByte, ctx => IncrementOpcodeHandler(ctx,  VariableWidth.SByte) },
                       { FieldScriptOpCode.IncrementByte, ctx => IncrementOpcodeHandler(ctx,   VariableWidth.Byte) },
                       { FieldScriptOpCode.IncrementShort, ctx => IncrementOpcodeHandler(ctx,  VariableWidth.Short) },
                       { FieldScriptOpCode.IncrementUShort, ctx => IncrementOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.IncrementInt, ctx => IncrementOpcodeHandler(ctx,    VariableWidth.Int) },
                       { FieldScriptOpCode.IncrementUInt, ctx => IncrementOpcodeHandler(ctx,   VariableWidth.UInt) },
                       { FieldScriptOpCode.IncrementLong, ctx => IncrementOpcodeHandler(ctx,   VariableWidth.Long) },
                       { FieldScriptOpCode.IncrementULong, ctx => IncrementOpcodeHandler(ctx,  VariableWidth.ULong) },
                       { FieldScriptOpCode.IncrementSByteClamped, ctx => IncrementClampedOpcodeHandler(ctx,  VariableWidth.SByte) },
                       { FieldScriptOpCode.IncrementByteClamped, ctx => IncrementClampedOpcodeHandler(ctx,   VariableWidth.Byte) },
                       { FieldScriptOpCode.IncrementShortClamped, ctx => IncrementClampedOpcodeHandler(ctx,  VariableWidth.Short) },
                       { FieldScriptOpCode.IncrementUShortClamped, ctx => IncrementClampedOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.IncrementIntClamped, ctx => IncrementClampedOpcodeHandler(ctx,    VariableWidth.Int) },
                       { FieldScriptOpCode.IncrementUIntClamped, ctx => IncrementClampedOpcodeHandler(ctx,   VariableWidth.UInt) },
                       { FieldScriptOpCode.IncrementLongClamped, ctx => IncrementClampedOpcodeHandler(ctx,   VariableWidth.Long) },
                       { FieldScriptOpCode.IncrementULongClamped, ctx => IncrementClampedOpcodeHandler(ctx,  VariableWidth.ULong) },
                       { FieldScriptOpCode.DecrementSByte, ctx => DecrementOpcodeHandler(ctx,  VariableWidth.SByte) },
                       { FieldScriptOpCode.DecrementByte, ctx => DecrementOpcodeHandler(ctx,   VariableWidth.Byte) },
                       { FieldScriptOpCode.DecrementShort, ctx => DecrementOpcodeHandler(ctx,  VariableWidth.Short) },
                       { FieldScriptOpCode.DecrementUShort, ctx => DecrementOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.DecrementInt, ctx => DecrementOpcodeHandler(ctx,    VariableWidth.Int) },
                       { FieldScriptOpCode.DecrementUInt, ctx => DecrementOpcodeHandler(ctx,   VariableWidth.UInt) },
                       { FieldScriptOpCode.DecrementLong, ctx => DecrementOpcodeHandler(ctx,   VariableWidth.Long) },
                       { FieldScriptOpCode.DecrementULong, ctx => DecrementOpcodeHandler(ctx,  VariableWidth.ULong) },
                       { FieldScriptOpCode.DecrementSByteClamped, ctx => DecrementClampedOpcodeHandler(ctx,  VariableWidth.SByte) },
                       { FieldScriptOpCode.DecrementByteClamped, ctx => DecrementClampedOpcodeHandler(ctx,   VariableWidth.Byte) },
                       { FieldScriptOpCode.DecrementShortClamped, ctx => DecrementClampedOpcodeHandler(ctx,  VariableWidth.Short) },
                       { FieldScriptOpCode.DecrementUShortClamped, ctx => DecrementClampedOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.DecrementIntClamped, ctx => DecrementClampedOpcodeHandler(ctx,    VariableWidth.Int) },
                       { FieldScriptOpCode.DecrementUIntClamped, ctx => DecrementClampedOpcodeHandler(ctx,   VariableWidth.UInt) },
                       { FieldScriptOpCode.DecrementLongClamped, ctx => DecrementClampedOpcodeHandler(ctx,   VariableWidth.Long) },
                       { FieldScriptOpCode.DecrementULongClamped, ctx => DecrementClampedOpcodeHandler(ctx,  VariableWidth.ULong) },
                       { FieldScriptOpCode.BitwiseAndSByte, ctx => BitwiseAndOpcodeHandler(ctx,  VariableWidth.SByte) },
                       { FieldScriptOpCode.BitwiseAndByte, ctx => BitwiseAndOpcodeHandler(ctx,   VariableWidth.Byte) },
                       { FieldScriptOpCode.BitwiseAndShort, ctx => BitwiseAndOpcodeHandler(ctx,  VariableWidth.Short) },
                       { FieldScriptOpCode.BitwiseAndUShort, ctx => BitwiseAndOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.BitwiseAndInt, ctx => BitwiseAndOpcodeHandler(ctx,    VariableWidth.Int) },
                       { FieldScriptOpCode.BitwiseAndUInt, ctx => BitwiseAndOpcodeHandler(ctx,   VariableWidth.UInt) },
                       { FieldScriptOpCode.BitwiseAndLong, ctx => BitwiseAndOpcodeHandler(ctx,   VariableWidth.Long) },
                       { FieldScriptOpCode.BitwiseAndULong, ctx => BitwiseAndOpcodeHandler(ctx,  VariableWidth.ULong) },
                       { FieldScriptOpCode.BitwiseOrSByte, ctx => BitwiseOrOpcodeHandler(ctx,  VariableWidth.SByte) },
                       { FieldScriptOpCode.BitwiseOrByte, ctx => BitwiseOrOpcodeHandler(ctx,   VariableWidth.Byte) },
                       { FieldScriptOpCode.BitwiseOrShort, ctx => BitwiseOrOpcodeHandler(ctx,  VariableWidth.Short) },
                       { FieldScriptOpCode.BitwiseOrUShort, ctx => BitwiseOrOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.BitwiseOrInt, ctx => BitwiseOrOpcodeHandler(ctx,    VariableWidth.Int) },
                       { FieldScriptOpCode.BitwiseOrUInt, ctx => BitwiseOrOpcodeHandler(ctx,   VariableWidth.UInt) },
                       { FieldScriptOpCode.BitwiseOrLong, ctx => BitwiseOrOpcodeHandler(ctx,   VariableWidth.Long) },
                       { FieldScriptOpCode.BitwiseOrULong, ctx => BitwiseOrOpcodeHandler(ctx,  VariableWidth.ULong) },
                       { FieldScriptOpCode.BitwiseXorSByte, ctx => BitwiseXorOpcodeHandler(ctx,  VariableWidth.SByte) },
                       { FieldScriptOpCode.BitwiseXorByte, ctx => BitwiseXorOpcodeHandler(ctx,   VariableWidth.Byte) },
                       { FieldScriptOpCode.BitwiseXorShort, ctx => BitwiseXorOpcodeHandler(ctx,  VariableWidth.Short) },
                       { FieldScriptOpCode.BitwiseXorUShort, ctx => BitwiseXorOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.BitwiseXorInt, ctx => BitwiseXorOpcodeHandler(ctx,    VariableWidth.Int) },
                       { FieldScriptOpCode.BitwiseXorUInt, ctx => BitwiseXorOpcodeHandler(ctx,   VariableWidth.UInt) },
                       { FieldScriptOpCode.BitwiseXorLong, ctx => BitwiseXorOpcodeHandler(ctx,   VariableWidth.Long) },
                       { FieldScriptOpCode.BitwiseXorULong, ctx => BitwiseXorOpcodeHandler(ctx,  VariableWidth.ULong) },
                       { FieldScriptOpCode.SetBitSByte, ctx => SetBitOpcodeHandler(ctx,  VariableWidth.SByte) },
                       { FieldScriptOpCode.SetBitByte, ctx => SetBitOpcodeHandler(ctx,   VariableWidth.Byte) },
                       { FieldScriptOpCode.SetBitShort, ctx => SetBitOpcodeHandler(ctx,  VariableWidth.Short) },
                       { FieldScriptOpCode.SetBitUShort, ctx => SetBitOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.SetBitInt, ctx => SetBitOpcodeHandler(ctx,    VariableWidth.Int) },
                       { FieldScriptOpCode.SetBitUInt, ctx => SetBitOpcodeHandler(ctx,   VariableWidth.UInt) },
                       { FieldScriptOpCode.SetBitLong, ctx => SetBitOpcodeHandler(ctx,   VariableWidth.Long) },
                       { FieldScriptOpCode.SetBitULong, ctx => SetBitOpcodeHandler(ctx,  VariableWidth.ULong) },
                       { FieldScriptOpCode.UnsetBitSByte, ctx => UnsetBitOpcodeHandler(ctx,  VariableWidth.SByte) },
                       { FieldScriptOpCode.UnsetBitByte, ctx => UnsetBitOpcodeHandler(ctx,   VariableWidth.Byte) },
                       { FieldScriptOpCode.UnsetBitShort, ctx => UnsetBitOpcodeHandler(ctx,  VariableWidth.Short) },
                       { FieldScriptOpCode.UnsetBitUShort, ctx => UnsetBitOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.UnsetBitInt, ctx => UnsetBitOpcodeHandler(ctx,    VariableWidth.Int) },
                       { FieldScriptOpCode.UnsetBitUInt, ctx => UnsetBitOpcodeHandler(ctx,   VariableWidth.UInt) },
                       { FieldScriptOpCode.UnsetBitLong, ctx => UnsetBitOpcodeHandler(ctx,   VariableWidth.Long) },
                       { FieldScriptOpCode.UnsetBitULong, ctx => UnsetBitOpcodeHandler(ctx,  VariableWidth.ULong) },
                       { FieldScriptOpCode.GetRandomNumber, GetRandomNumberOpcodeHandler },
                       { FieldScriptOpCode.RandomNumberSeed, RandomNumberSeedOpcodeHandler },
                       { FieldScriptOpCode.BitwiseNotSByte, ctx => BitwiseNotOpcodeHandler(ctx,  VariableWidth.SByte) },
                       { FieldScriptOpCode.BitwiseNotByte, ctx => BitwiseNotOpcodeHandler(ctx,   VariableWidth.Byte) },
                       { FieldScriptOpCode.BitwiseNotShort, ctx => BitwiseNotOpcodeHandler(ctx,  VariableWidth.Short) },
                       { FieldScriptOpCode.BitwiseNotUShort, ctx => BitwiseNotOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.BitwiseNotInt, ctx => BitwiseNotOpcodeHandler(ctx,    VariableWidth.Int) },
                       { FieldScriptOpCode.BitwiseNotUInt, ctx => BitwiseNotOpcodeHandler(ctx,   VariableWidth.UInt) },
                       { FieldScriptOpCode.BitwiseNotLong, ctx => BitwiseNotOpcodeHandler(ctx,   VariableWidth.Long) },
                       { FieldScriptOpCode.BitwiseNotULong, ctx => BitwiseNotOpcodeHandler(ctx,  VariableWidth.ULong) },

                       // Windowing and menu
                       { FieldScriptOpCode.CreateDialogueWindow, CreateDialogueWindowOpcodeHandler },
                       { FieldScriptOpCode.ShowDialogueWindow, ShowDialogueWindowOpcodeHandler },
                       { FieldScriptOpCode.ShowDialogueWindowNoWait, ShowDialogueWindowNoWaitOpcodeHandler },
                       { FieldScriptOpCode.WaitForDialogueWindow, WaitForDialogueWindowOpcodeHandler },
                       { FieldScriptOpCode.CloseDialogueWindow, CloseDialogueWindowOpcodeHandler },
                       { FieldScriptOpCode.SetDialogueWindowStyle, SetDialogueWindowStyleOpcodeHandler },
                       { FieldScriptOpCode.SetMessageVariable, SetMessageVariableOpcodeHandler },
                       // { FieldScriptOpCode.SetMessageSpeed, SetMessageSpeedOpcodeHandler },
                       { FieldScriptOpCode.AskPlayerToMakeAChoice, AskPlayerToMakeAChoiceOpcodeHandler },
                       { FieldScriptOpCode.MainMenuAccessibility, MainMenuAccessibilityOpcodeHandler },
                       // { FieldScriptOpCode.OpenMainMenu, OpenMainMenuOpcodeHandler },
                       { FieldScriptOpCode.OpenSaveMenu, OpenSaveMenuOpcodeHandler },
                       // { FieldScriptOpCode.OpenShop, OpenShopOpcodeHandler },
                       // { FieldScriptOpCode.OpenNameEntry, OpenNameEntryOpcodeHandler },
                       // { FieldScriptOpCode.SetMapNameInMenu, SetMapNameInMenuOpcodeHandler },
                       // { FieldScriptOpCode.RunTutorial, RunTutorialOpcodeHandler },
                       { FieldScriptOpCode.ShowDialogueWindowNoWaitDismissible, ShowDialogueWindowNoWaitDismissibleOpcodeHandler },
                       { FieldScriptOpCode.ShowDialogueAt, ShowDialogueAtOpcodeHandler },
                       { FieldScriptOpCode.ShowDialogueAtNoWait, ShowDialogueAtNoWaitOpcodeHandler },
                       { FieldScriptOpCode.ShowDialogueAtNoWaitDismissible, ShowDialogueAtNoWaitDismissibleOpcodeHandler },
                       { FieldScriptOpCode.AskPlayerAt, AskPlayerAtOpcodeHandler },

                       // Party and inventory
                       // { FieldScriptOpCode.AddCharacterToParty, AddCharacterToPartyOpcodeHandler },
                       // { FieldScriptOpCode.RemoveCharacterFromParty, RemoveCharacterFromPartyOpcodeHandler },
                       // { FieldScriptOpCode.ChangePartyMembers, ChangePartyMembersOpcodeHandler },
                       // { FieldScriptOpCode.SetAllPartyCharacters, SetAllPartyCharactersOpcodeHandler },
                       // { FieldScriptOpCode.GetCharacterInPartySlot, GetCharacterInPartySlotOpcodeHandler },
                       // { FieldScriptOpCode.GetCharacterPartySlot, GetCharacterPartySlotOpcodeHandler },
                       // { FieldScriptOpCode.SetCharacterAvailability, SetCharacterAvailabilityOpcodeHandler },
                       // { FieldScriptOpCode.GetCharacterIsAvailable, GetCharacterIsAvailableOpcodeHandler },
                       // { FieldScriptOpCode.SetPartyMemberLocked, SetPartyMemberLockedOpcodeHandler },
                       // { FieldScriptOpCode.SetCharacterHP, SetCharacterHPOpcodeHandler },
                       // { FieldScriptOpCode.GetCharacterHP, GetCharacterHPOpcodeHandler },
                       // { FieldScriptOpCode.SetCharacterMP, SetCharacterMPOpcodeHandler },
                       // { FieldScriptOpCode.GetCharacterMP, GetCharacterMPOpcodeHandler },
                       { FieldScriptOpCode.AddMoney, AddMoneyOpcodeHandler },
                       // { FieldScriptOpCode.AddItem, AddItemOpcodeHandler },
                       // { FieldScriptOpCode.GetItemCount, GetItemCountOpcodeHandler },
                       { FieldScriptOpCode.SubtractMoney, SubtractMoneyOpcodeHandler },

                       // Field models and animation
                       { FieldScriptOpCode.SetPlayerEntity, SetPlayerEntityOpcodeHandler },
                       { FieldScriptOpCode.LockInput, LockInputOpcodeHandler },
                       // { FieldScriptOpCode.SetRunningEnabled, SetRunningEnabledOpcodeHandler },
                       // { FieldScriptOpCode.JoinPartyToLeader, JoinPartyToLeaderOpcodeHandler },
                       // { FieldScriptOpCode.SplitPartyFromLeader, SplitPartyFromLeaderOpcodeHandler },
                       // { FieldScriptOpCode.SetFollowerEnabled, SetFollowerEnabledOpcodeHandler },
                       // { FieldScriptOpCode.ResetFollowerTrail, ResetFollowerTrailOpcodeHandler },
                       { FieldScriptOpCode.Visibility, VisibilityOpcodeHandler },
                       { FieldScriptOpCode.EntityInField, EntityInFieldOpcodeHandler },
                       { FieldScriptOpCode.EntitySolidity, EntitySolidityOpcodeHandler },
                       { FieldScriptOpCode.PushScriptActivation, PushScriptActivationOpcodeHandler },
                       { FieldScriptOpCode.InteractionTriggerActivation, InteractabilityOpcodeHandler },
                       { FieldScriptOpCode.SetInteractionRange, SetInteractionRangeOpcodeHandler },
                       { FieldScriptOpCode.SetEntityPosition, SetEntityPositionOpcodeHandler },
                       // { FieldScriptOpCode.SetEntityHeightOffset, SetEntityHeightOffsetOpcodeHandler },
                       { FieldScriptOpCode.SetEntityDrawOffset, SetEntityDrawOffsetOpcodeHandler },
                       { FieldScriptOpCode.WaitForEntityDrawOffset, WaitForEntityDrawOffsetOpcodeHandler },
                       { FieldScriptOpCode.SetMovementSpeed, SetMovementSpeedOpcodeHandler },
                       { FieldScriptOpCode.MoveEntityAndWait, MoveEntityAndWaitOpcodeHandler },
                       { FieldScriptOpCode.MoveEntity, MoveEntityOpcodeHandler },
                       { FieldScriptOpCode.GlideEntityAndWait, GlideEntityAndWaitOpcodeHandler },
                       { FieldScriptOpCode.GlideEntity, GlideEntityOpcodeHandler },
                       { FieldScriptOpCode.SlideEntityAndWait, SlideEntityAndWaitOpcodeHandler },
                       { FieldScriptOpCode.SlideEntity, SlideEntityOpcodeHandler },
                       { FieldScriptOpCode.MoveEntityToEntityAndWait, MoveEntityToEntityAndWaitOpcodeHandler },
                       { FieldScriptOpCode.MoveEntityToEntity, MoveEntityToEntityOpcodeHandler },
                       // { FieldScriptOpCode.MoveToPartyMember, MoveToPartyMemberOpcodeHandler },
                       { FieldScriptOpCode.JumpEntityAndWait, JumpEntityAndWaitOpcodeHandler },
                       // { FieldScriptOpCode.JumpToPartyMember, JumpToPartyMemberOpcodeHandler },
                       { FieldScriptOpCode.ClimbLadderAndWait, ClimbLadderAndWaitOpcodeHandler },
                       { FieldScriptOpCode.WaitForMovement, WaitForMovementOpcodeHandler },
                       { FieldScriptOpCode.StopMovement, StopMovementOpcodeHandler },
                       { FieldScriptOpCode.SetEntityRotation, SetEntityRotationOpcodeHandler },
                       { FieldScriptOpCode.SetEntityRotationOverTime, SetEntityRotationOverTimeOpcodeHandler },
                       { FieldScriptOpCode.SetDirectionToFaceEntity, SetDirectionToFaceEntityOpcodeHandler },
                       { FieldScriptOpCode.SetDirectionToFacePosition, SetDirectionToFacePositionOpcodeHandler },
                       // { FieldScriptOpCode.SetDirectionToPartyMember, SetDirectionToPartyMemberOpcodeHandler },
                       { FieldScriptOpCode.TurnToEntityAndWait, TurnToEntityAndWaitOpcodeHandler },
                       // { FieldScriptOpCode.TurnToPartyMember, TurnToPartyMemberOpcodeHandler },
                       { FieldScriptOpCode.SetBaseAnimation, SetBaseAnimationOpcodeHandler },
                       { FieldScriptOpCode.SetLadderAnimations, SetLadderAnimationsOpcodeHandler },
                       { FieldScriptOpCode.PlayAnimationOnceAndWait, PlayAnimationOnceAndWaitOpcodeHandler },
                       { FieldScriptOpCode.PlayAnimationStopOnLastFrameWait, PlayAnimationStopOnLastFrameWaitOpcodeHandler },
                       { FieldScriptOpCode.PlayAnimationOnceAsync, PlayAnimationOnceAsyncOpcodeHandler },
                       { FieldScriptOpCode.PlayAnimationHoldingAsync, PlayAnimationHoldingAsyncOpcodeHandler },
                       { FieldScriptOpCode.PlayAnimationLooping, PlayAnimationLoopingOpcodeHandler },
                       { FieldScriptOpCode.PlayPartialAnimationAndWait, PlayPartialAnimationAndWaitOpcodeHandler },
                       { FieldScriptOpCode.PlayPartialAnimationAndHold, PlayPartialAnimationAndHoldOpcodeHandler },
                       { FieldScriptOpCode.PlayPartialAnimationAsync, PlayPartialAnimationAsyncOpcodeHandler },
                       { FieldScriptOpCode.PlayPartialAnimationHoldingAsync, PlayPartialAnimationHoldingAsyncOpcodeHandler },
                       { FieldScriptOpCode.PlayPartialAnimationLooping, PlayPartialAnimationLoopingOpcodeHandler },
                       { FieldScriptOpCode.SetAnimationSpeed, SetAnimationSpeedOpcodeHandler },
                       { FieldScriptOpCode.WaitForAnimation, WaitForAnimationOpcodeHandler },
                       { FieldScriptOpCode.StopAnimation, StopAnimationOpcodeHandler },
                       { FieldScriptOpCode.PushAnimationState, PushAnimationStateOpcodeHandler },
                       { FieldScriptOpCode.PopAnimationState, PopAnimationStateOpcodeHandler },
                       // { FieldScriptOpCode.InitialiseHeadFacing, InitialiseHeadFacingOpcodeHandler },
                       // { FieldScriptOpCode.SetHeadFacingEntity, SetHeadFacingEntityOpcodeHandler },
                       // { FieldScriptOpCode.SetHeadFacingPlayer, SetHeadFacingPlayerOpcodeHandler },
                       // { FieldScriptOpCode.SetHeadFacingLimit, SetHeadFacingLimitOpcodeHandler },
                       // { FieldScriptOpCode.SetHeadPose, SetHeadPoseOpcodeHandler },
                       // { FieldScriptOpCode.StopHeadFacing, StopHeadFacingOpcodeHandler },
                       // { FieldScriptOpCode.WaitForHeadFacing, WaitForHeadFacingOpcodeHandler },
                       // { FieldScriptOpCode.SetFootstepSound, SetFootstepSoundOpcodeHandler },
                       // { FieldScriptOpCode.SetFootstepsEnabled, SetFootstepsEnabledOpcodeHandler },
                       // { FieldScriptOpCode.SetEntityShadeLevel, SetEntityShadeLevelOpcodeHandler },
                       { FieldScriptOpCode.GetEntityPosition, GetEntityPositionOpcodeHandler },
                       { FieldScriptOpCode.GetEntityDirection, GetEntityDirectionOpcodeHandler },
                       // { FieldScriptOpCode.GetPartyMemberPosition, GetPartyMemberPositionOpcodeHandler },
                       // { FieldScriptOpCode.GetPartyMemberDirection, GetPartyMemberDirectionOpcodeHandler },
                       { FieldScriptOpCode.CopyEntityPose, CopyEntityPoseOpcodeHandler },
                       { FieldScriptOpCode.IsEntityInRange, IsEntityInRangeOpcodeHandler },
                       { FieldScriptOpCode.CollisionTriggerActivation, CollisionTriggerActivationOpcodeHandler },
                       { FieldScriptOpCode.SetEntityDrawOffsetAndWait, SetEntityDrawOffsetAndWaitOpcodeHandler },

                       // Screen and field effects
                       { FieldScriptOpCode.SubtractiveScreenFade, ctx => ScreenColourOpcodeHandler(ctx, false) },
                       { FieldScriptOpCode.AdditiveScreenFade, ctx => ScreenColourOpcodeHandler(ctx,    true) },
                       { FieldScriptOpCode.WaitForScreenColour, WaitForScreenColourOpcodeHandler },
                       // { FieldScriptOpCode.ParticleActivation, ParticleActivationOpcodeHandler },
                       { FieldScriptOpCode.BlockerActivation, BlockerActivationOpcodeHandler },

                       // Camera and screen movement
                       { FieldScriptOpCode.FadeScreen, FadeScreenOpcodeHandler },
                       { FieldScriptOpCode.FadeScreenWait, FadeScreenAndWaitOpcodeHandler },
                       { FieldScriptOpCode.WaitForFade, WaitForFadeOpcodeHandler },
                       // { FieldScriptOpCode.ShakeScreen, ShakeScreenOpcodeHandler },
                       { FieldScriptOpCode.ScrollScreenToPosition, ScrollScreenToPositionOpcodeHandler },
                       { FieldScriptOpCode.ScrollScreenToEntity, ScrollScreenToEntityOpcodeHandler },
                       // { FieldScriptOpCode.ScrollToPartyMember, ScrollToPartyMemberOpcodeHandler },
                       { FieldScriptOpCode.WaitForScrolling, WaitForScrollingOpcodeHandler },
                       { FieldScriptOpCode.SetCamera, SetCameraOpcodeHandler },

                       // Audio
                       { FieldScriptOpCode.PlayMusic, PlayMusicOpcodeHandler },
                       { FieldScriptOpCode.StopMusic, StopMusicOpcodeHandler },
                       { FieldScriptOpCode.SetMusicVolume, SetMusicVolumeOpcodeHandler },
                       { FieldScriptOpCode.FadeMusicVolume, FadeMusicVolumeOpcodeHandler },
                       { FieldScriptOpCode.SetMusicStemState, SetMusicStemStateOpcodeHandler },
                       { FieldScriptOpCode.CheckIfMusicIsPlaying, CheckIfMusicIsPlayingOpcodeHandler },
                       { FieldScriptOpCode.SetBattleMusic, SetBattleMusicOpcodeHandler },
                       { FieldScriptOpCode.PlaySound, PlaySoundOpcodeHandler },
                       { FieldScriptOpCode.SetAllSoundVolume, SetAllSoundVolumeOpcodeHandler },
                       { FieldScriptOpCode.FadeAllSoundVolume, FadeAllSoundVolumeOpcodeHandler },
                       { FieldScriptOpCode.SetReverbPreset, SetReverbPresetOpcodeHandler },
                       { FieldScriptOpCode.SetReverbVolume, SetReverbVolumeOpcodeHandler },
                       { FieldScriptOpCode.CrossfadeMusic, CrossfadeMusicOpcodeHandler },
                       { FieldScriptOpCode.FadeMusicVolumeFrom, FadeMusicVolumeFromOpcodeHandler },
                       { FieldScriptOpCode.WaitForMusicVolume, WaitForMusicVolumeOpcodeHandler },
                       { FieldScriptOpCode.StopSound, StopSoundOpcodeHandler },
                       { FieldScriptOpCode.FadeSoundVolume, FadeSoundVolumeOpcodeHandler },
                       { FieldScriptOpCode.FadeSoundPan, FadeSoundPanOpcodeHandler },

                       // Video
                       // { FieldScriptOpCode.PrepareMovie, PrepareMovieOpcodeHandler },
                       // { FieldScriptOpCode.PlayMovie, PlayMovieOpcodeHandler },
                       // { FieldScriptOpCode.WaitForMovie, WaitForMovieOpcodeHandler },

                       // Timer
                       { FieldScriptOpCode.SetCountdownTimer, SetCountdownTimerOpcodeHandler },
                       { FieldScriptOpCode.ShowCountdownTimer, ShowCountdownTimerOpcodeHandler },
                       { FieldScriptOpCode.GetCountdownTimer, GetCountdownTimerOpcodeHandler },
                       { FieldScriptOpCode.HideCountdownTimer, HideCountdownTimerOpcodeHandler },

                       // Input and haptics
                       // { FieldScriptOpCode.SetVibration, SetVibrationOpcodeHandler },
                       { FieldScriptOpCode.WaitForButton, WaitForButtonOpcodeHandler },
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
        /// <see cref="FieldEntities.BYTECODE_FORMAT_VERSION" /> for why that matters.
        /// </summary>
        internal void RegisterScript(int scriptId, uint formatVersion, byte[] bytecode)
        {
            if (formatVersion != FieldEntities.BYTECODE_FORMAT_VERSION)
            {
                Debug.LogError($"{nameof(FieldVM)}::{nameof(RegisterScript)} Script [{scriptId}] was compiled for bytecode format [{formatVersion}] but this build reads format [{FieldEntities.BYTECODE_FORMAT_VERSION}], so it has not been loaded. Export the field again. Format [0] means it was never exported");

                return;
            }

            m_Scripts.Add(scriptId, bytecode);
        }

        /// <summary>
        /// Start one of an entity's scripts by event id, for something the engine initiates rather than
        /// another script — a collision or interaction trigger. Refused if the slot is busy.
        /// </summary>
        internal void RequestScript(int entityId, int eventId)
        {
            FieldEntityRuntime entity = m_Entities[entityId];

            if (!entity.TryGetScript(eventId, out int scriptId, out byte slot))
            {
                return;
            }

            entity.TryRequestScript(scriptId, slot);
        }

        /// <summary>
        /// Shared by an entity's slots within a frame: a script that returns, or is preempted, leaves the rest
        /// for the slot that runs after it.
        /// </summary>
        internal const int INSTRUCTIONS_PER_ENTITY_PER_FRAME = 16;

        /// <summary>
        /// Init runs until it returns, however many instructions that takes. This only stops one that
        /// never returns, which would otherwise hang the field load.
        /// </summary>
        internal const int INIT_INSTRUCTION_CEILING = 1024;

        /// <summary>
        /// Replace an entity's init script with its Default script, whether or not init returned. An entity
        /// with no Default script is left with the slot empty.
        /// </summary>
        internal void StartDefaultScript(int entityId, int defaultEventId)
        {
            FieldEntityRuntime entity = m_Entities[entityId];

            m_Contexts.Remove(new SlotKey(entityId, (byte)FieldScriptPriority.Main));

            if (entity.TryGetScriptId(defaultEventId, out int scriptId))
            {
                entity.ReplaceScriptInSlot(scriptId, (byte)FieldScriptPriority.Main);
                return;
            }

            entity.ClearSlot((byte)FieldScriptPriority.Main);
        }

        internal ScriptRunOutcome Execute(int entityId, byte priority, int scriptId, FieldEntityRuntime entity, ref int instructionBudget)
        {
            SlotKey key = new SlotKey(entityId, priority);

            // A slot outlives the scripts that pass through it, so a context is reused only while it is
            // still running the script the slot currently holds.
            if (!m_Contexts.TryGetValue(key, out FieldScriptContext ctx) || ctx.ScriptId != scriptId)
            {
                if (!m_Scripts.TryGetValue(scriptId, out byte[] bytecode))
                {
                    entity.ClearSlot(priority);
                    return ScriptRunOutcome.Ended;
                }

                ctx             = new FieldScriptContext(entityId, priority, scriptId, bytecode, m_MemoryService, m_TempBytes);
                m_Contexts[key] = ctx;
            }

            // The frame a block completes on is spent, so the next instruction runs next frame.
            if (ctx.IsBlocked())
            {
                ctx.UpdateBlock(Time.deltaTime);
                return ScriptRunOutcome.Waiting;
            }

            while (!ctx.IsBlocked())
            {
                if (instructionBudget == 0)
                {
                    return ScriptRunOutcome.OutOfInstructions;
                }

                instructionBudget--;

                FieldScriptOpCode  opcode        = FetchOpcode(ctx);
                FieldOpCodeHandler opcodeHandler = m_OpcodeHandlers[(ushort)opcode];

                if (opcodeHandler == null)
                {
                    m_Contexts.Remove(key);
                    entity.ClearSlot(priority);
                    return ScriptRunOutcome.Ended;
                }

                ctx.BeginInstruction((ushort)opcode);

                opcodeHandler(ctx);

#if UNITY_EDITOR
                if (!ctx.ReadAsDescribed())
                {
                    ctx.Block(new WaitUntilBlock(() => false));
                }
#endif

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

                // The slot was handed to another script, which runs next on what is left of the budget.
                if (!m_Contexts.TryGetValue(key, out FieldScriptContext current) || current != ctx)
                {
                    return ScriptRunOutcome.Ended;
                }

                if (entity.RunningPriority > priority)
                {
                    return ScriptRunOutcome.Preempted;
                }
            }

            return ScriptRunOutcome.Waiting;
        }

        private static FieldScriptOpCode FetchOpcode(FieldScriptContext ctx)
        {
            FieldScriptOpCode opcode = (FieldScriptOpCode)ctx.NextUshort();

            return opcode;
        }

        /// <summary>
        /// Decode an opcode that only names a destination, such as an increment.
        /// </summary>
        private static void ReadDestination(FieldScriptContext ctx, out MemoryBank destinationBank, out ushort destinationAddress)
        {
            byte sources = ctx.NextByte();

            destinationBank    = FieldScriptContext.ToMemoryBank(FieldScriptContext.GetFirstArgumentSource(sources));
            destinationAddress = ctx.NextUshort();
        }

        /// <summary>
        /// Decode a bank-layout destination: the sources byte and the destination's address. The value's source is
        /// the sources byte's low nibble.
        /// </summary>
        private static void DecodeDestination(FieldScriptContext ctx, out MemoryBank bank, out ushort address, out byte valueSource)
        {
            byte sources = ctx.NextByte();

            bank        = FieldScriptContext.ToMemoryBank(FieldScriptContext.GetFirstArgumentSource(sources));
            address     = ctx.NextUshort();
            valueSource = FieldScriptContext.GetSecondArgumentSource(sources);
        }

        private void ApplyBinary(FieldScriptContext ctx, VariableWidth width, Func<ulong, ulong, VariableWidth, ulong> integer, Func<float, float, float> real)
        {
            DecodeDestination(ctx, out MemoryBank bank, out ushort address, out byte valueSource);

            if (width == VariableWidth.Float)
            {
                float value = ReadFloatOperand(ctx, valueSource);

                ctx.WriteVariableFloat(bank, address, real(ctx.ReadVariableFloat(bank, address), value));
                return;
            }

            ulong integerValue = ReadIntegerOperand(ctx, valueSource, width);
            ulong current      = ctx.ReadVariableBits(bank, address, width);

            ctx.WriteVariableBits(bank, address, width, integer(current, integerValue, width));
        }

        private void ApplyUnary(FieldScriptContext ctx, VariableWidth width, Func<ulong, VariableWidth, ulong> integer)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            ulong current = ctx.ReadVariableBits(bank, address, width);

            ctx.WriteVariableBits(bank, address, width, integer(current, width));
        }

        private void ChangeBit(FieldScriptContext ctx, VariableWidth width, bool set, string handlerName)
        {
            DecodeDestination(ctx, out MemoryBank bank, out ushort address, out byte indexSource);

            ulong index   = ReadIntegerOperand(ctx, indexSource, VariableWidth.Byte);
            ulong current = ctx.ReadVariableBits(bank, address, width);

            if (!ScriptArithmetic.TrySetBit(current, index, width, set, out ulong result))
            {
                Debug.LogError($"{nameof(FieldVM)}::{handlerName} Bit index [{index}] is out of range for a {width} at [{bank}:{address}]");
                return;
            }

            ctx.WriteVariableBits(bank, address, width, result);
        }

        // A bank-layout value is an immediate at the argument's width, or the width of the variable it reads followed
        // by that variable's address. A narrower variable is converted as a cast would convert it.
        private ulong ReadIntegerOperand(FieldScriptContext ctx, byte source, VariableWidth width)
        {
            if (source == FieldScriptContext.ARGUMENT_IMMEDIATE)
            {
                ulong immediate = ReadImmediateBits(ctx, width);

                return immediate;
            }

            VariableWidth variableWidth = (VariableWidth)ctx.NextByte();
            ushort        address       = ctx.NextUshort();

            ulong bits  = ctx.ReadVariableBits(FieldScriptContext.ToMemoryBank(source), address, variableWidth);
            ulong value = ScriptArithmetic.Convert(bits, variableWidth, width);

            return value;
        }

        private float ReadFloatOperand(FieldScriptContext ctx, byte source)
        {
            if (source == FieldScriptContext.ARGUMENT_IMMEDIATE)
            {
                float immediate = ctx.NextFloat();

                return immediate;
            }

            VariableWidth variableWidth = (VariableWidth)ctx.NextByte();
            MemoryBank    bank          = FieldScriptContext.ToMemoryBank(source);
            ushort        address       = ctx.NextUshort();

            float value = variableWidth == VariableWidth.Float
                              ? ctx.ReadVariableFloat(bank, address)
                              : ScriptArithmetic.ToFloat(ctx.ReadVariableBits(bank, address, variableWidth), variableWidth);

            return value;
        }

        private bool ReadBoolOperand(FieldScriptContext ctx, byte source)
        {
            if (source == FieldScriptContext.ARGUMENT_IMMEDIATE)
            {
                bool immediate = ctx.NextBool();

                return immediate;
            }

            // The width byte: a bool only ever reads a bool.
            ctx.NextByte();

            ushort address = ctx.NextUshort();
            bool   value   = ctx.ReadVariableBool(FieldScriptContext.ToMemoryBank(source), address);

            return value;
        }

        private static ulong ReadImmediateBits(FieldScriptContext ctx, VariableWidth width)
        {
            ulong bits = width.GetByteCount() switch
                         {
                             1 => ctx.NextByte(),
                             2 => ctx.NextUshort(),
                             4 => (uint)ctx.NextInt(),
                             _ => ctx.NextUlong()
                         };

            return bits;
        }

        private static bool CompareBools(bool a, bool b, ScriptComparison comparison)
        {
            bool result = comparison switch
                          {
                              ScriptComparison.Equal    => a == b,
                              ScriptComparison.NotEqual => a != b,
                              _                         => throw new InvalidOperationException($"{nameof(FieldVM)}::{nameof(CompareBools)} Unknown comparison [{comparison}]")
                          };

            return result;
        }

        private static bool CompareFloats(float a, float b, ScriptComparison comparison)
        {
            bool result = comparison switch
                          {
                              ScriptComparison.Equal              => a == b,
                              ScriptComparison.NotEqual           => a != b,
                              ScriptComparison.GreaterThan        => a > b,
                              ScriptComparison.LessThan           => a < b,
                              ScriptComparison.GreaterThanOrEqual => a >= b,
                              ScriptComparison.LessThanOrEqual    => a <= b,
                              _                                   => throw new InvalidOperationException($"{nameof(FieldVM)}::{nameof(CompareFloats)} Unknown comparison [{comparison}]")
                          };

            return result;
        }

        /// <summary>
        /// A dialogue channel shows one thing at a time. When it is busy the instruction is rewound and waits,
        /// so it runs again once the channel is free.
        /// </summary>
        /// <returns>True when the channel was busy and the instruction is now waiting.</returns>
        private bool WaitForDialogueChannel(FieldScriptContext ctx, byte channel, int instructionStart)
        {
            if (!IsDialogueWindowOpen(channel))
            {
                return false;
            }

            ctx.InstructionPointer = instructionStart;
            ctx.Block(new WaitUntilBlock(() => !IsDialogueWindowOpen(channel)));

            return true;
        }

        /// <summary>
        /// Decode the arguments shared by the three request opcodes and resolve the target entity.
        /// </summary>
        private bool TryReadScriptRequest(FieldScriptContext ctx, out byte targetEntityId, out FieldEntityRuntime target, out int targetScriptId, out byte priority)
        {
            ushort targetEventId;

            targetEntityId = ctx.ReadByte();
            targetEventId  = ctx.ReadUShort();

            targetScriptId = 0;
            priority       = 0;

            if (!m_Entities.TryGetValue(targetEntityId, out target))
            {
                return false;
            }

            // The request names an event id relative to the target entity, which the entity resolves to the
            // field-wide script id the VM holds bytecode under, and to the slot that script's type runs in.
            if (!target.TryGetScript(targetEventId, out targetScriptId, out priority))
            {
                target = null;
                return false;
            }

            return true;
        }

        // Already satisfied: carry on next frame. Otherwise wait, and the frame the wait ends on is spent too.
        private static void WaitForRequest(FieldScriptContext ctx, Func<bool> isSatisfied)
        {
            if (isSatisfied())
            {
                ctx.YieldRequested = true;
                return;
            }

            ctx.Block(new WaitUntilBlock(isSatisfied));
        }

        /// <summary>
        /// End the script. Execute frees the slot once this has run, so there is nothing to do here.
        /// </summary>
        private static void ReturnOpcodeHandler(FieldScriptContext ctx)
        {
            // noop
        }

        /// <summary>
        /// Put a script in an entity's priority slot and carry on; refused if the slot is busy. A request on
        /// another entity ends this script's frame. One on this entity for a more urgent slot takes over
        /// straight away.
        /// </summary>
        private void RunAnotherEntityScriptUnlessBusyOpcodeHandler(FieldScriptContext ctx)
        {
            if (TryReadScriptRequest(ctx, out byte targetEntityId, out FieldEntityRuntime target, out int targetScriptId, out byte priority))
            {
                target.TryRequestScript(targetScriptId, priority);
            }

            if (targetEntityId != ctx.EntityId)
            {
                ctx.YieldRequested = true;
            }
        }

        /// <summary>
        /// Request a script once. If the slot is busy, carry on. If it is accepted, wait until the entity is
        /// running it — or has already run it to its return, which would otherwise leave this waiting for
        /// good.
        /// </summary>
        private void RunAnotherEntityScriptWaitUntilStartedOpcodeHandler(FieldScriptContext ctx)
        {
            if (!TryReadScriptRequest(ctx, out _, out FieldEntityRuntime target, out int targetScriptId, out byte priority) ||
                !target.TryRequestScript(targetScriptId, priority))
            {
                ctx.YieldRequested = true;
                return;
            }

            bool HasStarted() => target.RunningPriority == priority || !target.HoldsScript(targetScriptId, priority);

            WaitForRequest(ctx, HasStarted);
        }

        /// <summary>
        /// Request a script once. If the slot is busy, carry on. If it is accepted, wait until the entity is
        /// running something less urgent than it — the requested script has returned.
        /// </summary>
        private void RunAnotherEntityScriptWaitUntilFinishedOpcodeHandler(FieldScriptContext ctx)
        {
            if (!TryReadScriptRequest(ctx, out _, out FieldEntityRuntime target, out int targetScriptId, out byte priority) ||
                !target.TryRequestScript(targetScriptId, priority))
            {
                ctx.YieldRequested = true;
                return;
            }

            bool HasFinished() => target.RunningPriority < priority;

            WaitForRequest(ctx, HasFinished);
        }

        /// <summary>
        /// Hand this slot over to another script. The script being replaced is the one asking, so unlike
        /// a request from outside this replaces rather than being refused.
        /// </summary>
        private void ReturnToAnotherScriptOpcodeHandler(FieldScriptContext ctx)
        {
            ushort targetEventId = ctx.ReadUShort();

            FieldEntityRuntime entity = m_Entities[ctx.EntityId];

            if (!entity.TryGetScriptId(targetEventId, out int targetScriptId))
            {
                return;
            }

            m_Contexts.Remove(new SlotKey(ctx.EntityId, ctx.Priority));

            entity.ReplaceScriptInSlot(targetScriptId, ctx.Priority);
        }

        /// <summary>
        /// Run another of this entity's scripts in this slot, carrying on from the next instruction once it returns
        /// from the call. Export has checked the script exists and that calls end.
        /// </summary>
        private void CallAnotherScriptOpcodeHandler(FieldScriptContext ctx)
        {
            ushort targetEventId = ctx.NextUshort();

            if (m_Entities[ctx.EntityId].TryGetScriptId(targetEventId, out int targetScriptId))
            {
                ctx.EnterCall(m_Scripts[targetScriptId]);
            }
        }

        /// <summary>
        /// Go back to the script that made the call. A script not inside a call ends, so one can be both called and
        /// run on its own.
        /// </summary>
        private void ReturnFromCallOpcodeHandler(FieldScriptContext ctx)
        {
            if (!ctx.TryLeaveCall())
            {
                EndSlot(ctx.EntityId, ctx.Priority);
            }
        }

        private void EndSlot(int entityId, byte priority)
        {
            m_Contexts.Remove(new SlotKey(entityId, priority));
            m_Entities[entityId].ClearSlot(priority);
        }

        /// <summary>
        /// Move the instruction pointer by a signed offset, counted from the end of this instruction.
        /// </summary>
        private static void GotoOpcodeHandler(FieldScriptContext ctx)
        {
            int offset = ctx.NextInt();
            ctx.InstructionPointer += offset;
        }

        /// <summary>
        /// Move the instruction pointer to an absolute position in the script.
        /// </summary>
        private static void GotoDirectlyOpcodeHandler(FieldScriptContext ctx)
        {
            int target = ctx.NextInt();
            ctx.InstructionPointer = target;
        }

        /// <summary>
        /// Compare two values, skipping the IF body when the comparison fails.
        /// </summary>
        private void CompareOpcodeHandler(FieldScriptContext ctx, VariableWidth width)
        {
            byte sources = ctx.NextByte();
            byte aSource = FieldScriptContext.GetFirstArgumentSource(sources);
            byte bSource = FieldScriptContext.GetSecondArgumentSource(sources);

            bool result;

            switch (width)
            {
                case VariableWidth.Bool:
                {
                    bool a = ReadBoolOperand(ctx, aSource);
                    bool b = ReadBoolOperand(ctx, bSource);

                    result = CompareBools(a, b, (ScriptComparison)ctx.NextByte());
                    break;
                }

                case VariableWidth.Float:
                {
                    float a = ReadFloatOperand(ctx, aSource);
                    float b = ReadFloatOperand(ctx, bSource);

                    result = CompareFloats(a, b, (ScriptComparison)ctx.NextByte());
                    break;
                }

                default:
                {
                    ulong a = ReadIntegerOperand(ctx, aSource, width);
                    ulong b = ReadIntegerOperand(ctx, bSource, width);

                    result = ScriptArithmetic.Compare(a, b, width, (ScriptComparison)ctx.NextByte());
                    break;
                }
            }

            int jumpDistance = ctx.NextInt();

            if (!result)
            {
                ctx.InstructionPointer += jumpDistance;
            }
        }

        /// <summary>
        /// Stop running this script for the rest of the frame; it carries on from the next instruction next
        /// frame.
        /// </summary>
        private static void YieldOpcodeHandler(FieldScriptContext ctx)
        {
            ctx.YieldRequested = true;
        }

        /// <summary>
        /// Block this script for a number of seconds.
        /// </summary>
        private void WaitSecondsOpcodeHandler(FieldScriptContext ctx)
        {
            float seconds = ctx.ReadFloat();
            ctx.Block(new WaitSecondsBlock(seconds));
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        private static void DoNothingOpcodeHandler(FieldScriptContext ctx)
        {
            // noop
        }

        /// <summary>
        /// End this script and every interrupted script between it and a less urgent slot, which is what runs next.
        /// </summary>
        private void ReturnToPriorityOpcodeHandler(FieldScriptContext ctx)
        {
            byte priority = ctx.ReadByte();

            EndSlot(ctx.EntityId, ctx.Priority);

            for (int slot = ctx.Priority - 1; slot > priority; slot--)
            {
                EndSlot(ctx.EntityId, (byte)slot);
            }
        }

        /// <summary>
        /// Ask for a transition to another field, entering at one of its spawn points. The script stops here:
        /// nothing after a map jump runs.
        /// </summary>
        private void JumpToAnotherMapOpcodeHandler(FieldScriptContext ctx)
        {
            ulong fieldNameHash = ctx.ReadName();
            int   spawnId       = ctx.ReadInt();

            FieldArgs args = new FieldArgs(fieldNameHash, spawnId);
            RequestFieldTransition?.Invoke(args);

            ctx.Block(new WaitUntilBlock(() => false));
        }

        /// <summary>
        /// Let the player leave the field through its gateways, or stop them: only entities' Gateway scripts
        /// listen. A script's own map jump, and every other collision trigger script, carry on regardless.
        /// </summary>
        private void GatewayTriggerActivationOpcodeHandler(FieldScriptContext ctx)
        {
            bool enabled = ctx.ReadBool();
            RequestSetGatewayTriggersActive?.Invoke(enabled);
        }

        /// <summary>
        /// Set whether leaving for another field covers the screen with this field's exit transition, kept until set
        /// again: a script that has faded the screen itself turns it off.
        /// </summary>
        private void SetFieldExitFadeOpcodeHandler(FieldScriptContext ctx)
        {
            bool fades = ctx.ReadBool();

            RequestFieldExitFade?.Invoke(fades);
        }

        /// <summary>
        /// Set the battle mode: the flags every random battle is fought with, and whether losing a battle or a countdown
        /// running out ends the game.
        /// </summary>
        private void SetBattleModeOpcodeHandler(FieldScriptContext ctx)
        {
            ushort mode = ctx.ReadUShort();

            RequestSetBattleMode?.Invoke((BattleFlags)mode);
        }

        /// <summary>
        /// Ask for a battle to begin, with its arena, enemy group, flags, enemy level and the transition that starts it,
        /// and end the frame, so the rest of the script runs after the battle rather than before it.
        /// </summary>
        private void StartBattleOpcodeHandler(FieldScriptContext ctx)
        {
            ushort arena       = ctx.ReadUShort();
            ushort enemyGroup  = ctx.ReadUShort();
            ushort battleFlags = ctx.ReadUShort();
            byte   enemyLevel  = ctx.ReadByte();
            ulong  transition  = ctx.ReadName();

            BattleArgs args = new BattleArgs(arena, enemyGroup, (BattleFlags)battleFlags, enemyLevel, transition);

            RequestStartBattle?.Invoke(args);

            ctx.YieldRequested = true;
        }

        /// <summary>
        /// Store how the last battle ended, as its <see cref="BattleCompleteState" />.
        /// </summary>
        private void GetLastBattleResultOpcodeHandler(FieldScriptContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            byte result = (byte)GetLastBattleResult();

            ctx.WriteVariableByte(bank, address, result);
        }

        /// <summary>
        /// Ask for the game to end, and end the frame, as the field is about to go.
        /// </summary>
        private void GameOverOpcodeHandler(FieldScriptContext ctx)
        {
            RequestGameOver?.Invoke();

            ctx.YieldRequested = true;
        }

        /// <summary>
        /// Allow or block saving from the party menu.
        /// </summary>
        private void SaveAccessibilityOpcodeHandler(FieldScriptContext ctx)
        {
            bool enabled = ctx.ReadBool();
            RequestSetSaveEnabled?.Invoke(enabled);
        }

        /// <summary>
        /// Set a variable to a value.
        /// </summary>
        private void SetOpcodeHandler(FieldScriptContext ctx, VariableWidth width)
        {
            DecodeDestination(ctx, out MemoryBank bank, out ushort address, out byte valueSource);

            switch (width)
            {
                case VariableWidth.Bool:
                    ctx.WriteVariableBool(bank, address, ReadBoolOperand(ctx, valueSource));
                    return;

                case VariableWidth.Float:
                    ctx.WriteVariableFloat(bank, address, ReadFloatOperand(ctx, valueSource));
                    return;

                default:
                    ctx.WriteVariableBits(bank, address, width, ReadIntegerOperand(ctx, valueSource, width));
                    return;
            }
        }

        /// <summary>
        /// Add a value to a variable, wrapping if it overflows.
        /// </summary>
        private void AddOpcodeHandler(FieldScriptContext ctx, VariableWidth width)
        {
            ApplyBinary(ctx, width, (current, value, w) => ScriptArithmetic.Add(current, value, w), (current, value) => current + value);
        }

        /// <summary>
        /// Add a value to a variable, stopping at its limit.
        /// </summary>
        private void AddClampedOpcodeHandler(FieldScriptContext ctx, VariableWidth width)
        {
            ApplyBinary(ctx, width, (current, value, w) => ScriptArithmetic.AddClamped(current, value, w), null);
        }

        /// <summary>
        /// Subtract a value from a variable, wrapping if it overflows.
        /// </summary>
        private void SubtractOpcodeHandler(FieldScriptContext ctx, VariableWidth width)
        {
            ApplyBinary(ctx, width, (current, value, w) => ScriptArithmetic.Subtract(current, value, w), (current, value) => current - value);
        }

        /// <summary>
        /// Subtract a value from a variable, stopping at its limit.
        /// </summary>
        private void SubtractClampedOpcodeHandler(FieldScriptContext ctx, VariableWidth width)
        {
            ApplyBinary(ctx, width, (current, value, w) => ScriptArithmetic.SubtractClamped(current, value, w), null);
        }

        /// <summary>
        /// Multiply a variable by a value, wrapping if it overflows.
        /// </summary>
        private void MultiplyOpcodeHandler(FieldScriptContext ctx, VariableWidth width)
        {
            ApplyBinary(ctx, width, (current, value, w) => ScriptArithmetic.Multiply(current, value, w), (current, value) => current * value);
        }

        /// <summary>
        /// Divide a variable by a value. Dividing by zero is logged and the variable left unchanged.
        /// </summary>
        private void DivideOpcodeHandler(FieldScriptContext ctx, VariableWidth width)
        {
            DecodeDestination(ctx, out MemoryBank bank, out ushort address, out byte valueSource);

            if (width == VariableWidth.Float)
            {
                float divisor = ReadFloatOperand(ctx, valueSource);

                if (divisor == 0f)
                {
                    Debug.LogError($"{nameof(FieldVM)}::{nameof(DivideOpcodeHandler)} Divide by zero at [{bank}:{address}], leaving the value unchanged");
                    return;
                }

                ctx.WriteVariableFloat(bank, address, ctx.ReadVariableFloat(bank, address) / divisor);
                return;
            }

            ulong value   = ReadIntegerOperand(ctx, valueSource, width);
            ulong current = ctx.ReadVariableBits(bank, address, width);

            if (!ScriptArithmetic.TryDivide(current, value, width, out ulong quotient))
            {
                Debug.LogError($"{nameof(FieldVM)}::{nameof(DivideOpcodeHandler)} Divide by zero at [{bank}:{address}], leaving the value unchanged");
                return;
            }

            ctx.WriteVariableBits(bank, address, width, quotient);
        }

        /// <summary>
        /// Replace a variable with its remainder after dividing by a value. Dividing by zero is logged and the variable left unchanged.
        /// </summary>
        private void RemainderOpcodeHandler(FieldScriptContext ctx, VariableWidth width)
        {
            DecodeDestination(ctx, out MemoryBank bank, out ushort address, out byte valueSource);

            ulong value   = ReadIntegerOperand(ctx, valueSource, width);
            ulong current = ctx.ReadVariableBits(bank, address, width);

            if (!ScriptArithmetic.TryRemainder(current, value, width, out ulong remainder))
            {
                Debug.LogError($"{nameof(FieldVM)}::{nameof(RemainderOpcodeHandler)} Modulo by zero at [{bank}:{address}], leaving the value unchanged");
                return;
            }

            ctx.WriteVariableBits(bank, address, width, remainder);
        }

        /// <summary>
        /// Add one to a variable, wrapping if it overflows.
        /// </summary>
        private void IncrementOpcodeHandler(FieldScriptContext ctx, VariableWidth width)
        {
            ApplyUnary(ctx, width, (current, w) => ScriptArithmetic.Add(current, 1, w));
        }

        /// <summary>
        /// Add one to a variable, stopping at its limit.
        /// </summary>
        private void IncrementClampedOpcodeHandler(FieldScriptContext ctx, VariableWidth width)
        {
            ApplyUnary(ctx, width, (current, w) => ScriptArithmetic.AddClamped(current, 1, w));
        }

        /// <summary>
        /// Subtract one from a variable, wrapping if it overflows.
        /// </summary>
        private void DecrementOpcodeHandler(FieldScriptContext ctx, VariableWidth width)
        {
            ApplyUnary(ctx, width, (current, w) => ScriptArithmetic.Subtract(current, 1, w));
        }

        /// <summary>
        /// Subtract one from a variable, stopping at its limit.
        /// </summary>
        private void DecrementClampedOpcodeHandler(FieldScriptContext ctx, VariableWidth width)
        {
            ApplyUnary(ctx, width, (current, w) => ScriptArithmetic.SubtractClamped(current, 1, w));
        }

        /// <summary>
        /// Bitwise AND a variable with a value.
        /// </summary>
        private void BitwiseAndOpcodeHandler(FieldScriptContext ctx, VariableWidth width)
        {
            ApplyBinary(ctx, width, (current, value, w) => ScriptArithmetic.BitwiseAnd(current, value, w), null);
        }

        /// <summary>
        /// Bitwise OR a variable with a value.
        /// </summary>
        private void BitwiseOrOpcodeHandler(FieldScriptContext ctx, VariableWidth width)
        {
            ApplyBinary(ctx, width, (current, value, w) => ScriptArithmetic.BitwiseOr(current, value, w), null);
        }

        /// <summary>
        /// Bitwise XOR a variable with a value.
        /// </summary>
        private void BitwiseXorOpcodeHandler(FieldScriptContext ctx, VariableWidth width)
        {
            ApplyBinary(ctx, width, (current, value, w) => ScriptArithmetic.BitwiseXor(current, value, w), null);
        }

        /// <summary>
        /// Set one bit of a variable. An index past its last bit is logged and the variable left unchanged.
        /// </summary>
        private void SetBitOpcodeHandler(FieldScriptContext ctx, VariableWidth width)
        {
            ChangeBit(ctx, width, true, nameof(SetBitOpcodeHandler));
        }

        /// <summary>
        /// Clear one bit of a variable. An index past its last bit is logged and the variable left unchanged.
        /// </summary>
        private void UnsetBitOpcodeHandler(FieldScriptContext ctx, VariableWidth width)
        {
            ChangeBit(ctx, width, false, nameof(UnsetBitOpcodeHandler));
        }

        /// <summary>
        /// Write a random byte, 0 to 255, to the destination.
        /// </summary>
        private void GetRandomNumberOpcodeHandler(FieldScriptContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            byte result = (byte)m_Random.Next(0, byte.MaxValue + 1);

            ctx.WriteVariableByte(bank, address, result);
        }

        /// <summary>
        /// Reseed the VM's random sequence, so a script can be made deterministic.
        /// </summary>
        private void RandomNumberSeedOpcodeHandler(FieldScriptContext ctx)
        {
            byte sources = ctx.NextByte();
            int  seed    = unchecked((int)ReadIntegerOperand(ctx, FieldScriptContext.GetSecondArgumentSource(sources), VariableWidth.Int));

            m_Random = new System.Random(seed);
        }

        /// <summary>
        /// Flip every bit of a variable.
        /// </summary>
        private void BitwiseNotOpcodeHandler(FieldScriptContext ctx, VariableWidth width)
        {
            ApplyUnary(ctx, width, (current, w) => ScriptArithmetic.BitwiseNot(current, w));
        }

        /// <summary>
        /// Set where a dialogue channel's window appears and how big it is.
        /// </summary>
        private void CreateDialogueWindowOpcodeHandler(FieldScriptContext ctx)
        {
            byte    channel = ctx.NextByte();
            int     x       = ctx.ReadInt();
            int     y       = ctx.ReadInt();
            int     width   = ctx.ReadInt();
            int     height  = ctx.ReadInt();
            RectInt rect    = new RectInt(x, y, width, height);
            RequestCreateDialogueWindow?.Invoke(new DialogueWindowArgs(channel, rect));
        }

        /// <summary>
        /// Show a line of dialogue on a channel and block until the player dismisses it. A channel already
        /// showing something is waited for, and this instruction runs again once it is free.
        /// </summary>
        private void ShowDialogueWindowOpcodeHandler(FieldScriptContext ctx)
        {
            int   instructionStart = ctx.InstructionPointer - sizeof(ushort);
            byte  channel          = ctx.NextByte();
            ulong dialogueId       = ctx.ReadName();
            bool  blockMovement    = ctx.ReadBool();

            if (WaitForDialogueChannel(ctx, channel, instructionStart))
            {
                return;
            }

            RequestShowDialogueWindow?.Invoke(channel, dialogueId, blockMovement);

            ctx.Block(new WaitUntilBlock(() => !IsDialogueWindowOpen(channel)));
        }

        /// <summary>
        /// Show dialogue the player does not answer, and carry on. The window stays up until
        /// <c>CLOSE_DIALOGUE_WINDOW</c>. A channel already showing something is waited for. Showing it ends this
        /// entity's frame, as the reference's non-waiting message does.
        /// </summary>
        private void ShowDialogueWindowNoWaitOpcodeHandler(FieldScriptContext ctx)
        {
            ShowDialogueWindowWithoutWaiting(ctx, false);
        }

        private void ShowDialogueWindowWithoutWaiting(FieldScriptContext ctx, bool dismissible)
        {
            int   instructionStart = ctx.InstructionPointer - sizeof(ushort);
            byte  channel          = ctx.NextByte();
            ulong dialogueId       = ctx.NextUlong();

            if (WaitForDialogueChannel(ctx, channel, instructionStart))
            {
                return;
            }

            RequestShowDialogueWindowNoWait?.Invoke(channel, dialogueId, dismissible);

            ctx.YieldRequested = true;
        }

        /// <summary>
        /// Wait until nothing is showing on a dialogue channel. The field is told, as a window left up in the background
        /// then closes on a press once it has finished.
        /// </summary>
        private void WaitForDialogueWindowOpcodeHandler(FieldScriptContext ctx)
        {
            byte channel = ctx.NextByte();

            if (IsDialogueWindowOpen(channel))
            {
                RequestWaitForDialogueWindow?.Invoke(channel);

                ctx.Block(new WaitUntilBlock(() => !IsDialogueWindowOpen(channel)));
            }
        }

        /// <summary>
        /// Close whatever is showing on a dialogue channel. A script waiting on that window carries on as if it had
        /// been dismissed; a question closed this way stores no answer.
        /// </summary>
        private void CloseDialogueWindowOpcodeHandler(FieldScriptContext ctx)
        {
            byte channel = ctx.NextByte();

            RequestCloseDialogueWindow?.Invoke(channel);
        }

        /// <summary>
        /// Set how a dialogue channel's window looks, for every message shown on it from now on.
        /// </summary>
        private void SetDialogueWindowStyleOpcodeHandler(FieldScriptContext ctx)
        {
            byte                channel = ctx.NextByte();
            DialogueWindowStyle style   = (DialogueWindowStyle)ctx.NextByte();

            RequestSetDialogueWindowStyle?.Invoke(channel, style);
        }

        /// <summary>
        /// Set a value dialogue shows in place of <c>{Var n}</c>.
        /// </summary>
        private void SetMessageVariableOpcodeHandler(FieldScriptContext ctx)
        {
            byte slot  = ctx.NextByte();
            int  value = ctx.ReadInt();

            RequestSetMessageVariable?.Invoke(slot, value);
        }

        /// <summary>
        /// Ask the player to pick one of several answers on a channel and block until they have. The listener
        /// stores the answer through the callback, so a temp destination is this script's own.
        /// </summary>
        private void AskPlayerToMakeAChoiceOpcodeHandler(FieldScriptContext ctx)
        {
            AskPlayer(ctx, false);
        }

        private void AskPlayer(FieldScriptContext ctx, bool atAPoint)
        {
            int        instructionStart     = ctx.InstructionPointer - sizeof(ushort);
            byte       bank                 = ctx.NextByte();
            ushort     addressToStoreChoice = ctx.NextUshort();
            byte       channel              = ctx.NextByte();
            Vector2Int position             = atAPoint ? new Vector2Int(ctx.ReadInt(), ctx.ReadInt()) : default;
            ulong      dialogueId           = ctx.ReadName();
            byte    startingAnswer       = ctx.NextByte();
            byte    cancelAnswer         = ctx.NextByte();
            byte    answerCount          = ctx.NextByte();
            ulong[] answerIds            = new ulong[answerCount];

            for (int i = 0; i < answerCount; i++)
            {
                answerIds[i] = ctx.ReadName();
            }

            if (WaitForDialogueChannel(ctx, channel, instructionStart))
            {
                return;
            }

            MemoryBank choiceBank = (MemoryBank)bank;

            QuestionArgs question = new QuestionArgs(channel, dialogueId, answerIds, startingAnswer, cancelAnswer);

            if (atAPoint)
            {
                RequestFitDialogueWindow?.Invoke(channel, position);
            }

            RequestAskPlayerToMakeAChoice?.Invoke(question, choice => ctx.WriteVariableByte(choiceBank, addressToStoreChoice, choice));

            ctx.Block(new WaitUntilBlock(() => !IsDialogueWindowOpen(channel)));
        }

        /// <summary>
        /// Allow or block opening the main menu.
        /// </summary>
        private void MainMenuAccessibilityOpcodeHandler(FieldScriptContext ctx)
        {
            bool enabled = ctx.ReadBool();
            RequestSetMainMenuAccessibility?.Invoke(enabled);
        }

        /// <summary>
        /// Open the save menu, and end the frame, so the rest of the script runs once it has closed.
        /// </summary>
        private void OpenSaveMenuOpcodeHandler(FieldScriptContext ctx)
        {
            RequestOpenSaveMenu?.Invoke();

            ctx.YieldRequested = true;
        }

        /// <summary>
        /// As <c>SHOW_DIALOGUE_WINDOW_NO_WAIT</c>, but a press closes the window once it has typed, as one a script
        /// waits for.
        /// </summary>
        private void ShowDialogueWindowNoWaitDismissibleOpcodeHandler(FieldScriptContext ctx)
        {
            ShowDialogueWindowWithoutWaiting(ctx, true);
        }

        /// <summary>
        /// Show a line of dialogue at a point, in a window sized to its text, and block until the player dismisses it.
        /// </summary>
        private void ShowDialogueAtOpcodeHandler(FieldScriptContext ctx)
        {
            int   instructionStart = ctx.InstructionPointer - sizeof(ushort);
            byte  channel          = ctx.NextByte();
            ulong dialogueId       = ctx.ReadName();
            int   x                = ctx.ReadInt();
            int   y                = ctx.ReadInt();
            bool  blockMovement    = ctx.ReadBool();

            if (WaitForDialogueChannel(ctx, channel, instructionStart))
            {
                return;
            }

            RequestFitDialogueWindow?.Invoke(channel, new Vector2Int(x, y));
            RequestShowDialogueWindow?.Invoke(channel, dialogueId, blockMovement);

            ctx.Block(new WaitUntilBlock(() => !IsDialogueWindowOpen(channel)));
        }

        /// <summary>
        /// As <c>SHOW_DIALOGUE_WINDOW_NO_WAIT</c>, at a point in a window sized to its text.
        /// </summary>
        private void ShowDialogueAtNoWaitOpcodeHandler(FieldScriptContext ctx)
        {
            ShowDialogueAtWithoutWaiting(ctx, false);
        }

        /// <summary>
        /// As <c>SHOW_DIALOGUE_WINDOW_NO_WAIT_DISMISSIBLE</c>, at a point in a window sized to its text.
        /// </summary>
        private void ShowDialogueAtNoWaitDismissibleOpcodeHandler(FieldScriptContext ctx)
        {
            ShowDialogueAtWithoutWaiting(ctx, true);
        }

        private void ShowDialogueAtWithoutWaiting(FieldScriptContext ctx, bool dismissible)
        {
            int   instructionStart = ctx.InstructionPointer - sizeof(ushort);
            byte  channel          = ctx.NextByte();
            ulong dialogueId       = ctx.NextUlong();
            int   x                = ctx.ReadInt();
            int   y                = ctx.ReadInt();

            if (WaitForDialogueChannel(ctx, channel, instructionStart))
            {
                return;
            }

            RequestFitDialogueWindow?.Invoke(channel, new Vector2Int(x, y));
            RequestShowDialogueWindowNoWait?.Invoke(channel, dialogueId, dismissible);

            ctx.YieldRequested = true;
        }

        /// <summary>
        /// As <c>ASK_PLAYER_TO_MAKE_A_CHOICE</c>, at a point in a window sized to the question and its answers.
        /// </summary>
        private void AskPlayerAtOpcodeHandler(FieldScriptContext ctx)
        {
            AskPlayer(ctx, true);
        }

        /// <summary>
        /// Give the party money.
        /// </summary>
        private void AddMoneyOpcodeHandler(FieldScriptContext ctx)
        {
            int amount = ctx.ReadInt();

            RequestAddMoney?.Invoke(amount);
        }

        /// <summary>
        /// Take money from the party, which is adding a negative amount.
        /// </summary>
        private void SubtractMoneyOpcodeHandler(FieldScriptContext ctx)
        {
            int amount = ctx.ReadInt();

            RequestAddMoney?.Invoke(-amount);
        }

        /// <summary>
        /// Make this entity the player character.
        /// </summary>
        private void SetPlayerEntityOpcodeHandler(FieldScriptContext ctx)
        {
            RequestSetPlayerEntity?.Invoke(m_Entities[ctx.EntityId]);
        }

        /// <summary>
        /// Lock or unlock player input.
        /// </summary>
        private void LockInputOpcodeHandler(FieldScriptContext ctx)
        {
            bool inputLocked = ctx.ReadBool();
            RequestInputLock?.Invoke(inputLocked);
        }

        /// <summary>
        /// Show or hide this entity.
        /// </summary>
        private void VisibilityOpcodeHandler(FieldScriptContext ctx)
        {
            bool isVisible = ctx.ReadBool();
            RequestSetEntityVisible?.Invoke(ctx.EntityId, isVisible);
        }

        /// <summary>
        /// Say whether this entity is in the field this visit. The field reads it once every init script has run.
        /// </summary>
        private void EntityInFieldOpcodeHandler(FieldScriptContext ctx)
        {
            bool inField = ctx.ReadBool();

            RequestSetEntityInField?.Invoke(ctx.EntityId, inField);
        }

        /// <summary>
        /// Let other entities walk through this one, or block them again.
        /// </summary>
        private void EntitySolidityOpcodeHandler(FieldScriptContext ctx)
        {
            bool isSolid = ctx.ReadBool();
            RequestSetEntitySolid?.Invoke(ctx.EntityId, isSolid);
        }

        /// <summary>
        /// Let walking into this entity run its push script, or stop it.
        /// </summary>
        private void PushScriptActivationOpcodeHandler(FieldScriptContext ctx)
        {
            bool enabled = ctx.ReadBool();
            RequestSetPushScriptActive?.Invoke(ctx.EntityId, enabled);
        }

        /// <summary>
        /// Turn this entity's interaction trigger on or off.
        /// </summary>
        private void InteractabilityOpcodeHandler(FieldScriptContext ctx)
        {
            bool enabled = ctx.ReadBool();
            RequestSetInteractionTriggerActive?.Invoke(ctx.EntityId, enabled);
        }

        /// <summary>
        /// Set the radius around this entity within which the player can interact with it.
        /// </summary>
        private void SetInteractionRangeOpcodeHandler(FieldScriptContext ctx)
        {
            float radius = ctx.ReadFloat();
            RequestSetInteractionRange?.Invoke(ctx.EntityId, radius);
        }

        /// <summary>
        /// Place this entity at a position immediately.
        /// </summary>
        private void SetEntityPositionOpcodeHandler(FieldScriptContext ctx)
        {
            Vector3 position = new Vector3(ctx.ReadFloat(), ctx.ReadFloat(), ctx.ReadFloat());
            RequestSetEntityPosition?.Invoke(ctx.EntityId, position);
        }

        /// <summary>
        /// Shift where this entity is drawn, and carry on.
        /// </summary>
        private void SetEntityDrawOffsetOpcodeHandler(FieldScriptContext ctx)
        {
            RequestDrawOffset(ctx);
        }

        /// <summary>
        /// Wait until this entity's draw offset has finished moving.
        /// </summary>
        private void WaitForEntityDrawOffsetOpcodeHandler(FieldScriptContext ctx)
        {
            ctx.Block(new WaitUntilBlock(() => !IsEntityDrawOffsetMoving(ctx.EntityId)));
        }

        /// <summary>
        /// Set how fast this entity moves.
        /// </summary>
        private void SetMovementSpeedOpcodeHandler(FieldScriptContext ctx)
        {
            float movementSpeed = ctx.ReadFloat();
            RequestSetEntityMovementSpeed?.Invoke(ctx.EntityId, movementSpeed);
        }

        /// <summary>
        /// Walk this entity to a point, and wait until it arrives.
        /// </summary>
        private void MoveEntityAndWaitOpcodeHandler(FieldScriptContext ctx)
        {
            MoveEntityToPoint(ctx, MoveStyle.Walk, true);
        }

        /// <summary>
        /// Start this entity walking to a point, and carry on.
        /// </summary>
        private void MoveEntityOpcodeHandler(FieldScriptContext ctx)
        {
            MoveEntityToPoint(ctx, MoveStyle.Walk, false);
        }

        /// <summary>
        /// Move this entity to a point facing its way, without walking, and wait until it arrives.
        /// </summary>
        private void GlideEntityAndWaitOpcodeHandler(FieldScriptContext ctx)
        {
            MoveEntityToPoint(ctx, MoveStyle.Glide, true);
        }

        /// <summary>
        /// Start this entity moving to a point facing its way, without walking, and carry on.
        /// </summary>
        private void GlideEntityOpcodeHandler(FieldScriptContext ctx)
        {
            MoveEntityToPoint(ctx, MoveStyle.Glide, false);
        }

        /// <summary>
        /// Move this entity to a point without turning or walking, and wait until it arrives.
        /// </summary>
        private void SlideEntityAndWaitOpcodeHandler(FieldScriptContext ctx)
        {
            MoveEntityToPoint(ctx, MoveStyle.Slide, true);
        }

        /// <summary>
        /// Start this entity moving to a point without turning or walking, and carry on.
        /// </summary>
        private void SlideEntityOpcodeHandler(FieldScriptContext ctx)
        {
            MoveEntityToPoint(ctx, MoveStyle.Slide, false);
        }

        /// <summary>
        /// Walk this entity up to another, and wait until it arrives.
        /// </summary>
        private void MoveEntityToEntityAndWaitOpcodeHandler(FieldScriptContext ctx)
        {
            MoveEntityToEntity(ctx, MoveStyle.Walk, true);
        }

        /// <summary>
        /// Start this entity walking up to another, and carry on.
        /// </summary>
        private void MoveEntityToEntityOpcodeHandler(FieldScriptContext ctx)
        {
            MoveEntityToEntity(ctx, MoveStyle.Walk, false);
        }

        /// <summary>
        /// The eight moves differ only in how the entity carries itself, where it is going, and whether the script
        /// waits, so they read their arguments in two places.
        /// </summary>
        private void MoveEntityToPoint(FieldScriptContext ctx, MoveStyle style, bool waits)
        {
            float x = ctx.ReadFloat();
            float y = ctx.ReadFloat();
            float z = ctx.ReadFloat();

            MoveEntity(ctx, new MoveEntityArgs(new Vector3(x, y, z), FieldEntity.NO_ENTITY, 0f, style), waits);
        }

        private void MoveEntityToEntity(FieldScriptContext ctx, MoveStyle style, bool waits)
        {
            byte  targetEntityId = ctx.ReadByte();
            float stopDistance   = ctx.ReadFloat();

            MoveEntity(ctx, new MoveEntityArgs(Vector3.zero, targetEntityId, stopDistance, style), waits);
        }

        private void MoveEntity(FieldScriptContext ctx, MoveEntityArgs args, bool waits)
        {
            RequestMoveEntity?.Invoke(ctx.EntityId, args);

            if (waits)
            {
                ctx.Block(new WaitUntilBlock(() => !IsEntityMoving(ctx.EntityId)));
            }
        }

        /// <summary>
        /// Jump this entity to a point and block until it lands.
        /// </summary>
        private void JumpEntityAndWaitOpcodeHandler(FieldScriptContext ctx)
        {
            float x       = ctx.ReadFloat();
            float y       = ctx.ReadFloat();
            float z       = ctx.ReadFloat();
            float seconds = ctx.ReadFloat();
            float height  = ctx.ReadFloat();

            RequestJumpEntity?.Invoke(ctx.EntityId, new Vector3(x, y, z), seconds, height);

            ctx.Block(new WaitUntilBlock(() => !IsEntityJumping(ctx.EntityId)));
        }

        /// <summary>
        /// Take this entity over a ladder and block until it is off.
        /// </summary>
        private void ClimbLadderAndWaitOpcodeHandler(FieldScriptContext ctx)
        {
            bool    up        = ctx.ReadBool();
            Vector3 onLadder  = ReadPoint(ctx);
            Vector3 offLadder = ReadPoint(ctx);
            Vector3 finish    = ReadPoint(ctx);

            RequestClimbLadder?.Invoke(ctx.EntityId, new LadderClimbArgs(up, onLadder, offLadder, finish));

            ctx.Block(new WaitUntilBlock(() => !IsEntityClimbing(ctx.EntityId)));
        }

        /// <summary>
        /// Wait until this entity has arrived where a script sent it.
        /// </summary>
        private void WaitForMovementOpcodeHandler(FieldScriptContext ctx)
        {
            ctx.Block(new WaitUntilBlock(() => !IsEntityMoving(ctx.EntityId)));
        }

        /// <summary>
        /// Stop this entity where it is.
        /// </summary>
        private void StopMovementOpcodeHandler(FieldScriptContext ctx)
        {
            RequestStopMovement?.Invoke(ctx.EntityId);
        }

        /// <summary>
        /// Set this entity's rotation immediately, from Euler angles.
        /// </summary>
        private void SetEntityRotationOpcodeHandler(FieldScriptContext ctx)
        {
            float x = ctx.ReadFloat();
            float y = ctx.ReadFloat();
            float z = ctx.ReadFloat();

            RequestSetEntityRotation?.Invoke(ctx.EntityId, Quaternion.Euler(x, y, z));
        }

        /// <summary>
        /// Turn this entity to a rotation over time and block until it finishes.
        /// </summary>
        private void SetEntityRotationOverTimeOpcodeHandler(FieldScriptContext ctx)
        {
            float             x            = ctx.ReadFloat();
            float             y            = ctx.ReadFloat();
            float             z            = ctx.ReadFloat();
            RotationDirection direction    = (RotationDirection)ctx.ReadByte();
            float             duration     = ctx.ReadFloat();
            Easing            rotationType = (Easing)ctx.ReadByte();

            SetEntityRotationAsyncArgs args = new SetEntityRotationAsyncArgs(Quaternion.Euler(x, y, z), direction, duration, rotationType);

            RequestSetEntityRotationAsync?.Invoke(ctx.EntityId, args);

            ctx.Block(new WaitUntilBlock(() => !IsEntityRotating(ctx.EntityId)));
        }

        /// <summary>
        /// Turn this entity to face another entity.
        /// </summary>
        private void SetDirectionToFaceEntityOpcodeHandler(FieldScriptContext ctx)
        {
            byte targetEntityId = ctx.ReadByte();
            RequestSetEntityToFaceEntity?.Invoke(ctx.EntityId, targetEntityId);
        }

        /// <summary>
        /// Face this entity towards a point.
        /// </summary>
        private void SetDirectionToFacePositionOpcodeHandler(FieldScriptContext ctx)
        {
            float x = ctx.ReadFloat();
            float y = ctx.ReadFloat();
            float z = ctx.ReadFloat();

            RequestSetEntityToFacePosition?.Invoke(ctx.EntityId, new Vector3(x, y, z));
        }

        /// <summary>
        /// Turn this entity over time to face another and block until it has.
        /// </summary>
        private void TurnToEntityAndWaitOpcodeHandler(FieldScriptContext ctx)
        {
            byte   targetEntityId = ctx.ReadByte();
            float  duration       = ctx.ReadFloat();
            Easing rotationType   = (Easing)ctx.ReadByte();

            RequestTurnEntityToEntity?.Invoke(ctx.EntityId, targetEntityId, duration, rotationType);

            ctx.Block(new WaitUntilBlock(() => !IsEntityRotating(ctx.EntityId)));
        }

        /// <summary>
        /// Set what this entity animates as when no script is animating it.
        /// </summary>
        private void SetBaseAnimationOpcodeHandler(FieldScriptContext ctx)
        {
            ulong stateNameHash = ctx.ReadName();

            RequestSetBaseAnimation?.Invoke(ctx.EntityId, stateNameHash);
        }

        /// <summary>
        /// Name the animations this entity climbs ladders with.
        /// </summary>
        private void SetLadderAnimationsOpcodeHandler(FieldScriptContext ctx)
        {
            ulong getOn  = ctx.ReadName();
            ulong climb  = ctx.ReadName();
            ulong getOff = ctx.ReadName();

            RequestSetLadderAnimations?.Invoke(ctx.EntityId, getOn, climb, getOff);
        }

        /// <summary>
        /// Play an animation, wait for it, then go back to the base animation.
        /// </summary>
        private void PlayAnimationOnceAndWaitOpcodeHandler(FieldScriptContext ctx)
        {
            PlayAnimation(ctx, AnimationPlayMode.ReturnToBase, false, true);
        }

        /// <summary>
        /// Play an animation, wait for it, then hold its last frame.
        /// </summary>
        private void PlayAnimationStopOnLastFrameWaitOpcodeHandler(FieldScriptContext ctx)
        {
            PlayAnimation(ctx, AnimationPlayMode.HoldLastFrame, false, true);
        }

        /// <summary>
        /// Start an animation and carry on.
        /// </summary>
        private void PlayAnimationOnceAsyncOpcodeHandler(FieldScriptContext ctx)
        {
            PlayAnimation(ctx, AnimationPlayMode.ReturnToBase, false, false);
        }

        /// <summary>
        /// Start an animation that holds its last frame, and carry on.
        /// </summary>
        private void PlayAnimationHoldingAsyncOpcodeHandler(FieldScriptContext ctx)
        {
            PlayAnimation(ctx, AnimationPlayMode.HoldLastFrame, false, false);
        }

        /// <summary>
        /// Start an animation looping, and carry on.
        /// </summary>
        private void PlayAnimationLoopingOpcodeHandler(FieldScriptContext ctx)
        {
            PlayAnimation(ctx, AnimationPlayMode.Loop, false, false);
        }

        /// <summary>
        /// Play part of an animation, wait for it, then go back to the base animation.
        /// </summary>
        private void PlayPartialAnimationAndWaitOpcodeHandler(FieldScriptContext ctx)
        {
            PlayAnimation(ctx, AnimationPlayMode.ReturnToBase, true, true);
        }

        /// <summary>
        /// Play part of an animation, wait for it, then hold its last frame.
        /// </summary>
        private void PlayPartialAnimationAndHoldOpcodeHandler(FieldScriptContext ctx)
        {
            PlayAnimation(ctx, AnimationPlayMode.HoldLastFrame, true, true);
        }

        /// <summary>
        /// Start part of an animation and carry on.
        /// </summary>
        private void PlayPartialAnimationAsyncOpcodeHandler(FieldScriptContext ctx)
        {
            PlayAnimation(ctx, AnimationPlayMode.ReturnToBase, true, false);
        }

        /// <summary>
        /// Start part of an animation that holds its last frame, and carry on.
        /// </summary>
        private void PlayPartialAnimationHoldingAsyncOpcodeHandler(FieldScriptContext ctx)
        {
            PlayAnimation(ctx, AnimationPlayMode.HoldLastFrame, true, false);
        }

        /// <summary>
        /// Start part of an animation looping, and carry on.
        /// </summary>
        private void PlayPartialAnimationLoopingOpcodeHandler(FieldScriptContext ctx)
        {
            PlayAnimation(ctx, AnimationPlayMode.Loop, true, false);
        }

        /// <summary>
        /// The ten play opcodes differ only in what happens at the end, whether a range was given, and whether the
        /// script waits, so they read their arguments in one place.
        /// </summary>
        private void PlayAnimation(FieldScriptContext ctx, AnimationPlayMode mode, bool partial, bool waits)
        {
            ulong nameHash = ctx.NextUlong();
            float from     = partial ? ctx.ReadFloat() : 0f;
            float to       = partial ? ctx.ReadFloat() : 1f;

            RequestPlayAnimation?.Invoke(ctx.EntityId, new PlayAnimationArgs(nameHash, mode, from, to));

            if (waits)
            {
                ctx.Block(new WaitUntilBlock(() => !IsEntityAnimating(ctx.EntityId)));
            }
        }

        /// <summary>
        /// Scale how fast this entity's animation plays.
        /// </summary>
        private void SetAnimationSpeedOpcodeHandler(FieldScriptContext ctx)
        {
            float multiplier = ctx.ReadFloat();

            RequestSetAnimationSpeed?.Invoke(ctx.EntityId, multiplier);
        }

        /// <summary>
        /// Wait until the animation this entity is playing has reached its end.
        /// </summary>
        private void WaitForAnimationOpcodeHandler(FieldScriptContext ctx)
        {
            ctx.Block(new WaitUntilBlock(() => !IsEntityAnimating(ctx.EntityId)));
        }

        /// <summary>
        /// Drop whatever this entity is playing and return it to its base animation.
        /// </summary>
        private void StopAnimationOpcodeHandler(FieldScriptContext ctx)
        {
            RequestStopAnimation?.Invoke(ctx.EntityId);
        }

        /// <summary>
        /// Remember what this entity is playing and where it has got to.
        /// </summary>
        private void PushAnimationStateOpcodeHandler(FieldScriptContext ctx)
        {
            RequestPushAnimationState?.Invoke(ctx.EntityId);
        }

        /// <summary>
        /// Put back what <c>PUSH_ANIMATION_STATE</c> remembered.
        /// </summary>
        private void PopAnimationStateOpcodeHandler(FieldScriptContext ctx)
        {
            RequestPopAnimationState?.Invoke(ctx.EntityId);
        }

        /// <summary>
        /// Store where an entity is.
        /// </summary>
        private void GetEntityPositionOpcodeHandler(FieldScriptContext ctx)
        {
            byte                   entityId = ctx.ReadByte();
            FieldScriptDestination x        = ctx.ReadDestination();
            FieldScriptDestination y        = ctx.ReadDestination();
            FieldScriptDestination z        = ctx.ReadDestination();
            Vector3                position = GetEntityPosition(entityId);

            ctx.WriteFloat(x, position.x);
            ctx.WriteFloat(y, position.y);
            ctx.WriteFloat(z, position.z);
        }

        /// <summary>
        /// Store which way an entity faces, in degrees.
        /// </summary>
        private void GetEntityDirectionOpcodeHandler(FieldScriptContext ctx)
        {
            byte                   entityId    = ctx.ReadByte();
            FieldScriptDestination destination = ctx.ReadDestination();

            ctx.WriteFloat(destination, GetEntityDirection(entityId));
        }

        /// <summary>
        /// Put this entity where another stands, facing the way it faces.
        /// </summary>
        private void CopyEntityPoseOpcodeHandler(FieldScriptContext ctx)
        {
            byte sourceEntityId = ctx.ReadByte();

            RequestCopyEntityPose?.Invoke(ctx.EntityId, sourceEntityId);
        }

        /// <summary>
        /// Store whether another entity is within this one's interaction range.
        /// </summary>
        private void IsEntityInRangeOpcodeHandler(FieldScriptContext ctx)
        {
            byte                   otherEntityId = ctx.ReadByte();
            FieldScriptDestination destination   = ctx.ReadDestination();

            ctx.WriteBool(destination, IsEntityInRange(ctx.EntityId, otherEntityId));
        }

        /// <summary>
        /// Turn this entity's collision trigger on or off.
        /// </summary>
        private void CollisionTriggerActivationOpcodeHandler(FieldScriptContext ctx)
        {
            bool enabled = ctx.ReadBool();
            RequestSetCollisionTriggerActive?.Invoke(ctx.EntityId, enabled);
        }

        /// <summary>
        /// Shift where this entity is drawn and block until it has finished.
        /// </summary>
        private void SetEntityDrawOffsetAndWaitOpcodeHandler(FieldScriptContext ctx)
        {
            RequestDrawOffset(ctx);

            ctx.Block(new WaitUntilBlock(() => !IsEntityDrawOffsetMoving(ctx.EntityId)));
        }

        private void RequestDrawOffset(FieldScriptContext ctx)
        {
            Vector3 offset  = ReadPoint(ctx);
            float   seconds = ctx.ReadFloat();
            Easing  easing  = (Easing)ctx.ReadByte();

            RequestSetEntityDrawOffset?.Invoke(ctx.EntityId, offset, seconds, easing);
        }

        private static Vector3 ReadPoint(FieldScriptContext ctx)
        {
            float x = ctx.ReadFloat();
            float y = ctx.ReadFloat();
            float z = ctx.ReadFloat();

            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Take a colour from the screen beneath the message windows, or add one, fading to it over the given seconds.
        /// </summary>
        private void ScreenColourOpcodeHandler(FieldScriptContext ctx, bool add)
        {
            float red     = ctx.ReadFloat();
            float green   = ctx.ReadFloat();
            float blue    = ctx.ReadFloat();
            float seconds = ctx.ReadFloat();

            RequestScreenColour?.Invoke(new Color(red, green, blue), add, seconds);
        }

        /// <summary>
        /// Wait until the screen's colour has reached the colour it was sent to.
        /// </summary>
        private void WaitForScreenColourOpcodeHandler(FieldScriptContext ctx)
        {
            ctx.Block(new WaitUntilBlock(() => !IsScreenColourFading()));
        }

        /// <summary>
        /// Close part of the field with one of its blockers, or open it again.
        /// </summary>
        private void BlockerActivationOpcodeHandler(FieldScriptContext ctx)
        {
            byte blocker    = ctx.ReadByte();
            bool isBlocking = ctx.ReadBool();
            RequestSetBlockerActive?.Invoke(blocker, isBlocking);
        }

        /// <summary>
        /// Fade the screen beneath the message windows out or back in, and carry on.
        /// </summary>
        private void FadeScreenOpcodeHandler(FieldScriptContext ctx)
        {
            bool  fadeOut    = ctx.ReadBool();
            bool  atOnce     = ctx.ReadBool();
            ulong transition = ctx.ReadName();

            RequestScreenFade?.Invoke(fadeOut, atOnce, transition);
        }

        /// <summary>
        /// Fade the screen beneath the message windows out or back in, and wait until it has.
        /// </summary>
        private void FadeScreenAndWaitOpcodeHandler(FieldScriptContext ctx)
        {
            FadeScreenOpcodeHandler(ctx);
            WaitForFadeOpcodeHandler(ctx);
        }

        /// <summary>
        /// Wait until the screen has faded out or back in.
        /// </summary>
        private void WaitForFadeOpcodeHandler(FieldScriptContext ctx)
        {
            ctx.Block(new WaitUntilBlock(() => !IsScreenFading()));
        }

        /// <summary>
        /// Move the view onto a point, where it stays until another scroll.
        /// </summary>
        private void ScrollScreenToPositionOpcodeHandler(FieldScriptContext ctx)
        {
            float  x       = ctx.ReadFloat();
            float  y       = ctx.ReadFloat();
            float  z       = ctx.ReadFloat();
            float  seconds = ctx.ReadFloat();
            Easing easing  = (Easing)ctx.ReadByte();

            RequestScrollScreenToPosition?.Invoke(new Vector3(x, y, z), seconds, easing);
        }

        /// <summary>
        /// Move the view onto an entity, and follow it; onto the player, the view follows the player again.
        /// </summary>
        private void ScrollScreenToEntityOpcodeHandler(FieldScriptContext ctx)
        {
            byte   targetEntityId = ctx.ReadByte();
            float  seconds        = ctx.ReadFloat();
            Easing easing         = (Easing)ctx.ReadByte();

            RequestScrollScreenToEntity?.Invoke(targetEntityId, seconds, easing);
        }

        /// <summary>
        /// Wait until the view has arrived where a script scrolled it.
        /// </summary>
        private void WaitForScrollingOpcodeHandler(FieldScriptContext ctx)
        {
            ctx.Block(new WaitUntilBlock(() => !IsScreenScrolling()));
        }

        /// <summary>
        /// Look through another of the field's cameras, cutting to it or blending over seconds.
        /// </summary>
        private void SetCameraOpcodeHandler(FieldScriptContext ctx)
        {
            byte   camera  = ctx.ReadByte();
            float  seconds = ctx.ReadFloat();
            Easing easing  = (Easing)ctx.ReadByte();

            RequestSetCamera?.Invoke(camera, seconds, easing);
        }

        /// <summary>
        /// Play a music track, starting in one of its stem states.
        /// </summary>
        private void PlayMusicOpcodeHandler(FieldScriptContext ctx)
        {
            ulong nameHash      = ctx.ReadName();
            ulong stateNameHash = ctx.ReadName();

            RequestMusic?.Invoke(nameHash, stateNameHash);
        }

        /// <summary>
        /// Stop the music at once.
        /// </summary>
        private void StopMusicOpcodeHandler(FieldScriptContext ctx)
        {
            RequestStopMusic?.Invoke();
        }

        /// <summary>
        /// Set the volume the field plays its music at.
        /// </summary>
        private void SetMusicVolumeOpcodeHandler(FieldScriptContext ctx)
        {
            float volume = ctx.ReadFloat();

            RequestMusicVolume?.Invoke(volume, 0f);
        }

        /// <summary>
        /// Fade the field's music to a volume over the given seconds.
        /// </summary>
        private void FadeMusicVolumeOpcodeHandler(FieldScriptContext ctx)
        {
            float volume  = ctx.ReadFloat();
            float seconds = ctx.ReadFloat();

            RequestMusicVolume?.Invoke(volume, seconds);
        }

        /// <summary>
        /// Switch the playing music to one of its stem states, fading over the given number of seconds.
        /// </summary>
        private void SetMusicStemStateOpcodeHandler(FieldScriptContext ctx)
        {
            ulong stateNameHash = ctx.ReadName();
            float fadeSeconds   = ctx.ReadFloat();

            RequestMusicStemState?.Invoke(stateNameHash, fadeSeconds);
        }

        /// <summary>
        /// Store whether music is playing, or still loading to play.
        /// </summary>
        private void CheckIfMusicIsPlayingOpcodeHandler(FieldScriptContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            bool playing = IsMusicPlaying();

            ctx.WriteVariableBool(bank, address, playing);
        }

        /// <summary>
        /// Set the track every battle plays.
        /// </summary>
        private void SetBattleMusicOpcodeHandler(FieldScriptContext ctx)
        {
            ulong nameHash = ctx.ReadName();

            RequestBattleMusic?.Invoke(nameHash);
        }

        /// <summary>
        /// Play a sound effect, on a channel if the script gives one.
        /// </summary>
        private void PlaySoundOpcodeHandler(FieldScriptContext ctx)
        {
            ulong nameHash = ctx.ReadName();
            float volume   = ctx.ReadFloat();
            float pan      = ctx.ReadFloat();
            byte  channel  = ctx.NextByte();

            RequestSfx?.Invoke(nameHash, volume, pan, channel);
        }

        /// <summary>
        /// Set every sound effect's volume at once.
        /// </summary>
        private void SetAllSoundVolumeOpcodeHandler(FieldScriptContext ctx)
        {
            float volume = ctx.ReadFloat();

            RequestFadeAllSoundVolume?.Invoke(volume, 0f);
        }

        /// <summary>
        /// Fade every sound effect's volume.
        /// </summary>
        private void FadeAllSoundVolumeOpcodeHandler(FieldScriptContext ctx)
        {
            float volume  = ctx.ReadFloat();
            float seconds = ctx.ReadFloat();

            RequestFadeAllSoundVolume?.Invoke(volume, seconds);
        }

        /// <summary>
        /// Switch the PS1 reverb to a preset.
        /// </summary>
        private void SetReverbPresetOpcodeHandler(FieldScriptContext ctx)
        {
            byte preset = ctx.ReadByte();

            RequestReverbPreset?.Invoke(preset);
        }

        /// <summary>
        /// Set how loud the PS1 reverb plays, 0 to 1.
        /// </summary>
        private void SetReverbVolumeOpcodeHandler(FieldScriptContext ctx)
        {
            float volume = ctx.ReadFloat();

            RequestReverbVolume?.Invoke(volume);
        }

        /// <summary>
        /// Fade the playing music out and a track in, starting in one of its stem states, over the given seconds, to the
        /// given volume.
        /// </summary>
        private void CrossfadeMusicOpcodeHandler(FieldScriptContext ctx)
        {
            ulong nameHash      = ctx.ReadName();
            ulong stateNameHash = ctx.ReadName();
            float seconds       = ctx.ReadFloat();
            float volume        = ctx.ReadFloat();

            RequestMusicCrossfade?.Invoke(nameHash, stateNameHash, seconds, volume);
        }

        /// <summary>
        /// Fade the field's music from one volume to another over the given seconds.
        /// </summary>
        private void FadeMusicVolumeFromOpcodeHandler(FieldScriptContext ctx)
        {
            float from    = ctx.ReadFloat();
            float volume  = ctx.ReadFloat();
            float seconds = ctx.ReadFloat();

            RequestMusicVolumeFrom?.Invoke(from, volume, seconds);
        }

        /// <summary>
        /// Wait until the field's music has finished fading to a volume.
        /// </summary>
        private void WaitForMusicVolumeOpcodeHandler(FieldScriptContext ctx)
        {
            ctx.Block(new WaitUntilBlock(() => !IsMusicVolumeFading()));
        }

        /// <summary>
        /// Stop the sound a channel holds.
        /// </summary>
        private void StopSoundOpcodeHandler(FieldScriptContext ctx)
        {
            byte channel = ctx.NextByte();

            RequestStopSound?.Invoke(channel);
        }

        /// <summary>
        /// Take the volume of the sound a channel holds to a new level.
        /// </summary>
        private void FadeSoundVolumeOpcodeHandler(FieldScriptContext ctx)
        {
            byte  channel = ctx.NextByte();
            float volume  = ctx.ReadFloat();
            float seconds = ctx.ReadFloat();

            RequestFadeSoundVolume?.Invoke(channel, volume, seconds);
        }

        /// <summary>
        /// Take the pan of the sound a channel holds to a new place.
        /// </summary>
        private void FadeSoundPanOpcodeHandler(FieldScriptContext ctx)
        {
            byte  channel = ctx.NextByte();
            float pan     = ctx.ReadFloat();
            float seconds = ctx.ReadFloat();

            RequestFadeSoundPan?.Invoke(channel, pan, seconds);
        }

        /// <summary>
        /// Set the countdown's seconds left.
        /// </summary>
        private void SetCountdownTimerOpcodeHandler(FieldScriptContext ctx)
        {
            int seconds = ctx.ReadInt();

            RequestSetCountdown?.Invoke(seconds);
        }

        /// <summary>
        /// Show the countdown at a place on screen.
        /// </summary>
        private void ShowCountdownTimerOpcodeHandler(FieldScriptContext ctx)
        {
            int x = ctx.ReadInt();
            int y = ctx.ReadInt();

            RequestShowCountdown?.Invoke(x, y);
        }

        /// <summary>
        /// Store the countdown's seconds left.
        /// </summary>
        private void GetCountdownTimerOpcodeHandler(FieldScriptContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            ctx.WriteVariableInt(bank, address, GetCountdown());
        }

        /// <summary>
        /// Take the countdown off the screen.
        /// </summary>
        private void HideCountdownTimerOpcodeHandler(FieldScriptContext ctx)
        {
            RequestHideCountdown?.Invoke();
        }

        /// <summary>
        /// Wait until the player presses a button, named by its action, counting only a press made after the wait began.
        /// </summary>
        private void WaitForButtonOpcodeHandler(FieldScriptContext ctx)
        {
            ulong button = ctx.ReadName();

            Func<bool> pressed = StartWaitingForButton(button);

            ctx.Block(new WaitUntilBlock(pressed));
        }

        /// <summary>An entity's priority slot, which holds one running script's context at a time.</summary>
        private readonly struct SlotKey : IEquatable<SlotKey>
        {
            private readonly int  m_EntityId;
            private readonly byte m_Priority;

            internal SlotKey(int entityId, byte priority)
            {
                m_EntityId = entityId;
                m_Priority = priority;
            }

            bool IEquatable<SlotKey>.Equals(SlotKey other)
            {
                return m_EntityId == other.m_EntityId && m_Priority == other.m_Priority;
            }

            public override bool Equals(object obj)
            {
                return obj is SlotKey other && m_EntityId == other.m_EntityId && m_Priority == other.m_Priority;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(m_EntityId, m_Priority);
            }
        }
    }
}
