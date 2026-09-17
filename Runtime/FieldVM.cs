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
                       // { FieldScriptOpCode.RunPartyMemberScriptUnlessBusy, RunPartyMemberScriptUnlessBusyOpcodeHandler },
                       // { FieldScriptOpCode.RunPartyMemberScriptWaitUntilStarted, RunPartyMemberScriptWaitUntilStartedOpcodeHandler },
                       // { FieldScriptOpCode.RunPartyMemberScriptWaitUntilFinished, RunPartyMemberScriptWaitUntilFinishedOpcodeHandler },
                       { FieldScriptOpCode.ReturnToAnotherScript, ReturnToAnotherScriptOpcodeHandler },
                       // { FieldScriptOpCode.CallAnotherScript, CallAnotherScriptOpcodeHandler },
                       // { FieldScriptOpCode.ReturnFromCall, ReturnFromCallOpcodeHandler },
                       { FieldScriptOpCode.GotoJump, GotoOpcodeHandler },
                       { FieldScriptOpCode.GotoDirectly, GotoDirectlyOpcodeHandler },
                       { FieldScriptOpCode.CompareBool, ctx => CompareOpcodeHandler(ctx, VariableWidth.Bool) },
                       { FieldScriptOpCode.CompareSByte, ctx => CompareOpcodeHandler(ctx, VariableWidth.SByte) },
                       { FieldScriptOpCode.CompareByte, ctx => CompareOpcodeHandler(ctx, VariableWidth.Byte) },
                       { FieldScriptOpCode.CompareShort, ctx => CompareOpcodeHandler(ctx, VariableWidth.Short) },
                       { FieldScriptOpCode.CompareUShort, ctx => CompareOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.CompareInt, ctx => CompareOpcodeHandler(ctx, VariableWidth.Int) },
                       { FieldScriptOpCode.CompareUInt, ctx => CompareOpcodeHandler(ctx, VariableWidth.UInt) },
                       { FieldScriptOpCode.CompareLong, ctx => CompareOpcodeHandler(ctx, VariableWidth.Long) },
                       { FieldScriptOpCode.CompareULong, ctx => CompareOpcodeHandler(ctx, VariableWidth.ULong) },
                       { FieldScriptOpCode.CompareFloat, ctx => CompareOpcodeHandler(ctx, VariableWidth.Float) },
                       { FieldScriptOpCode.Yield, YieldOpcodeHandler },
                       { FieldScriptOpCode.WaitSeconds, WaitSecondsOpcodeHandler },
                       // { FieldScriptOpCode.HaltScript, HaltScriptOpcodeHandler },
                       { FieldScriptOpCode.DoNothing, DoNothingOpcodeHandler },
                       // { FieldScriptOpCode.DebugLog, DebugLogOpcodeHandler },

                       // System and module control
                       { FieldScriptOpCode.JumpToAnotherMap, JumpToAnotherMapOpcodeHandler },
                       // { FieldScriptOpCode.SetJumpFieldID, SetJumpFieldIDOpcodeHandler },
                       { FieldScriptOpCode.GatewayTriggerActivation, GatewayTriggerActivationOpcodeHandler },
                       // { FieldScriptOpCode.SetFieldExitFade, SetFieldExitFadeOpcodeHandler },
                       // { FieldScriptOpCode.WorldMapJump, WorldMapJumpOpcodeHandler },
                       { FieldScriptOpCode.SetBattleModeOptions, SetBattleModeOptionsOpcodeHandler },
                       { FieldScriptOpCode.StartBattle, StartBattleOpcodeHandler },
                       // { FieldScriptOpCode.LoadResultOfLastBattle, LoadResultOfLastBattleOpcodeHandler },
                       // { FieldScriptOpCode.RandomEncounters, RandomEncountersOpcodeHandler },
                       // { FieldScriptOpCode.GameOver, GameOverOpcodeHandler },
                       // { FieldScriptOpCode.SetSaveEnabled, SetSaveEnabledOpcodeHandler },

                       // Assignment and mathematics
                       { FieldScriptOpCode.SetBool, ctx => SetOpcodeHandler(ctx, VariableWidth.Bool) },
                       { FieldScriptOpCode.SetSByte, ctx => SetOpcodeHandler(ctx, VariableWidth.SByte) },
                       { FieldScriptOpCode.SetByte, ctx => SetOpcodeHandler(ctx, VariableWidth.Byte) },
                       { FieldScriptOpCode.SetShort, ctx => SetOpcodeHandler(ctx, VariableWidth.Short) },
                       { FieldScriptOpCode.SetUShort, ctx => SetOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.SetInt, ctx => SetOpcodeHandler(ctx, VariableWidth.Int) },
                       { FieldScriptOpCode.SetUInt, ctx => SetOpcodeHandler(ctx, VariableWidth.UInt) },
                       { FieldScriptOpCode.SetLong, ctx => SetOpcodeHandler(ctx, VariableWidth.Long) },
                       { FieldScriptOpCode.SetULong, ctx => SetOpcodeHandler(ctx, VariableWidth.ULong) },
                       { FieldScriptOpCode.SetFloat, ctx => SetOpcodeHandler(ctx, VariableWidth.Float) },
                       { FieldScriptOpCode.AddSByte, ctx => AddOpcodeHandler(ctx, VariableWidth.SByte) },
                       { FieldScriptOpCode.AddByte, ctx => AddOpcodeHandler(ctx, VariableWidth.Byte) },
                       { FieldScriptOpCode.AddShort, ctx => AddOpcodeHandler(ctx, VariableWidth.Short) },
                       { FieldScriptOpCode.AddUShort, ctx => AddOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.AddInt, ctx => AddOpcodeHandler(ctx, VariableWidth.Int) },
                       { FieldScriptOpCode.AddUInt, ctx => AddOpcodeHandler(ctx, VariableWidth.UInt) },
                       { FieldScriptOpCode.AddLong, ctx => AddOpcodeHandler(ctx, VariableWidth.Long) },
                       { FieldScriptOpCode.AddULong, ctx => AddOpcodeHandler(ctx, VariableWidth.ULong) },
                       { FieldScriptOpCode.AddFloat, ctx => AddOpcodeHandler(ctx, VariableWidth.Float) },
                       { FieldScriptOpCode.AddSByteClamped, ctx => AddClampedOpcodeHandler(ctx, VariableWidth.SByte) },
                       { FieldScriptOpCode.AddByteClamped, ctx => AddClampedOpcodeHandler(ctx, VariableWidth.Byte) },
                       { FieldScriptOpCode.AddShortClamped, ctx => AddClampedOpcodeHandler(ctx, VariableWidth.Short) },
                       { FieldScriptOpCode.AddUShortClamped, ctx => AddClampedOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.AddIntClamped, ctx => AddClampedOpcodeHandler(ctx, VariableWidth.Int) },
                       { FieldScriptOpCode.AddUIntClamped, ctx => AddClampedOpcodeHandler(ctx, VariableWidth.UInt) },
                       { FieldScriptOpCode.AddLongClamped, ctx => AddClampedOpcodeHandler(ctx, VariableWidth.Long) },
                       { FieldScriptOpCode.AddULongClamped, ctx => AddClampedOpcodeHandler(ctx, VariableWidth.ULong) },
                       { FieldScriptOpCode.SubtractSByte, ctx => SubtractOpcodeHandler(ctx, VariableWidth.SByte) },
                       { FieldScriptOpCode.SubtractByte, ctx => SubtractOpcodeHandler(ctx, VariableWidth.Byte) },
                       { FieldScriptOpCode.SubtractShort, ctx => SubtractOpcodeHandler(ctx, VariableWidth.Short) },
                       { FieldScriptOpCode.SubtractUShort, ctx => SubtractOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.SubtractInt, ctx => SubtractOpcodeHandler(ctx, VariableWidth.Int) },
                       { FieldScriptOpCode.SubtractUInt, ctx => SubtractOpcodeHandler(ctx, VariableWidth.UInt) },
                       { FieldScriptOpCode.SubtractLong, ctx => SubtractOpcodeHandler(ctx, VariableWidth.Long) },
                       { FieldScriptOpCode.SubtractULong, ctx => SubtractOpcodeHandler(ctx, VariableWidth.ULong) },
                       { FieldScriptOpCode.SubtractFloat, ctx => SubtractOpcodeHandler(ctx, VariableWidth.Float) },
                       { FieldScriptOpCode.SubtractSByteClamped, ctx => SubtractClampedOpcodeHandler(ctx, VariableWidth.SByte) },
                       { FieldScriptOpCode.SubtractByteClamped, ctx => SubtractClampedOpcodeHandler(ctx, VariableWidth.Byte) },
                       { FieldScriptOpCode.SubtractShortClamped, ctx => SubtractClampedOpcodeHandler(ctx, VariableWidth.Short) },
                       { FieldScriptOpCode.SubtractUShortClamped, ctx => SubtractClampedOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.SubtractIntClamped, ctx => SubtractClampedOpcodeHandler(ctx, VariableWidth.Int) },
                       { FieldScriptOpCode.SubtractUIntClamped, ctx => SubtractClampedOpcodeHandler(ctx, VariableWidth.UInt) },
                       { FieldScriptOpCode.SubtractLongClamped, ctx => SubtractClampedOpcodeHandler(ctx, VariableWidth.Long) },
                       { FieldScriptOpCode.SubtractULongClamped, ctx => SubtractClampedOpcodeHandler(ctx, VariableWidth.ULong) },
                       { FieldScriptOpCode.MultiplySByte, ctx => MultiplyOpcodeHandler(ctx, VariableWidth.SByte) },
                       { FieldScriptOpCode.MultiplyByte, ctx => MultiplyOpcodeHandler(ctx, VariableWidth.Byte) },
                       { FieldScriptOpCode.MultiplyShort, ctx => MultiplyOpcodeHandler(ctx, VariableWidth.Short) },
                       { FieldScriptOpCode.MultiplyUShort, ctx => MultiplyOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.MultiplyInt, ctx => MultiplyOpcodeHandler(ctx, VariableWidth.Int) },
                       { FieldScriptOpCode.MultiplyUInt, ctx => MultiplyOpcodeHandler(ctx, VariableWidth.UInt) },
                       { FieldScriptOpCode.MultiplyLong, ctx => MultiplyOpcodeHandler(ctx, VariableWidth.Long) },
                       { FieldScriptOpCode.MultiplyULong, ctx => MultiplyOpcodeHandler(ctx, VariableWidth.ULong) },
                       { FieldScriptOpCode.MultiplyFloat, ctx => MultiplyOpcodeHandler(ctx, VariableWidth.Float) },
                       { FieldScriptOpCode.DivideSByte, ctx => DivideOpcodeHandler(ctx, VariableWidth.SByte) },
                       { FieldScriptOpCode.DivideByte, ctx => DivideOpcodeHandler(ctx, VariableWidth.Byte) },
                       { FieldScriptOpCode.DivideShort, ctx => DivideOpcodeHandler(ctx, VariableWidth.Short) },
                       { FieldScriptOpCode.DivideUShort, ctx => DivideOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.DivideInt, ctx => DivideOpcodeHandler(ctx, VariableWidth.Int) },
                       { FieldScriptOpCode.DivideUInt, ctx => DivideOpcodeHandler(ctx, VariableWidth.UInt) },
                       { FieldScriptOpCode.DivideLong, ctx => DivideOpcodeHandler(ctx, VariableWidth.Long) },
                       { FieldScriptOpCode.DivideULong, ctx => DivideOpcodeHandler(ctx, VariableWidth.ULong) },
                       { FieldScriptOpCode.DivideFloat, ctx => DivideOpcodeHandler(ctx, VariableWidth.Float) },
                       { FieldScriptOpCode.RemainderSByte, ctx => RemainderOpcodeHandler(ctx, VariableWidth.SByte) },
                       { FieldScriptOpCode.RemainderByte, ctx => RemainderOpcodeHandler(ctx, VariableWidth.Byte) },
                       { FieldScriptOpCode.RemainderShort, ctx => RemainderOpcodeHandler(ctx, VariableWidth.Short) },
                       { FieldScriptOpCode.RemainderUShort, ctx => RemainderOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.RemainderInt, ctx => RemainderOpcodeHandler(ctx, VariableWidth.Int) },
                       { FieldScriptOpCode.RemainderUInt, ctx => RemainderOpcodeHandler(ctx, VariableWidth.UInt) },
                       { FieldScriptOpCode.RemainderLong, ctx => RemainderOpcodeHandler(ctx, VariableWidth.Long) },
                       { FieldScriptOpCode.RemainderULong, ctx => RemainderOpcodeHandler(ctx, VariableWidth.ULong) },
                       { FieldScriptOpCode.IncrementSByte, ctx => IncrementOpcodeHandler(ctx, VariableWidth.SByte) },
                       { FieldScriptOpCode.IncrementByte, ctx => IncrementOpcodeHandler(ctx, VariableWidth.Byte) },
                       { FieldScriptOpCode.IncrementShort, ctx => IncrementOpcodeHandler(ctx, VariableWidth.Short) },
                       { FieldScriptOpCode.IncrementUShort, ctx => IncrementOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.IncrementInt, ctx => IncrementOpcodeHandler(ctx, VariableWidth.Int) },
                       { FieldScriptOpCode.IncrementUInt, ctx => IncrementOpcodeHandler(ctx, VariableWidth.UInt) },
                       { FieldScriptOpCode.IncrementLong, ctx => IncrementOpcodeHandler(ctx, VariableWidth.Long) },
                       { FieldScriptOpCode.IncrementULong, ctx => IncrementOpcodeHandler(ctx, VariableWidth.ULong) },
                       { FieldScriptOpCode.IncrementSByteClamped, ctx => IncrementClampedOpcodeHandler(ctx, VariableWidth.SByte) },
                       { FieldScriptOpCode.IncrementByteClamped, ctx => IncrementClampedOpcodeHandler(ctx, VariableWidth.Byte) },
                       { FieldScriptOpCode.IncrementShortClamped, ctx => IncrementClampedOpcodeHandler(ctx, VariableWidth.Short) },
                       { FieldScriptOpCode.IncrementUShortClamped, ctx => IncrementClampedOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.IncrementIntClamped, ctx => IncrementClampedOpcodeHandler(ctx, VariableWidth.Int) },
                       { FieldScriptOpCode.IncrementUIntClamped, ctx => IncrementClampedOpcodeHandler(ctx, VariableWidth.UInt) },
                       { FieldScriptOpCode.IncrementLongClamped, ctx => IncrementClampedOpcodeHandler(ctx, VariableWidth.Long) },
                       { FieldScriptOpCode.IncrementULongClamped, ctx => IncrementClampedOpcodeHandler(ctx, VariableWidth.ULong) },
                       { FieldScriptOpCode.DecrementSByte, ctx => DecrementOpcodeHandler(ctx, VariableWidth.SByte) },
                       { FieldScriptOpCode.DecrementByte, ctx => DecrementOpcodeHandler(ctx, VariableWidth.Byte) },
                       { FieldScriptOpCode.DecrementShort, ctx => DecrementOpcodeHandler(ctx, VariableWidth.Short) },
                       { FieldScriptOpCode.DecrementUShort, ctx => DecrementOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.DecrementInt, ctx => DecrementOpcodeHandler(ctx, VariableWidth.Int) },
                       { FieldScriptOpCode.DecrementUInt, ctx => DecrementOpcodeHandler(ctx, VariableWidth.UInt) },
                       { FieldScriptOpCode.DecrementLong, ctx => DecrementOpcodeHandler(ctx, VariableWidth.Long) },
                       { FieldScriptOpCode.DecrementULong, ctx => DecrementOpcodeHandler(ctx, VariableWidth.ULong) },
                       { FieldScriptOpCode.DecrementSByteClamped, ctx => DecrementClampedOpcodeHandler(ctx, VariableWidth.SByte) },
                       { FieldScriptOpCode.DecrementByteClamped, ctx => DecrementClampedOpcodeHandler(ctx, VariableWidth.Byte) },
                       { FieldScriptOpCode.DecrementShortClamped, ctx => DecrementClampedOpcodeHandler(ctx, VariableWidth.Short) },
                       { FieldScriptOpCode.DecrementUShortClamped, ctx => DecrementClampedOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.DecrementIntClamped, ctx => DecrementClampedOpcodeHandler(ctx, VariableWidth.Int) },
                       { FieldScriptOpCode.DecrementUIntClamped, ctx => DecrementClampedOpcodeHandler(ctx, VariableWidth.UInt) },
                       { FieldScriptOpCode.DecrementLongClamped, ctx => DecrementClampedOpcodeHandler(ctx, VariableWidth.Long) },
                       { FieldScriptOpCode.DecrementULongClamped, ctx => DecrementClampedOpcodeHandler(ctx, VariableWidth.ULong) },
                       { FieldScriptOpCode.BitwiseAndSByte, ctx => BitwiseAndOpcodeHandler(ctx, VariableWidth.SByte) },
                       { FieldScriptOpCode.BitwiseAndByte, ctx => BitwiseAndOpcodeHandler(ctx, VariableWidth.Byte) },
                       { FieldScriptOpCode.BitwiseAndShort, ctx => BitwiseAndOpcodeHandler(ctx, VariableWidth.Short) },
                       { FieldScriptOpCode.BitwiseAndUShort, ctx => BitwiseAndOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.BitwiseAndInt, ctx => BitwiseAndOpcodeHandler(ctx, VariableWidth.Int) },
                       { FieldScriptOpCode.BitwiseAndUInt, ctx => BitwiseAndOpcodeHandler(ctx, VariableWidth.UInt) },
                       { FieldScriptOpCode.BitwiseAndLong, ctx => BitwiseAndOpcodeHandler(ctx, VariableWidth.Long) },
                       { FieldScriptOpCode.BitwiseAndULong, ctx => BitwiseAndOpcodeHandler(ctx, VariableWidth.ULong) },
                       { FieldScriptOpCode.BitwiseOrSByte, ctx => BitwiseOrOpcodeHandler(ctx, VariableWidth.SByte) },
                       { FieldScriptOpCode.BitwiseOrByte, ctx => BitwiseOrOpcodeHandler(ctx, VariableWidth.Byte) },
                       { FieldScriptOpCode.BitwiseOrShort, ctx => BitwiseOrOpcodeHandler(ctx, VariableWidth.Short) },
                       { FieldScriptOpCode.BitwiseOrUShort, ctx => BitwiseOrOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.BitwiseOrInt, ctx => BitwiseOrOpcodeHandler(ctx, VariableWidth.Int) },
                       { FieldScriptOpCode.BitwiseOrUInt, ctx => BitwiseOrOpcodeHandler(ctx, VariableWidth.UInt) },
                       { FieldScriptOpCode.BitwiseOrLong, ctx => BitwiseOrOpcodeHandler(ctx, VariableWidth.Long) },
                       { FieldScriptOpCode.BitwiseOrULong, ctx => BitwiseOrOpcodeHandler(ctx, VariableWidth.ULong) },
                       { FieldScriptOpCode.BitwiseXorSByte, ctx => BitwiseXorOpcodeHandler(ctx, VariableWidth.SByte) },
                       { FieldScriptOpCode.BitwiseXorByte, ctx => BitwiseXorOpcodeHandler(ctx, VariableWidth.Byte) },
                       { FieldScriptOpCode.BitwiseXorShort, ctx => BitwiseXorOpcodeHandler(ctx, VariableWidth.Short) },
                       { FieldScriptOpCode.BitwiseXorUShort, ctx => BitwiseXorOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.BitwiseXorInt, ctx => BitwiseXorOpcodeHandler(ctx, VariableWidth.Int) },
                       { FieldScriptOpCode.BitwiseXorUInt, ctx => BitwiseXorOpcodeHandler(ctx, VariableWidth.UInt) },
                       { FieldScriptOpCode.BitwiseXorLong, ctx => BitwiseXorOpcodeHandler(ctx, VariableWidth.Long) },
                       { FieldScriptOpCode.BitwiseXorULong, ctx => BitwiseXorOpcodeHandler(ctx, VariableWidth.ULong) },
                       { FieldScriptOpCode.SetBitSByte, ctx => SetBitOpcodeHandler(ctx, VariableWidth.SByte) },
                       { FieldScriptOpCode.SetBitByte, ctx => SetBitOpcodeHandler(ctx, VariableWidth.Byte) },
                       { FieldScriptOpCode.SetBitShort, ctx => SetBitOpcodeHandler(ctx, VariableWidth.Short) },
                       { FieldScriptOpCode.SetBitUShort, ctx => SetBitOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.SetBitInt, ctx => SetBitOpcodeHandler(ctx, VariableWidth.Int) },
                       { FieldScriptOpCode.SetBitUInt, ctx => SetBitOpcodeHandler(ctx, VariableWidth.UInt) },
                       { FieldScriptOpCode.SetBitLong, ctx => SetBitOpcodeHandler(ctx, VariableWidth.Long) },
                       { FieldScriptOpCode.SetBitULong, ctx => SetBitOpcodeHandler(ctx, VariableWidth.ULong) },
                       { FieldScriptOpCode.UnsetBitSByte, ctx => UnsetBitOpcodeHandler(ctx, VariableWidth.SByte) },
                       { FieldScriptOpCode.UnsetBitByte, ctx => UnsetBitOpcodeHandler(ctx, VariableWidth.Byte) },
                       { FieldScriptOpCode.UnsetBitShort, ctx => UnsetBitOpcodeHandler(ctx, VariableWidth.Short) },
                       { FieldScriptOpCode.UnsetBitUShort, ctx => UnsetBitOpcodeHandler(ctx, VariableWidth.UShort) },
                       { FieldScriptOpCode.UnsetBitInt, ctx => UnsetBitOpcodeHandler(ctx, VariableWidth.Int) },
                       { FieldScriptOpCode.UnsetBitUInt, ctx => UnsetBitOpcodeHandler(ctx, VariableWidth.UInt) },
                       { FieldScriptOpCode.UnsetBitLong, ctx => UnsetBitOpcodeHandler(ctx, VariableWidth.Long) },
                       { FieldScriptOpCode.UnsetBitULong, ctx => UnsetBitOpcodeHandler(ctx, VariableWidth.ULong) },
                       { FieldScriptOpCode.GetRandomNumber, GetRandomNumberOpcodeHandler },
                       { FieldScriptOpCode.RandomNumberSeed, RandomNumberSeedOpcodeHandler },

                       // Windowing and menu
                       { FieldScriptOpCode.CreateDialogueWindow, CreateDialogueWindowOpcodeHandler },
                       { FieldScriptOpCode.ShowDialogueWindow, ShowDialogueWindowOpcodeHandler },
                       // { FieldScriptOpCode.ShowDialogueWindowNoWait, ShowDialogueWindowNoWaitOpcodeHandler },
                       // { FieldScriptOpCode.WaitForDialogueWindow, WaitForDialogueWindowOpcodeHandler },
                       // { FieldScriptOpCode.CloseWindow, CloseWindowOpcodeHandler },
                       // { FieldScriptOpCode.SetDialogueWindowStyle, SetDialogueWindowStyleOpcodeHandler },
                       // { FieldScriptOpCode.SetMessageVariable, SetMessageVariableOpcodeHandler },
                       // { FieldScriptOpCode.SetMessageSpeed, SetMessageSpeedOpcodeHandler },
                       { FieldScriptOpCode.AskPlayerToMakeAChoice, AskPlayerToMakeAChoiceOpcodeHandler },
                       { FieldScriptOpCode.MainMenuAccessibility, MainMenuAccessibilityOpcodeHandler },
                       // { FieldScriptOpCode.OpenMainMenu, OpenMainMenuOpcodeHandler },
                       // { FieldScriptOpCode.OpenSaveMenu, OpenSaveMenuOpcodeHandler },
                       // { FieldScriptOpCode.OpenShop, OpenShopOpcodeHandler },
                       // { FieldScriptOpCode.OpenNameEntry, OpenNameEntryOpcodeHandler },
                       // { FieldScriptOpCode.SetMapNameInMenu, SetMapNameInMenuOpcodeHandler },
                       // { FieldScriptOpCode.RunTutorial, RunTutorialOpcodeHandler },

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
                       // { FieldScriptOpCode.AddMoney, AddMoneyOpcodeHandler },
                       // { FieldScriptOpCode.GetMoneyAmount, GetMoneyAmountOpcodeHandler },
                       // { FieldScriptOpCode.AddItem, AddItemOpcodeHandler },
                       // { FieldScriptOpCode.GetItemCount, GetItemCountOpcodeHandler },

                       // Field models and animation
                       { FieldScriptOpCode.SetPlayerEntity, SetPlayerEntityOpcodeHandler },
                       { FieldScriptOpCode.LockInput, LockInputOpcodeHandler },
                       // { FieldScriptOpCode.SetRunningEnabled, SetRunningEnabledOpcodeHandler },
                       // { FieldScriptOpCode.JoinPartyToLeader, JoinPartyToLeaderOpcodeHandler },
                       // { FieldScriptOpCode.SplitPartyFromLeader, SplitPartyFromLeaderOpcodeHandler },
                       // { FieldScriptOpCode.SetFollowerEnabled, SetFollowerEnabledOpcodeHandler },
                       // { FieldScriptOpCode.ResetFollowerTrail, ResetFollowerTrailOpcodeHandler },
                       { FieldScriptOpCode.Visibility, VisibilityOpcodeHandler },
                       // { FieldScriptOpCode.SetEntityActive, SetEntityActiveOpcodeHandler },
                       // { FieldScriptOpCode.EntitySolidity, EntitySolidityOpcodeHandler },
                       // { FieldScriptOpCode.CollisionScriptActivation, CollisionScriptActivationOpcodeHandler },
                       // { FieldScriptOpCode.SetCollisionRadius, SetCollisionRadiusOpcodeHandler },
                       { FieldScriptOpCode.InteractionTriggerActivation, InteractabilityOpcodeHandler },
                       { FieldScriptOpCode.SetInteractionRange, SetInteractionRangeOpcodeHandler },
                       { FieldScriptOpCode.SetEntityPosition, SetEntityPositionOpcodeHandler },
                       // { FieldScriptOpCode.SetEntityHeightOffset, SetEntityHeightOffsetOpcodeHandler },
                       // { FieldScriptOpCode.SetEntityDrawOffset, SetEntityDrawOffsetOpcodeHandler },
                       // { FieldScriptOpCode.WaitForEntityDrawOffset, WaitForEntityDrawOffsetOpcodeHandler },
                       { FieldScriptOpCode.SetMovementSpeed, SetMovementSpeedOpcodeHandler },
                       // { FieldScriptOpCode.MoveEntityToXYWalkAnimation, MoveEntityToXYWalkAnimationOpcodeHandler },
                       // { FieldScriptOpCode.MoveEntityToXYNoAnimation, MoveEntityToXYNoAnimationOpcodeHandler },
                       // { FieldScriptOpCode.MoveFieldObject, MoveFieldObjectOpcodeHandler },
                       // { FieldScriptOpCode.MoveEntityToAnotherEntity, MoveEntityToAnotherEntityOpcodeHandler },
                       // { FieldScriptOpCode.MoveToPartyMember, MoveToPartyMemberOpcodeHandler },
                       // { FieldScriptOpCode.MakeEntityJump, MakeEntityJumpOpcodeHandler },
                       // { FieldScriptOpCode.JumpToPartyMember, JumpToPartyMemberOpcodeHandler },
                       // { FieldScriptOpCode.ClimbLadder, ClimbLadderOpcodeHandler },
                       // { FieldScriptOpCode.WaitForMovement, WaitForMovementOpcodeHandler },
                       // { FieldScriptOpCode.FlushMovement, FlushMovementOpcodeHandler },
                       { FieldScriptOpCode.SetEntityRotation, SetEntityRotationOpcodeHandler },
                       { FieldScriptOpCode.SetEntityRotationOverTime, SetEntityRotationOverTimeOpcodeHandler },
                       { FieldScriptOpCode.SetDirectionToFaceEntity, SetDirectionToFaceEntityOpcodeHandler },
                       // { FieldScriptOpCode.SetDirectionToPosition, SetDirectionToPositionOpcodeHandler },
                       // { FieldScriptOpCode.SetDirectionToPartyMember, SetDirectionToPartyMemberOpcodeHandler },
                       // { FieldScriptOpCode.TurnEntityToAnotherEntity, TurnEntityToAnotherEntityOpcodeHandler },
                       // { FieldScriptOpCode.TurnToPartyMember, TurnToPartyMemberOpcodeHandler },
                       // { FieldScriptOpCode.SetBaseAnimation, SetBaseAnimationOpcodeHandler },
                       // { FieldScriptOpCode.SetLadderAnimations, SetLadderAnimationsOpcodeHandler },
                       // { FieldScriptOpCode.PlayAnimationLooping, PlayAnimationLoopingOpcodeHandler },
                       // { FieldScriptOpCode.PlayAnimationOnceAndWait, PlayAnimationOnceAndWaitOpcodeHandler },
                       // { FieldScriptOpCode.PlayAnimationOnceAsync, PlayAnimationOnceAsyncOpcodeHandler },
                       // { FieldScriptOpCode.PlayAnimationStopOnLastFrameWait, PlayAnimationStopOnLastFrameWaitOpcodeHandler },
                       // { FieldScriptOpCode.PlayPartialAnimation, PlayPartialAnimationOpcodeHandler },
                       // { FieldScriptOpCode.SetAnimationSpeed, SetAnimationSpeedOpcodeHandler },
                       // { FieldScriptOpCode.WaitForAnimation, WaitForAnimationOpcodeHandler },
                       // { FieldScriptOpCode.StopAnimation, StopAnimationOpcodeHandler },
                       // { FieldScriptOpCode.PushAnimationState, PushAnimationStateOpcodeHandler },
                       // { FieldScriptOpCode.PopAnimationState, PopAnimationStateOpcodeHandler },
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
                       // { FieldScriptOpCode.GetEntityPosition, GetEntityPositionXYZIOpcodeHandler },
                       // { FieldScriptOpCode.GetEntityDirection, GetEntityDirectionOpcodeHandler },
                       // { FieldScriptOpCode.GetPartyMemberPosition, GetPartyMemberPositionOpcodeHandler },
                       // { FieldScriptOpCode.GetPartyMemberDirection, GetPartyMemberDirectionOpcodeHandler },
                       // { FieldScriptOpCode.CopyEntityInfo, CopyEntityInfoOpcodeHandler },
                       // { FieldScriptOpCode.IsEntityTouching, IsEntityTouchingOpcodeHandler },

                       // Screen and field effects
                       // { FieldScriptOpCode.SubtractiveScreenFade, SubtractiveScreenFadeOpcodeHandler },
                       // { FieldScriptOpCode.AdditiveScreenFade, AdditiveScreenFadeOpcodeHandler },
                       // { FieldScriptOpCode.WaitForScreenColour, WaitForScreenColourOpcodeHandler },
                       // { FieldScriptOpCode.ParticleActivation, ParticleActivationOpcodeHandler },

                       // Camera and screen movement
                       // { FieldScriptOpCode.FadeScreen, FadeScreenOpcodeHandler },
                       // { FieldScriptOpCode.FadeScreenWait, FadeScreenWaitOpcodeHandler },
                       // { FieldScriptOpCode.WaitForFade, WaitForFadeOpcodeHandler },
                       // { FieldScriptOpCode.ShakeScreen, ShakeScreenOpcodeHandler },
                       // { FieldScriptOpCode.ScrollScreenToPosition, ScrollScreenToPositionOpcodeHandler },
                       // { FieldScriptOpCode.ScrollScreenToEntity, ScrollScreenToEntityOpcodeHandler },
                       // { FieldScriptOpCode.ScrollToPartyMember, ScrollToPartyMemberOpcodeHandler },
                       // { FieldScriptOpCode.WaitForScrolling, WaitForScrollingOpcodeHandler },
                       // { FieldScriptOpCode.SetCamera, SetCameraOpcodeHandler },

                       // Audio
                       { FieldScriptOpCode.PlayMusic, PlayMusicOpcodeHandler },
                       // { FieldScriptOpCode.StopMusic, StopMusicOpcodeHandler },
                       // { FieldScriptOpCode.SetMusicVolume, SetMusicVolumeOpcodeHandler },
                       // { FieldScriptOpCode.FadeMusicVolume, FadeMusicVolumeOpcodeHandler },
                       { FieldScriptOpCode.SetMusicStemState, SetMusicStemStateOpcodeHandler },
                       // { FieldScriptOpCode.CheckIfMusicIsPlaying, CheckIfMusicIsPlayingOpcodeHandler },
                       // { FieldScriptOpCode.SetBattleMusic, SetBattleMusicOpcodeHandler },
                       { FieldScriptOpCode.PlaySound, PlaySoundOpcodeHandler },
                       // { FieldScriptOpCode.PlayAmbientLoop, PlayAmbientLoopOpcodeHandler },
                       // { FieldScriptOpCode.SetAllSoundVolume, SetAllSoundVolumeOpcodeHandler },
                       // { FieldScriptOpCode.FadeAllSoundVolume, FadeAllSoundVolumeOpcodeHandler },

                       // Video
                       // { FieldScriptOpCode.PrepareMovie, PrepareMovieOpcodeHandler },
                       // { FieldScriptOpCode.PlayMovie, PlayMovieOpcodeHandler },
                       // { FieldScriptOpCode.WaitForMovie, WaitForMovieOpcodeHandler },

                       // Timer
                       // { FieldScriptOpCode.SetCountdownTimer, SetCountdownTimerOpcodeHandler },
                       // { FieldScriptOpCode.ShowCountdownTimer, ShowCountdownTimerOpcodeHandler },
                       // { FieldScriptOpCode.GetCountdownTimer, GetCountdownTimerOpcodeHandler },

                       // Input and haptics
                       // { FieldScriptOpCode.SetVibration, SetVibrationOpcodeHandler },
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

        internal ScriptRunOutcome Execute(int entityId, byte priority, int scriptId, FieldEntityRuntime entity, ref int instructionBudget)
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

            while (!ctx.IsBlocked())
            {
                if (instructionBudget == 0)
                {
                    return ScriptRunOutcome.OutOfInstructions;
                }

                instructionBudget--;

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

                // The slot was handed to another script, which runs next on what is left of the budget.
                if (!m_Contexts.TryGetValue(key, out ScriptExecutionContext current) || current != ctx)
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

        private void WriteVariableInt(ScriptExecutionContext ctx, MemoryBank bank, ushort address, int value)
        {
            if (bank == MemoryBank.Temp)
            {
                TempOf(ctx).WriteInt(address, value);
                return;
            }

            m_MemoryService.WriteInt(bank, address, value);
        }

        private void WriteVariableFloat(ScriptExecutionContext ctx, MemoryBank bank, ushort address, float value)
        {
            if (bank == MemoryBank.Temp)
            {
                TempOf(ctx).WriteFloat(address, value);
                return;
            }

            m_MemoryService.WriteFloat(bank, address, value);
        }

        private ulong ReadVariableUlong(ScriptExecutionContext ctx, MemoryBank bank, ushort address)
        {
            ulong value = bank == MemoryBank.Temp ? TempOf(ctx).ReadUlong(address) : m_MemoryService.ReadUlong(bank, address);

            return value;
        }

        private void WriteVariableUlong(ScriptExecutionContext ctx, MemoryBank bank, ushort address, ulong value)
        {
            if (bank == MemoryBank.Temp)
            {
                TempOf(ctx).WriteUlong(address, value);
                return;
            }

            m_MemoryService.WriteUlong(bank, address, value);
        }

        // A variable's bits at its width, zero-extended.
        private ulong ReadVariableBits(ScriptExecutionContext ctx, MemoryBank bank, ushort address, VariableWidth width)
        {
            ulong bits = width.GetByteCount() switch
                         {
                             1 => ReadVariableByte(ctx, bank, address),
                             2 => ReadVariableUshort(ctx, bank, address),
                             4 => (uint)ReadVariableInt(ctx, bank, address),
                             _ => ReadVariableUlong(ctx, bank, address)
                         };

            return bits;
        }

        private void WriteVariableBits(ScriptExecutionContext ctx, MemoryBank bank, ushort address, VariableWidth width, ulong bits)
        {
            switch (width.GetByteCount())
            {
                case 1:
                    WriteVariableByte(ctx, bank, address, (byte)bits);
                    return;

                case 2:
                    WriteVariableUshort(ctx, bank, address, (ushort)bits);
                    return;

                case 4:
                    WriteVariableInt(ctx, bank, address, unchecked((int)bits));
                    return;

                default:
                    WriteVariableUlong(ctx, bank, address, bits);
                    return;
            }
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
        /// Decode a bank-layout destination: the sources byte and the destination's address. The value's source is
        /// the sources byte's low nibble.
        /// </summary>
        private static void DecodeDestination(ScriptExecutionContext ctx, out MemoryBank bank, out ushort address, out byte valueSource)
        {
            byte sources = ReadByte(ctx);

            bank        = ToMemoryBank(GetFirstArgumentSource(sources));
            address     = ReadUshort(ctx);
            valueSource = GetSecondArgumentSource(sources);
        }

        private void ApplyBinary(ScriptExecutionContext ctx, VariableWidth width, Func<ulong, ulong, VariableWidth, ulong> integer, Func<float, float, float> real)
        {
            DecodeDestination(ctx, out MemoryBank bank, out ushort address, out byte valueSource);

            if (width == VariableWidth.Float)
            {
                float value = ReadFloatOperand(ctx, valueSource);

                WriteVariableFloat(ctx, bank, address, real(ReadVariableFloat(ctx, bank, address), value));
                return;
            }

            ulong integerValue = ReadIntegerOperand(ctx, valueSource, width);
            ulong current      = ReadVariableBits(ctx, bank, address, width);

            WriteVariableBits(ctx, bank, address, width, integer(current, integerValue, width));
        }

        private void ApplyUnary(ScriptExecutionContext ctx, VariableWidth width, Func<ulong, VariableWidth, ulong> integer)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            ulong current = ReadVariableBits(ctx, bank, address, width);

            WriteVariableBits(ctx, bank, address, width, integer(current, width));
        }

        private void ChangeBit(ScriptExecutionContext ctx, VariableWidth width, bool set, string handlerName)
        {
            DecodeDestination(ctx, out MemoryBank bank, out ushort address, out byte indexSource);

            ulong index   = ReadIntegerOperand(ctx, indexSource, VariableWidth.Byte);
            ulong current = ReadVariableBits(ctx, bank, address, width);

            if (!ScriptArithmetic.TrySetBit(current, index, width, set, out ulong result))
            {
                Debug.LogError($"{nameof(FieldVM)}::{handlerName} Bit index [{index}] is out of range for a {width} at [{bank}:{address}]");
                return;
            }

            WriteVariableBits(ctx, bank, address, width, result);
        }

        // A bank-layout value is an immediate at the argument's width, or the width of the variable it reads followed
        // by that variable's address. A narrower variable is converted as a cast would convert it.
        private ulong ReadIntegerOperand(ScriptExecutionContext ctx, byte source, VariableWidth width)
        {
            if (source == ARGUMENT_IMMEDIATE)
            {
                ulong immediate = ReadImmediateBits(ctx, width);

                return immediate;
            }

            VariableWidth variableWidth = (VariableWidth)ReadByte(ctx);
            ushort        address       = ReadUshort(ctx);

            ulong bits  = ReadVariableBits(ctx, ToMemoryBank(source), address, variableWidth);
            ulong value = ScriptArithmetic.Convert(bits, variableWidth, width);

            return value;
        }

        private float ReadFloatOperand(ScriptExecutionContext ctx, byte source)
        {
            if (source == ARGUMENT_IMMEDIATE)
            {
                float immediate = ReadFloat(ctx);

                return immediate;
            }

            VariableWidth variableWidth = (VariableWidth)ReadByte(ctx);
            MemoryBank    bank          = ToMemoryBank(source);
            ushort        address       = ReadUshort(ctx);

            float value = variableWidth == VariableWidth.Float
                              ? ReadVariableFloat(ctx, bank, address)
                              : ScriptArithmetic.ToFloat(ReadVariableBits(ctx, bank, address, variableWidth), variableWidth);

            return value;
        }

        private bool ReadBoolOperand(ScriptExecutionContext ctx, byte source)
        {
            if (source == ARGUMENT_IMMEDIATE)
            {
                bool immediate = ReadBool(ctx);

                return immediate;
            }

            // The width byte: a bool only ever reads a bool.
            ReadByte(ctx);

            ushort address = ReadUshort(ctx);
            bool   value   = ReadVariableBool(ctx, ToMemoryBank(source), address);

            return value;
        }

        private static ulong ReadImmediateBits(ScriptExecutionContext ctx, VariableWidth width)
        {
            ulong bits = width.GetByteCount() switch
                         {
                             1 => ReadByte(ctx),
                             2 => ReadUshort(ctx),
                             4 => (uint)ReadInt(ctx),
                             _ => ReadUlong(ctx)
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
        /// Decode the arguments shared by the three request opcodes and resolve the target entity.
        /// </summary>
        private bool TryReadScriptRequest(ScriptExecutionContext ctx, out byte targetEntityId, out FieldEntityRuntime target, out int targetScriptId, out byte priority)
        {
            SequentialSources sources = default;
            ushort            targetEventId;

            targetEntityId = ReadArgumentByte(ctx, ref sources);
            priority       = ReadArgumentByte(ctx, ref sources);
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

        // Already satisfied: carry on next frame. Otherwise wait, and the frame the wait ends on is spent too.
        private static void WaitForRequest(ScriptExecutionContext ctx, Func<bool> isSatisfied)
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
        private static void ReturnOpcodeHandler(ScriptExecutionContext ctx)
        {
            // noop
        }


        /// <summary>
        /// Put a script in an entity's priority slot and carry on; refused if the slot is busy. A request on
        /// another entity ends this script's frame. One on this entity for a more urgent slot takes over
        /// straight away.
        /// </summary>
        private void RunAnotherEntityScriptUnlessBusyOpcodeHandler(ScriptExecutionContext ctx)
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
        private void RunAnotherEntityScriptWaitUntilStartedOpcodeHandler(ScriptExecutionContext ctx)
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
        private void RunAnotherEntityScriptWaitUntilFinishedOpcodeHandler(ScriptExecutionContext ctx)
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
        /// Compare two values, skipping the IF body when the comparison fails.
        /// </summary>
        private void CompareOpcodeHandler(ScriptExecutionContext ctx, VariableWidth width)
        {
            byte sources = ReadByte(ctx);
            byte aSource = GetFirstArgumentSource(sources);
            byte bSource = GetSecondArgumentSource(sources);

            bool result;

            switch (width)
            {
                case VariableWidth.Bool:
                {
                    bool a = ReadBoolOperand(ctx, aSource);
                    bool b = ReadBoolOperand(ctx, bSource);

                    result = CompareBools(a, b, (ScriptComparison)ReadByte(ctx));
                    break;
                }

                case VariableWidth.Float:
                {
                    float a = ReadFloatOperand(ctx, aSource);
                    float b = ReadFloatOperand(ctx, bSource);

                    result = CompareFloats(a, b, (ScriptComparison)ReadByte(ctx));
                    break;
                }

                default:
                {
                    ulong a = ReadIntegerOperand(ctx, aSource, width);
                    ulong b = ReadIntegerOperand(ctx, bSource, width);

                    result = ScriptArithmetic.Compare(a, b, width, (ScriptComparison)ReadByte(ctx));
                    break;
                }
            }

            int jumpDistance = ReadInt(ctx);

            if (!result)
            {
                ctx.InstructionPointer += jumpDistance;
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
        /// Set a variable to a value.
        /// </summary>
        private void SetOpcodeHandler(ScriptExecutionContext ctx, VariableWidth width)
        {
            DecodeDestination(ctx, out MemoryBank bank, out ushort address, out byte valueSource);

            switch (width)
            {
                case VariableWidth.Bool:
                    WriteVariableBool(ctx, bank, address, ReadBoolOperand(ctx, valueSource));
                    return;

                case VariableWidth.Float:
                    WriteVariableFloat(ctx, bank, address, ReadFloatOperand(ctx, valueSource));
                    return;

                default:
                    WriteVariableBits(ctx, bank, address, width, ReadIntegerOperand(ctx, valueSource, width));
                    return;
            }
        }

        /// <summary>
        /// Add a value to a variable, wrapping if it overflows.
        /// </summary>
        private void AddOpcodeHandler(ScriptExecutionContext ctx, VariableWidth width)
        {
            ApplyBinary(ctx, width, (current, value, w) => ScriptArithmetic.Add(current, value, w), (current, value) => current + value);
        }

        /// <summary>
        /// Add a value to a variable, stopping at its limit.
        /// </summary>
        private void AddClampedOpcodeHandler(ScriptExecutionContext ctx, VariableWidth width)
        {
            ApplyBinary(ctx, width, (current, value, w) => ScriptArithmetic.AddClamped(current, value, w), null);
        }

        /// <summary>
        /// Subtract a value from a variable, wrapping if it overflows.
        /// </summary>
        private void SubtractOpcodeHandler(ScriptExecutionContext ctx, VariableWidth width)
        {
            ApplyBinary(ctx, width, (current, value, w) => ScriptArithmetic.Subtract(current, value, w), (current, value) => current - value);
        }

        /// <summary>
        /// Subtract a value from a variable, stopping at its limit.
        /// </summary>
        private void SubtractClampedOpcodeHandler(ScriptExecutionContext ctx, VariableWidth width)
        {
            ApplyBinary(ctx, width, (current, value, w) => ScriptArithmetic.SubtractClamped(current, value, w), null);
        }

        /// <summary>
        /// Multiply a variable by a value, wrapping if it overflows.
        /// </summary>
        private void MultiplyOpcodeHandler(ScriptExecutionContext ctx, VariableWidth width)
        {
            ApplyBinary(ctx, width, (current, value, w) => ScriptArithmetic.Multiply(current, value, w), (current, value) => current * value);
        }

        /// <summary>
        /// Divide a variable by a value. Dividing by zero is logged and the variable left unchanged.
        /// </summary>
        private void DivideOpcodeHandler(ScriptExecutionContext ctx, VariableWidth width)
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

                WriteVariableFloat(ctx, bank, address, ReadVariableFloat(ctx, bank, address) / divisor);
                return;
            }

            ulong value   = ReadIntegerOperand(ctx, valueSource, width);
            ulong current = ReadVariableBits(ctx, bank, address, width);

            if (!ScriptArithmetic.TryDivide(current, value, width, out ulong quotient))
            {
                Debug.LogError($"{nameof(FieldVM)}::{nameof(DivideOpcodeHandler)} Divide by zero at [{bank}:{address}], leaving the value unchanged");
                return;
            }

            WriteVariableBits(ctx, bank, address, width, quotient);
        }

        /// <summary>
        /// Replace a variable with its remainder after dividing by a value. Dividing by zero is logged and the variable left unchanged.
        /// </summary>
        private void RemainderOpcodeHandler(ScriptExecutionContext ctx, VariableWidth width)
        {
            DecodeDestination(ctx, out MemoryBank bank, out ushort address, out byte valueSource);

            ulong value   = ReadIntegerOperand(ctx, valueSource, width);
            ulong current = ReadVariableBits(ctx, bank, address, width);

            if (!ScriptArithmetic.TryRemainder(current, value, width, out ulong remainder))
            {
                Debug.LogError($"{nameof(FieldVM)}::{nameof(RemainderOpcodeHandler)} Modulo by zero at [{bank}:{address}], leaving the value unchanged");
                return;
            }

            WriteVariableBits(ctx, bank, address, width, remainder);
        }

        /// <summary>
        /// Add one to a variable, wrapping if it overflows.
        /// </summary>
        private void IncrementOpcodeHandler(ScriptExecutionContext ctx, VariableWidth width)
        {
            ApplyUnary(ctx, width, (current, w) => ScriptArithmetic.Add(current, 1, w));
        }

        /// <summary>
        /// Add one to a variable, stopping at its limit.
        /// </summary>
        private void IncrementClampedOpcodeHandler(ScriptExecutionContext ctx, VariableWidth width)
        {
            ApplyUnary(ctx, width, (current, w) => ScriptArithmetic.AddClamped(current, 1, w));
        }

        /// <summary>
        /// Subtract one from a variable, wrapping if it overflows.
        /// </summary>
        private void DecrementOpcodeHandler(ScriptExecutionContext ctx, VariableWidth width)
        {
            ApplyUnary(ctx, width, (current, w) => ScriptArithmetic.Subtract(current, 1, w));
        }

        /// <summary>
        /// Subtract one from a variable, stopping at its limit.
        /// </summary>
        private void DecrementClampedOpcodeHandler(ScriptExecutionContext ctx, VariableWidth width)
        {
            ApplyUnary(ctx, width, (current, w) => ScriptArithmetic.SubtractClamped(current, 1, w));
        }

        /// <summary>
        /// Bitwise AND a variable with a value.
        /// </summary>
        private void BitwiseAndOpcodeHandler(ScriptExecutionContext ctx, VariableWidth width)
        {
            ApplyBinary(ctx, width, (current, value, w) => ScriptArithmetic.BitwiseAnd(current, value, w), null);
        }

        /// <summary>
        /// Bitwise OR a variable with a value.
        /// </summary>
        private void BitwiseOrOpcodeHandler(ScriptExecutionContext ctx, VariableWidth width)
        {
            ApplyBinary(ctx, width, (current, value, w) => ScriptArithmetic.BitwiseOr(current, value, w), null);
        }

        /// <summary>
        /// Bitwise XOR a variable with a value.
        /// </summary>
        private void BitwiseXorOpcodeHandler(ScriptExecutionContext ctx, VariableWidth width)
        {
            ApplyBinary(ctx, width, (current, value, w) => ScriptArithmetic.BitwiseXor(current, value, w), null);
        }

        /// <summary>
        /// Set one bit of a variable. An index past its last bit is logged and the variable left unchanged.
        /// </summary>
        private void SetBitOpcodeHandler(ScriptExecutionContext ctx, VariableWidth width)
        {
            ChangeBit(ctx, width, true, nameof(SetBitOpcodeHandler));
        }

        /// <summary>
        /// Clear one bit of a variable. An index past its last bit is logged and the variable left unchanged.
        /// </summary>
        private void UnsetBitOpcodeHandler(ScriptExecutionContext ctx, VariableWidth width)
        {
            ChangeBit(ctx, width, false, nameof(UnsetBitOpcodeHandler));
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
            int  seed    = unchecked((int)ReadIntegerOperand(ctx, GetSecondArgumentSource(sources), VariableWidth.Int));

            m_Random = new System.Random(seed);
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
        private void SetPlayerEntityOpcodeHandler(ScriptExecutionContext ctx)
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
        private void SetEntityRotationOverTimeOpcodeHandler(ScriptExecutionContext ctx)
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