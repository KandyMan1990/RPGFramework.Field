using System;
using System.Collections.Generic;
using RPGFramework.Battle.SharedTypes;
using RPGFramework.Core;
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
        internal event Action<int>                             RequestMusic;
        internal event Action<int>                             RequestSfx;
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
        internal event Action<byte, ushort, ulong, ulong[]>    RequestAskPlayerToMakeAChoice;
        internal Func<ulong, bool>                             IsPlayerMakingAChoice;
        internal event Action<BattleArgs>                      RequestSetBattleModeOptions;
        internal event Action                                  RequestStartBattle;

        private delegate void OpcodeHandler(ScriptExecutionContext ctx);

        private readonly Dictionary<(int entityId, byte priority), ScriptExecutionContext> m_Contexts;
        private readonly Dictionary<int, FieldEntityRuntime>                               m_Entities;
        private readonly Dictionary<FieldScriptOpCode, OpcodeHandler>                      m_OpcodeHandlers;
        private readonly Dictionary<int, byte[]>                                           m_Scripts;

        private readonly IMemoryService m_MemoryService;

        private System.Random m_Random = new System.Random();

        internal FieldVM(IMemoryService memoryService)
        {
            m_Contexts       = new Dictionary<(int entityId, byte priority), ScriptExecutionContext>();
            m_Entities       = new Dictionary<int, FieldEntityRuntime>();
            m_OpcodeHandlers = BuildOpcodeHandlersArray();
            m_Scripts        = new Dictionary<int, byte[]>();

            m_MemoryService = memoryService;
        }

        internal void RegisterEntity(int entityId, FieldEntityRuntime entity)
        {
            m_Entities.Add(entityId, entity);
        }

        internal void RegisterScript(int scriptId, FieldCompiledScript script)
        {
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

        internal void Execute(int entityId, byte priority, int scriptId, FieldEntityRuntime entity)
        {
            (int entityId, byte priority) key = (entityId, priority);

            // A slot outlives the scripts that pass through it, so a context is reused only while it is
            // still running the script the slot currently holds.
            if (!m_Contexts.TryGetValue(key, out ScriptExecutionContext ctx) || ctx.ScriptId != scriptId)
            {
                if (!m_Scripts.TryGetValue(scriptId, out byte[] bytecode))
                {
                    entity.ClearSlot(priority);
                    return;
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

            if (ctx.IsBlocked())
            {
                ctx.UpdateBlock(Time.deltaTime);
                return;
            }

            while (!ctx.IsBlocked())
            {
                FieldScriptOpCode opcode = FetchOpcode(ctx);

                if (!m_OpcodeHandlers.TryGetValue(opcode, out OpcodeHandler opcodeHandler))
                {
                    m_Contexts.Remove(key);
                    entity.ClearSlot(priority);
                    return;
                }

                opcodeHandler(ctx);

                if (opcode == FieldScriptOpCode.Return)
                {
                    m_Contexts.Remove(key);
                    entity.ClearSlot(priority);
                    return;
                }
            }
        }

        private static FieldScriptOpCode FetchOpcode(ScriptExecutionContext ctx)
        {
            return (FieldScriptOpCode)ReadUshort(ctx);
        }

        // Operand sources are packed two to a byte: the high nibble selects where the first operand
        // (the destination, for anything that writes) comes from, the low nibble the second.
        //
        //   0 = immediate, the value follows inline in the bytecode
        //   1 = Global bank      2 = Session bank      3 = Temp bank
        //
        // Bank addresses are ushort, matching IMemoryService, so a script can reach any variable the
        // variable map declares.
        private const byte OPERAND_IMMEDIATE = 0;

        private static byte GetFirstOperandSource(byte sources)
        {
            byte source = (byte)(sources >> 4);

            return source;
        }

        private static byte GetSecondOperandSource(byte sources)
        {
            byte source = (byte)(sources & 0x0F);

            return source;
        }

        private static MemoryBank ToMemoryBank(byte source)
        {
            if (source == OPERAND_IMMEDIATE)
            {
                throw new InvalidOperationException($"{nameof(FieldVM)}::{nameof(ToMemoryBank)} Operand source [{source}] is an immediate value, not a bank. A destination operand must name a bank");
            }

            MemoryBank bank = (MemoryBank)(source - 1);

            return bank;
        }

        /// <summary>
        /// Read an 8 bit operand: either an inline literal byte, or a ushort address into a bank.
        /// </summary>
        private byte ReadOperandByte(ScriptExecutionContext ctx, byte source)
        {
            if (source == OPERAND_IMMEDIATE)
            {
                byte immediate = ReadByte(ctx);

                return immediate;
            }

            ushort address = ReadUshort(ctx);
            byte   value   = m_MemoryService.ReadByte(ToMemoryBank(source), address);

            return value;
        }

        /// <summary>
        /// Read a 16 bit operand: either an inline literal ushort, or a ushort address into a bank.
        /// </summary>
        private ushort ReadOperandUshort(ScriptExecutionContext ctx, byte source)
        {
            if (source == OPERAND_IMMEDIATE)
            {
                ushort immediate = ReadUshort(ctx);

                return immediate;
            }

            ushort address = ReadUshort(ctx);
            ushort value   = m_MemoryService.ReadUshort(ToMemoryBank(source), address);

            return value;
        }

        /// <summary>
        /// Read an int operand: either an inline literal int, or a ushort address into a bank.
        /// </summary>
        private int ReadOperandInt(ScriptExecutionContext ctx, byte source)
        {
            if (source == OPERAND_IMMEDIATE)
            {
                int immediate = ReadInt(ctx);

                return immediate;
            }

            ushort address = ReadUshort(ctx);
            int    value   = m_MemoryService.ReadInt(ToMemoryBank(source), address);

            return value;
        }

        /// <summary>
        /// Decode the common shape of every opcode that reads a destination variable, combines it with a
        /// second operand and writes the result back: sources byte, destination address, then the operand.
        /// </summary>
        private void ReadBinaryOperandsByte(ScriptExecutionContext ctx, out MemoryBank destinationBank, out ushort destinationAddress, out byte operand)
        {
            byte sources = ReadByte(ctx);

            destinationBank    = ToMemoryBank(GetFirstOperandSource(sources));
            destinationAddress = ReadUshort(ctx);
            operand            = ReadOperandByte(ctx, GetSecondOperandSource(sources));
        }

        private void ReadBinaryOperandsUshort(ScriptExecutionContext ctx, out MemoryBank destinationBank, out ushort destinationAddress, out ushort operand)
        {
            byte sources = ReadByte(ctx);

            destinationBank    = ToMemoryBank(GetFirstOperandSource(sources));
            destinationAddress = ReadUshort(ctx);
            operand            = ReadOperandUshort(ctx, GetSecondOperandSource(sources));
        }

        /// <summary>
        /// Decode an opcode that only names a destination, such as an increment.
        /// </summary>
        private static void ReadDestination(ScriptExecutionContext ctx, out MemoryBank destinationBank, out ushort destinationAddress)
        {
            byte sources = ReadByte(ctx);

            destinationBank    = ToMemoryBank(GetFirstOperandSource(sources));
            destinationAddress = ReadUshort(ctx);
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
            int value = ReadInt(ctx);

            return BitConverter.Int32BitsToSingle(value);
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

        private static byte[] ReadFieldStringBytes(ScriptExecutionContext ctx)
        {
            byte[] result = new byte[FieldNameUtils.FIELD_NAME_SIZE];
            Array.Copy(ctx.Bytecode, ctx.InstructionPointer, result, 0, FieldNameUtils.FIELD_NAME_SIZE);

            ctx.InstructionPointer += FieldNameUtils.FIELD_NAME_SIZE;

            return result;
        }

        // TODO: once op codes are implemented, convert from dictionary to an array
        private Dictionary<FieldScriptOpCode, OpcodeHandler> BuildOpcodeHandlersArray()
        {
            return new Dictionary<FieldScriptOpCode, OpcodeHandler>
                   {
                       // Script Flow and Control
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

                       // System and Module Control
                       // { FieldScriptOpCode.SpecialOp, SpecialOpOpcodeHandler },
                       // { FieldScriptOpCode.RunMinigame, RunMinigameOpcodeHandler },
                       { FieldScriptOpCode.SetBattleModeOptions, SetBattleModeOptionsOpcodeHandler },
                       // { FieldScriptOpCode.LoadResultOfLastBattle, LoadResultOfLastBattleOpcodeHandler },
                       // { FieldScriptOpCode.SetBattleEncounterTable, SetBattleEncounterTableOpcodeHandler },
                       { FieldScriptOpCode.JumpToAnotherMap, JumpToAnotherMapOpcodeHandler },
                       // { FieldScriptOpCode.GetLastFieldMap, GetLastFieldMapOpcodeHandler },
                       { FieldScriptOpCode.StartBattle, StartBattleOpcodeHandler },
                       // { FieldScriptOpCode.RandomEncounters, RandomEncountersOpcodeHandler },
                       { FieldScriptOpCode.GatewayTriggerActivation, GatewayTriggerActivationOpcodeHandler },
                       // { FieldScriptOpCode.GameOver, GameOverOpcodeHandler },

                       // Assignment and Mathematics
                       { FieldScriptOpCode.Addition8BitClamped, Addition8BitClampedOpcodeHandler },
                       { FieldScriptOpCode.Addition16BitClamped, Addition16BitClampedOpcodeHandler },
                       { FieldScriptOpCode.Subtraction8BitClamped, Subtraction8BitClampedOpcodeHandler },
                       { FieldScriptOpCode.Subtraction16BitClamped, Subtraction16BitClampedOpcodeHandler },
                       { FieldScriptOpCode.Increment8BitClamped, Increment8BitClampedOpcodeHandler },
                       { FieldScriptOpCode.Increment16BitClamped, Increment16BitClampedOpcodeHandler },
                       { FieldScriptOpCode.Decrement8BitClamped, Decrement8BitClampedOpcodeHandler },
                       { FieldScriptOpCode.Decrement16BitClamped, Decrement16BitClampedOpcodeHandler },
                       { FieldScriptOpCode.RandomNumberSeed, RandomNumberSeedOpcodeHandler },
                       { FieldScriptOpCode.AssignValue8Bit, AssignValue8BitOpcodeHandler },
                       { FieldScriptOpCode.AssignValue16Bit, AssignValue16BitOpcodeHandler },
                       { FieldScriptOpCode.SetBit, SetBitOpcodeHandler },
                       { FieldScriptOpCode.UnsetBit, UnsetBitOpcodeHandler },
                       { FieldScriptOpCode.Addition8Bit, Addition8BitOpcodeHandler },
                       { FieldScriptOpCode.Addition16Bit, Addition16BitOpcodeHandler },
                       { FieldScriptOpCode.Subtraction8Bit, Subtraction8BitOpcodeHandler },
                       { FieldScriptOpCode.Subtraction16Bit, Subtraction16BitOpcodeHandler },
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
                       { FieldScriptOpCode.Decrement8Bit, Decrement8BitOpcodeHandler },
                       { FieldScriptOpCode.Decrement16Bit, Decrement16BitOpcodeHandler },
                       { FieldScriptOpCode.GetRandomNumber, GetRandomNumberOpcodeHandler },
                       // { FieldScriptOpCode.GetLowByte, GetLowByteOpcodeHandler },
                       // { FieldScriptOpCode.GetHighByte, GetHighByteOpcodeHandler },
                       // { FieldScriptOpCode.GetTwoBytes, GetTwoBytesOpcodeHandler },
                       // { FieldScriptOpCode.Sine, SineOpcodeHandler },
                       // { FieldScriptOpCode.Cosine, CosineOpcodeHandler },

                       // Windowing and Menu
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

                       // Party and Inventory
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

                       // Field Models and Animation
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

                       // Background and Palette
                       // { FieldScriptOpCode.SetBackgroundDepth, SetBackgroundDepthOpcodeHandler },
                       // { FieldScriptOpCode.ScrollBackground, ScrollBackgroundOpcodeHandler },
                       // { FieldScriptOpCode.BackgroundOn, BackgroundOnOpcodeHandler },
                       // { FieldScriptOpCode.BackgroundOff, BackgroundOffOpcodeHandler },
                       // { FieldScriptOpCode.BackgroundRollForward, BackgroundRollForwardOpcodeHandler },
                       // { FieldScriptOpCode.BackgroundRollBackward, BackgroundRollBackwardOpcodeHandler },
                       // { FieldScriptOpCode.BackgroundClear, BackgroundClearOpcodeHandler },

                       // Camera, Audio and Video
                       // { FieldScriptOpCode.FadeScreen, FadeScreenOpcodeHandler },
                       // { FieldScriptOpCode.ShakeScreen, ShakeScreenOpcodeHandler },
                       // { FieldScriptOpCode.ScrollScreen, ScrollScreenOpcodeHandler },
                       // { FieldScriptOpCode.ScrollScreenToEntity, ScrollScreenToEntityOpcodeHandler },
                       // { FieldScriptOpCode.ScrollScreenToPosition, ScrollScreenToPositionOpcodeHandler },
                       // { FieldScriptOpCode.ScrollScreenToLeader, ScrollScreenToLeaderOpcodeHandler },
                       // { FieldScriptOpCode.StartTheScreenToPositionEaseInOut, StartTheScreenToPositionEaseInOutOpcodeHandler },
                       // { FieldScriptOpCode.WaitForScrolling, WaitForScrollingOpcodeHandler },
                       // { FieldScriptOpCode.StartTheScreenToPositionLinear, StartTheScreenToPositionLinearOpcodeHandler },
                       // { FieldScriptOpCode.FadeScreenWait, FadeScreenWaitOpcodeHandler },
                       // { FieldScriptOpCode.WaitForFade, WaitForFadeOpcodeHandler },
                       // { FieldScriptOpCode.ScrollToPartyMember, ScrollToPartyMemberOpcodeHandler },
                       // { FieldScriptOpCode.MusicOperation, MusicOperationOpcodeHandler },
                       { FieldScriptOpCode.PlayMusic, PlayMusicOpcodeHandler },
                       { FieldScriptOpCode.PlaySound, PlaySoundOpcodeHandler },
                       // { FieldScriptOpCode.MusicLockMode, MusicLockModeOpcodeHandler },
                       // { FieldScriptOpCode.SetBattleMusic, SetBattleMusicOpcodeHandler },
                       // { FieldScriptOpCode.CheckIfMusicIsPlaying, CheckIfMusicIsPlayingOpcodeHandler },

                       // Uncategorized
                       // { FieldScriptOpCode.SetJumpFieldID, SetJumpFieldIDOpcodeHandler },
                   };
        }

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
            if (!TryReadScriptRequest(ctx, nameof(RunAnotherEntityScriptUnlessBusyOpcodeHandler), out FieldEntityRuntime target, out int targetScriptId, out byte priority))
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
            if (!TryReadScriptRequest(ctx, nameof(RunAnotherEntityScriptWaitUntilStartedOpcodeHandler), out FieldEntityRuntime target, out int targetScriptId, out byte priority))
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
            if (!TryReadScriptRequest(ctx, nameof(RunAnotherEntityScriptWaitUntilFinishedOpcodeHandler), out FieldEntityRuntime target, out int targetScriptId, out byte priority))
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
            ushort targetEventId = ReadUshort(ctx);

            FieldEntityRuntime entity = m_Entities[ctx.EntityId];

            if (!entity.TryGetScriptId(targetEventId, out int targetScriptId))
            {
                return;
            }

            m_Contexts.Remove((ctx.EntityId, ctx.Priority));

            entity.ReplaceScriptInSlot(targetScriptId, ctx.Priority);
        }

        /// <summary>
        /// Decode the operands shared by the three request opcodes and resolve the target entity.
        /// </summary>
        private bool TryReadScriptRequest(ScriptExecutionContext ctx, string caller, out FieldEntityRuntime target, out int targetScriptId, out byte priority)
        {
            byte   targetEntityId = ReadByte(ctx);
            ushort targetEventId;

            priority      = ReadByte(ctx);
            targetEventId = ReadUshort(ctx);

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

        private void GotoOpcodeHandler(ScriptExecutionContext ctx)
        {
            int offset = ReadInt(ctx);
            ctx.InstructionPointer += offset;
        }

        private void GotoDirectlyOpcodeHandler(ScriptExecutionContext ctx)
        {
            int offset = ReadInt(ctx);
            ctx.InstructionPointer = offset;
        }

        private void CompareTwoByteValuesOpcodeHandler(ScriptExecutionContext ctx)
        {
            byte sources = ReadByte(ctx);

            byte a = ReadOperandByte(ctx, GetFirstOperandSource(sources));
            byte b = ReadOperandByte(ctx, GetSecondOperandSource(sources));

            byte comparisonType = ReadByte(ctx);
            byte jumpAmount     = ReadByte(ctx);

            bool result = comparisonType switch
                          {
                              0x0 => a              == b,
                              0x1 => a              != b,
                              0x2 => a              > b,
                              0x3 => a              < b,
                              0x4 => a              >= b,
                              0x5 => a              <= b,
                              0x6 => (a & b)        != 0,
                              0x7 => (a ^ b)        != 0,
                              0x8 => (a | b)        != 0,
                              0x9 => (a & (1 << b)) != 0,
                              0xA => (a & (1 << b)) == 0,
                              _   => throw new InvalidOperationException($"{nameof(FieldVM)}::{nameof(CompareTwoByteValuesOpcodeHandler)} Unknown comparison type {comparisonType}")
                          };

            if (!result)
            {
                ctx.InstructionPointer += jumpAmount;
            }
        }

        private void CompareTwoIntValuesOpcodeHandler(ScriptExecutionContext ctx)
        {
            byte sources = ReadByte(ctx);

            int a = ReadOperandInt(ctx, GetFirstOperandSource(sources));
            int b = ReadOperandInt(ctx, GetSecondOperandSource(sources));

            byte comparisonType = ReadByte(ctx);
            byte jumpAmount     = ReadByte(ctx);

            bool result = comparisonType switch
                          {
                              0x0 => a              == b,
                              0x1 => a              != b,
                              0x2 => a              > b,
                              0x3 => a              < b,
                              0x4 => a              >= b,
                              0x5 => a              <= b,
                              0x6 => (a & b)        != 0,
                              0x7 => (a ^ b)        != 0,
                              0x8 => (a | b)        != 0,
                              0x9 => (a & (1 << b)) != 0,
                              0xA => (a & (1 << b)) == 0,
                              _   => throw new InvalidOperationException($"{nameof(FieldVM)}::{nameof(CompareTwoIntValuesOpcodeHandler)} Unknown comparison type {comparisonType}")
                          };

            if (!result)
            {
                ctx.InstructionPointer += jumpAmount;
            }
        }

        private void AssignValue8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsByte(ctx, out MemoryBank bank, out ushort address, out byte operand);

            m_MemoryService.WriteByte(bank, address, operand);
        }

        private void AssignValue16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsUshort(ctx, out MemoryBank bank, out ushort address, out ushort operand);

            m_MemoryService.WriteUshort(bank, address, operand);
        }

        private void Addition8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsByte(ctx, out MemoryBank bank, out ushort address, out byte operand);

            byte current = m_MemoryService.ReadByte(bank, address);
            byte result  = (byte)(current + operand);

            m_MemoryService.WriteByte(bank, address, result);
        }

        private void Addition16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsUshort(ctx, out MemoryBank bank, out ushort address, out ushort operand);

            ushort current = m_MemoryService.ReadUshort(bank, address);
            ushort result  = (ushort)(current + operand);

            m_MemoryService.WriteUshort(bank, address, result);
        }

        private void Addition8BitClampedOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsByte(ctx, out MemoryBank bank, out ushort address, out byte operand);

            int  current = m_MemoryService.ReadByte(bank, address);
            byte result  = (byte)Math.Min(current + operand, byte.MaxValue);

            m_MemoryService.WriteByte(bank, address, result);
        }

        private void Addition16BitClampedOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsUshort(ctx, out MemoryBank bank, out ushort address, out ushort operand);

            int    current = m_MemoryService.ReadUshort(bank, address);
            ushort result  = (ushort)Math.Min(current + operand, ushort.MaxValue);

            m_MemoryService.WriteUshort(bank, address, result);
        }

        private void Subtraction8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsByte(ctx, out MemoryBank bank, out ushort address, out byte operand);

            byte current = m_MemoryService.ReadByte(bank, address);
            byte result  = (byte)(current - operand);

            m_MemoryService.WriteByte(bank, address, result);
        }

        private void Subtraction16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsUshort(ctx, out MemoryBank bank, out ushort address, out ushort operand);

            ushort current = m_MemoryService.ReadUshort(bank, address);
            ushort result  = (ushort)(current - operand);

            m_MemoryService.WriteUshort(bank, address, result);
        }

        private void Subtraction8BitClampedOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsByte(ctx, out MemoryBank bank, out ushort address, out byte operand);

            int  current = m_MemoryService.ReadByte(bank, address);
            byte result  = (byte)Math.Max(current - operand, 0);

            m_MemoryService.WriteByte(bank, address, result);
        }

        private void Subtraction16BitClampedOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsUshort(ctx, out MemoryBank bank, out ushort address, out ushort operand);

            int    current = m_MemoryService.ReadUshort(bank, address);
            ushort result  = (ushort)Math.Max(current - operand, 0);

            m_MemoryService.WriteUshort(bank, address, result);
        }

        private void Multiplication8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsByte(ctx, out MemoryBank bank, out ushort address, out byte operand);

            byte current = m_MemoryService.ReadByte(bank, address);
            byte result  = (byte)(current * operand);

            m_MemoryService.WriteByte(bank, address, result);
        }

        private void Multiplication16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsUshort(ctx, out MemoryBank bank, out ushort address, out ushort operand);

            ushort current = m_MemoryService.ReadUshort(bank, address);
            ushort result  = (ushort)(current * operand);

            m_MemoryService.WriteUshort(bank, address, result);
        }

        private void Division8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsByte(ctx, out MemoryBank bank, out ushort address, out byte operand);

            if (operand == 0)
            {
                Debug.LogError($"{nameof(FieldVM)}::{nameof(Division8BitOpcodeHandler)} Divide by zero at [{bank}:{address}], leaving the value unchanged");
                return;
            }

            byte current = m_MemoryService.ReadByte(bank, address);
            byte result  = (byte)(current / operand);

            m_MemoryService.WriteByte(bank, address, result);
        }

        private void Division16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsUshort(ctx, out MemoryBank bank, out ushort address, out ushort operand);

            if (operand == 0)
            {
                Debug.LogError($"{nameof(FieldVM)}::{nameof(Division16BitOpcodeHandler)} Divide by zero at [{bank}:{address}], leaving the value unchanged");
                return;
            }

            ushort current = m_MemoryService.ReadUshort(bank, address);
            ushort result  = (ushort)(current / operand);

            m_MemoryService.WriteUshort(bank, address, result);
        }

        private void Remainder8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsByte(ctx, out MemoryBank bank, out ushort address, out byte operand);

            if (operand == 0)
            {
                Debug.LogError($"{nameof(FieldVM)}::{nameof(Remainder8BitOpcodeHandler)} Modulo by zero at [{bank}:{address}], leaving the value unchanged");
                return;
            }

            byte current = m_MemoryService.ReadByte(bank, address);
            byte result  = (byte)(current % operand);

            m_MemoryService.WriteByte(bank, address, result);
        }

        private void Remainder16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsUshort(ctx, out MemoryBank bank, out ushort address, out ushort operand);

            if (operand == 0)
            {
                Debug.LogError($"{nameof(FieldVM)}::{nameof(Remainder16BitOpcodeHandler)} Modulo by zero at [{bank}:{address}], leaving the value unchanged");
                return;
            }

            ushort current = m_MemoryService.ReadUshort(bank, address);
            ushort result  = (ushort)(current % operand);

            m_MemoryService.WriteUshort(bank, address, result);
        }

        private void BitwiseAnd8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsByte(ctx, out MemoryBank bank, out ushort address, out byte operand);

            byte current = m_MemoryService.ReadByte(bank, address);
            byte result  = (byte)(current & operand);

            m_MemoryService.WriteByte(bank, address, result);
        }

        private void BitwiseAnd16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsUshort(ctx, out MemoryBank bank, out ushort address, out ushort operand);

            ushort current = m_MemoryService.ReadUshort(bank, address);
            ushort result  = (ushort)(current & operand);

            m_MemoryService.WriteUshort(bank, address, result);
        }

        private void BitwiseOr8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsByte(ctx, out MemoryBank bank, out ushort address, out byte operand);

            byte current = m_MemoryService.ReadByte(bank, address);
            byte result  = (byte)(current | operand);

            m_MemoryService.WriteByte(bank, address, result);
        }

        private void BitwiseOr16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsUshort(ctx, out MemoryBank bank, out ushort address, out ushort operand);

            ushort current = m_MemoryService.ReadUshort(bank, address);
            ushort result  = (ushort)(current | operand);

            m_MemoryService.WriteUshort(bank, address, result);
        }

        private void BitwiseXor8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsByte(ctx, out MemoryBank bank, out ushort address, out byte operand);

            byte current = m_MemoryService.ReadByte(bank, address);
            byte result  = (byte)(current ^ operand);

            m_MemoryService.WriteByte(bank, address, result);
        }

        private void BitwiseXor16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsUshort(ctx, out MemoryBank bank, out ushort address, out ushort operand);

            ushort current = m_MemoryService.ReadUshort(bank, address);
            ushort result  = (ushort)(current ^ operand);

            m_MemoryService.WriteUshort(bank, address, result);
        }

        private void SetBitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsByte(ctx, out MemoryBank bank, out ushort address, out byte bitIndex);

            if (bitIndex > 7)
            {
                Debug.LogError($"{nameof(FieldVM)}::{nameof(SetBitOpcodeHandler)} Bit index [{bitIndex}] is out of range for a byte at [{bank}:{address}]");
                return;
            }

            byte current = m_MemoryService.ReadByte(bank, address);
            byte result  = (byte)(current | (1 << bitIndex));

            m_MemoryService.WriteByte(bank, address, result);
        }

        private void UnsetBitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsByte(ctx, out MemoryBank bank, out ushort address, out byte bitIndex);

            if (bitIndex > 7)
            {
                Debug.LogError($"{nameof(FieldVM)}::{nameof(UnsetBitOpcodeHandler)} Bit index [{bitIndex}] is out of range for a byte at [{bank}:{address}]");
                return;
            }

            byte current = m_MemoryService.ReadByte(bank, address);
            byte result  = (byte)(current & ~(1 << bitIndex));

            m_MemoryService.WriteByte(bank, address, result);
        }

        private void Increment8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            byte current = m_MemoryService.ReadByte(bank, address);
            byte result  = (byte)(current + 1);

            m_MemoryService.WriteByte(bank, address, result);
        }

        private void Increment16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            ushort current = m_MemoryService.ReadUshort(bank, address);
            ushort result  = (ushort)(current + 1);

            m_MemoryService.WriteUshort(bank, address, result);
        }

        private void Increment8BitClampedOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            byte current = m_MemoryService.ReadByte(bank, address);
            byte result  = current == byte.MaxValue ? current : (byte)(current + 1);

            m_MemoryService.WriteByte(bank, address, result);
        }

        private void Increment16BitClampedOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            ushort current = m_MemoryService.ReadUshort(bank, address);
            ushort result  = current == ushort.MaxValue ? current : (ushort)(current + 1);

            m_MemoryService.WriteUshort(bank, address, result);
        }

        private void Decrement8BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            byte current = m_MemoryService.ReadByte(bank, address);
            byte result  = (byte)(current - 1);

            m_MemoryService.WriteByte(bank, address, result);
        }

        private void Decrement16BitOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            ushort current = m_MemoryService.ReadUshort(bank, address);
            ushort result  = (ushort)(current - 1);

            m_MemoryService.WriteUshort(bank, address, result);
        }

        private void Decrement8BitClampedOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            byte current = m_MemoryService.ReadByte(bank, address);
            byte result  = current == byte.MinValue ? current : (byte)(current - 1);

            m_MemoryService.WriteByte(bank, address, result);
        }

        private void Decrement16BitClampedOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadDestination(ctx, out MemoryBank bank, out ushort address);

            ushort current = m_MemoryService.ReadUshort(bank, address);
            ushort result  = current == ushort.MinValue ? current : (ushort)(current - 1);

            m_MemoryService.WriteUshort(bank, address, result);
        }

        /// <summary>
        /// Write a random byte in <c>[0, operand)</c> to the destination. An operand of 0 is treated as a
        /// full byte range, so GET_RANDOM_NUMBER with no sensible bound still produces a value.
        /// </summary>
        private void GetRandomNumberOpcodeHandler(ScriptExecutionContext ctx)
        {
            ReadBinaryOperandsByte(ctx, out MemoryBank bank, out ushort address, out byte exclusiveMaximum);

            int  upperBound = exclusiveMaximum == 0 ? byte.MaxValue + 1 : exclusiveMaximum;
            byte result     = (byte)m_Random.Next(0, upperBound);

            m_MemoryService.WriteByte(bank, address, result);
        }

        /// <summary>
        /// Reseed the VM's random sequence, so a script can be made deterministic.
        /// </summary>
        private void RandomNumberSeedOpcodeHandler(ScriptExecutionContext ctx)
        {
            byte sources = ReadByte(ctx);
            int  seed    = ReadOperandInt(ctx, GetSecondOperandSource(sources));

            m_Random = new System.Random(seed);
        }

        private static void YieldOpcodeHandler(ScriptExecutionContext ctx)
        {
            ctx.Block(new WaitForFrameBlock());
        }

        private static void WaitSecondsOpcodeHandler(ScriptExecutionContext ctx)
        {
            float seconds = ReadFloat(ctx);
            ctx.Block(new WaitSecondsBlock(seconds));
        }

        private static void DoNothingOpcodeHandler(ScriptExecutionContext ctx)
        {
            // noop
        }

        private void ShowDialogueWindowOpcodeHandler(ScriptExecutionContext ctx)
        {
            ulong dialogueId    = ReadUlong(ctx);
            bool  blockMovement = ReadBool(ctx);

            RequestShowDialogueWindow?.Invoke(dialogueId, blockMovement);

            ctx.Block(new WaitUntilBlock(() => !IsDialogueWindowOpen(dialogueId)));
        }

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

            RequestAskPlayerToMakeAChoice?.Invoke(bank, addressToStoreChoice, dialogueId, answerIds);

            ctx.Block(new WaitUntilBlock(() => !IsPlayerMakingAChoice(dialogueId)));
        }

        private void MainMenuAccessibilityOpcodeHandler(ScriptExecutionContext ctx)
        {
            bool enabled = ReadBool(ctx);
            RequestSetMainMenuAccessibility?.Invoke(enabled);
        }

        private void CreateDialogueWindowOpcodeHandler(ScriptExecutionContext ctx)
        {
            ulong   dialogueId = ReadUlong(ctx);
            int     x          = ReadInt(ctx);
            int     y          = ReadInt(ctx);
            int     width      = ReadInt(ctx);
            int     height     = ReadInt(ctx);
            RectInt rect       = new RectInt(x, y, width, height);
            RequestCreateDialogueWindow?.Invoke(new DialogueWindowArgs(dialogueId, rect));
        }

        private void LockInputOpcodeHandler(ScriptExecutionContext ctx)
        {
            bool inputLocked = ReadBool(ctx);
            RequestInputLock?.Invoke(inputLocked);
        }

        private void SetBattleModeOptionsOpcodeHandler(ScriptExecutionContext ctx)
        {
            ushort arena       = ReadUshort(ctx);
            ushort enemyGroup  = ReadUshort(ctx);
            ushort battleFlags = ReadUshort(ctx);
            byte   enemyLevel  = ReadByte(ctx);

            BattleArgs args = new BattleArgs(arena, enemyGroup, (BattleFlags)battleFlags, enemyLevel);

            RequestSetBattleModeOptions?.Invoke(args);
        }

        private void JumpToAnotherMapOpcodeHandler(ScriptExecutionContext ctx)
        {
            int fieldIndex = ReadInt(ctx);
            int spawnId    = ReadInt(ctx);

            FieldArgs args = new FieldArgs(fieldIndex, spawnId);
            RequestFieldTransition?.Invoke(args);
        }

        private void StartBattleOpcodeHandler(ScriptExecutionContext ctx)
        {
            RequestStartBattle?.Invoke();
        }

        private void GatewayTriggerActivationOpcodeHandler(ScriptExecutionContext ctx)
        {
            bool enabled = ReadBool(ctx);
            RequestSetGatewayTriggersActive?.Invoke(enabled);
        }

        private void InteractabilityOpcodeHandler(ScriptExecutionContext ctx)
        {
            bool enabled = ReadBool(ctx);
            RequestSetInteractionTriggerActive?.Invoke(ctx.EntityId, enabled);
        }

        private void InitAsCharacterOpcodeHandler(ScriptExecutionContext ctx)
        {
            RequestSetPlayerEntity?.Invoke(m_Entities[ctx.EntityId]);
        }

        private void VisibilityOpcodeHandler(ScriptExecutionContext ctx)
        {
            bool isVisible = ReadBool(ctx);
            RequestSetEntityVisible?.Invoke(ctx.EntityId, isVisible);
        }

        private void SetEntityPositionOpcodeHandler(ScriptExecutionContext ctx)
        {
            Vector3 position = new Vector3(ReadFloat(ctx), ReadFloat(ctx), ReadFloat(ctx));
            RequestSetEntityPosition?.Invoke(ctx.EntityId, position);
        }

        private void SetMovementSpeedOpcodeHandler(ScriptExecutionContext ctx)
        {
            float movementSpeed = ReadFloat(ctx);
            RequestSetEntityMovementSpeed?.Invoke(ctx.EntityId, movementSpeed);
        }

        private void SetEntityRotationOpcodeHandler(ScriptExecutionContext ctx)
        {
            Quaternion rotation = Quaternion.Euler(ReadFloat(ctx), ReadFloat(ctx), ReadFloat(ctx));
            RequestSetEntityRotation?.Invoke(ctx.EntityId, rotation);
        }

        private void SetEntityRotationAsyncOpcodeHandler(ScriptExecutionContext ctx)
        {
            Quaternion            rotation     = Quaternion.Euler(ReadFloat(ctx), ReadFloat(ctx), ReadFloat(ctx));
            RotationDirection     direction    = (RotationDirection)ReadByte(ctx);
            float                 duration     = ReadFloat(ctx);
            RotationInterpolation rotationType = (RotationInterpolation)ReadByte(ctx);

            SetEntityRotationAsyncArgs args = new SetEntityRotationAsyncArgs(rotation, direction, duration, rotationType);

            RequestSetEntityRotationAsync?.Invoke(ctx.EntityId, args);

            ctx.Block(new WaitUntilBlock(() => !IsEntityRotating(ctx.EntityId)));
        }

        private void SetDirectionToFaceEntityOpcodeHandler(ScriptExecutionContext ctx)
        {
            byte targetEntityId = ReadByte(ctx);
            RequestSetEntityToFaceEntity?.Invoke(ctx.EntityId, targetEntityId);
        }

        private void SetInteractionRangeOpcodeHandler(ScriptExecutionContext ctx)
        {
            float radius = ReadFloat(ctx);
            RequestSetInteractionRange?.Invoke(ctx.EntityId, radius);
        }

        private void PlayMusicOpcodeHandler(ScriptExecutionContext ctx)
        {
            int id = ReadInt(ctx);
            RequestMusic?.Invoke(id);
        }

        private void PlaySoundOpcodeHandler(ScriptExecutionContext ctx)
        {
            int id = ReadInt(ctx);
            RequestSfx?.Invoke(id);
        }
    }
}