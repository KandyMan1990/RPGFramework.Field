using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RPGFramework.Audio;
using RPGFramework.Core;
using RPGFramework.Field.RPGFramework.Field;
using UnityEngine;

namespace RPGFramework.Field
{
    internal delegate Task          PlayMusicHandler(int id);
    internal delegate ISfxReference PlaySfxHandler(int   id);

    internal sealed class FieldVM
    {
        private delegate void OpcodeHandler(ScriptExecutionContext ctx);

        private const int INSTRUCTION_POINTER_SIZE = sizeof(FieldScriptOpCode);

        //(Owned by the VM for now)
        private readonly Dictionary<int, FieldScript> m_Scripts;

        private readonly Dictionary<FieldScriptOpCode, OpcodeHandler>                     m_OpcodeHandlers;
        private readonly Dictionary<(int entityId, int scriptId), ScriptExecutionContext> m_Contexts;
        private readonly IReadOnlyDictionary<int, FieldEntityRuntime>                     m_Entities;

        private PlayMusicHandler m_PlayMusicHandler;
        private PlaySfxHandler   m_PlaySfxHandler;

        internal FieldVM(IReadOnlyDictionary<int, FieldEntityRuntime> entities)
        {
            m_Contexts = new Dictionary<(int entityId, int scriptId), ScriptExecutionContext>();
            m_Entities = entities;

            m_OpcodeHandlers = BuildOpcodeHandlersArray();

            // Hardcoded test scripts
            m_Scripts = new Dictionary<int, FieldScript>
                        {
                                { 0, new FieldScript(CreateTestEntityScript1()) },
                                { 1, new FieldScript(CreateTestEntityScript2()) }
                        };
        }

        private static byte[] CreateTestEntityScript1()
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);

            bw.Write((ushort)FieldScriptOpCode.PlayMusic);
            bw.Write(0);
            bw.Write((ushort)FieldScriptOpCode.Return);

            bw.Flush();
            return ms.ToArray();
        }

        private static byte[] CreateTestEntityScript2()
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);

            bw.Write((ushort)FieldScriptOpCode.Yield);
            bw.Write((ushort)FieldScriptOpCode.WaitSeconds);
            bw.Write(2.5f);
            bw.Write((ushort)FieldScriptOpCode.DoNothing);
            bw.Write((ushort)FieldScriptOpCode.Return);

            bw.Flush();
            return ms.ToArray();
        }

        internal void SetCallbackHandlers(PlayMusicHandler playMusicHandler, PlaySfxHandler playSfxHandler)
        {
            m_PlayMusicHandler = playMusicHandler;
            m_PlaySfxHandler   = playSfxHandler;
        }

        private bool IsScriptRunning(int entityId, int scriptId)
        {
            return m_Contexts.ContainsKey((entityId, scriptId));
        }

        internal void Execute(int entityId, int scriptId, FieldEntityRuntime entity)
        {
            (int entityId, int scriptId) key = (entityId, scriptId);

            if (!m_Contexts.TryGetValue(key, out ScriptExecutionContext ctx))
            {
                ctx = new ScriptExecutionContext
                      {
                              ScriptId           = scriptId,
                              InstructionPointer = 0
                      };
                m_Contexts[key] = ctx;
            }

            if (ctx.IsBlocked())
            {
                return;
            }

            while (!ctx.IsBlocked())
            {
                FieldScriptOpCode opcode = FetchOpcode(ctx);

                OpcodeHandler opcodeHandler = m_OpcodeHandlers[opcode];
                // uncomment when testing new handlers
                // Debug.Log($"Opcode: {opcode}, InstructionPointer: {ctx.InstructionPointer}");
                opcodeHandler(ctx);

                if (opcode == FieldScriptOpCode.Return)
                {
                    m_Contexts.Remove(key);
                    entity.OnScriptFinished();
                    return;
                }
            }
        }

        private FieldScriptOpCode FetchOpcode(ScriptExecutionContext ctx)
        {
            FieldScript script = m_Scripts[ctx.ScriptId];

            using MemoryStream ms = new MemoryStream(script.Bytecode);
            using BinaryReader br = new BinaryReader(ms);

            br.BaseStream.Seek(ctx.InstructionPointer, SeekOrigin.Begin);

            ctx.InstructionPointer += INSTRUCTION_POINTER_SIZE;
            return (FieldScriptOpCode)br.ReadUInt16();
        }

        private byte ReadByte(ScriptExecutionContext ctx)
        {
            FieldScript script = m_Scripts[ctx.ScriptId];

            using MemoryStream ms = new MemoryStream(script.Bytecode);
            using BinaryReader br = new BinaryReader(ms);

            br.BaseStream.Seek(ctx.InstructionPointer, SeekOrigin.Begin);

            ctx.InstructionPointer += sizeof(byte);
            return br.ReadByte();
        }

        private ushort ReadUshort(ScriptExecutionContext ctx)
        {
            FieldScript script = m_Scripts[ctx.ScriptId];

            using MemoryStream ms = new MemoryStream(script.Bytecode);
            using BinaryReader br = new BinaryReader(ms);

            br.BaseStream.Seek(ctx.InstructionPointer, SeekOrigin.Begin);

            ctx.InstructionPointer += sizeof(ushort);
            return br.ReadUInt16();
        }

        private int ReadInt(ScriptExecutionContext ctx)
        {
            FieldScript script = m_Scripts[ctx.ScriptId];

            using MemoryStream ms = new MemoryStream(script.Bytecode);
            using BinaryReader br = new BinaryReader(ms);

            br.BaseStream.Seek(ctx.InstructionPointer, SeekOrigin.Begin);

            ctx.InstructionPointer += sizeof(int);
            return br.ReadInt32();
        }

        private float ReadFloat(ScriptExecutionContext ctx)
        {
            FieldScript script = m_Scripts[ctx.ScriptId];

            using MemoryStream ms = new MemoryStream(script.Bytecode);
            using BinaryReader br = new BinaryReader(ms);

            br.BaseStream.Seek(ctx.InstructionPointer, SeekOrigin.Begin);

            ctx.InstructionPointer += sizeof(float);
            return br.ReadSingle();
        }

        private static async Task WaitForScriptStartAsync(byte targetEntityId, byte targetScriptId)
        {
            // TODO
        }

        private static async Task WaitForScriptFinishCondition(byte targetEntityId, byte targetScriptId)
        {
            // TODO
        }

        private static async Task AwaitNextFrameAsync()
        {
            await Awaitable.NextFrameAsync();
        }

        private static async Task WaitForSecondsAsync(float seconds)
        {
            await Awaitable.WaitForSecondsAsync(seconds);
        }

        private Dictionary<FieldScriptOpCode, OpcodeHandler> BuildOpcodeHandlersArray()
        {
            return new Dictionary<FieldScriptOpCode, OpcodeHandler>
                   {
                           { FieldScriptOpCode.Return, ReturnOpcodeHandler },
                           { FieldScriptOpCode.RunAnotherEntityScriptUnlessBusy, RunAnotherEntityScriptUnlessBusyOpcodeHandler },
                           { FieldScriptOpCode.RunAnotherEntityScriptWaitUntilStarted, RunAnotherEntityScriptWaitUntilStartedOpcodeHandler },
                           { FieldScriptOpCode.RunAnotherEntityScriptWaitUntilFinished, RunAnotherEntityScriptWaitUntilFinishedOpcodeHandler },
                           // { FieldScriptOpCode.RunPartyMemberScriptUnlessBusy, RunPartyMemberScriptUnlessBusyOpcodeHandler },
                           // { FieldScriptOpCode.RunPartyMemberScriptWaitUntilStarted, RunPartyMemberScriptWaitUntilStartedOpcodeHandler },
                           // { FieldScriptOpCode.RunPartyMemberScriptWaitUntilFinished, RunPartyMemberScriptWaitUntilFinishedOpcodeHandler },
                           // { FieldScriptOpCode.ReturnToAnotherScript, ReturnToAnotherScriptOpcodeHandler },
                           // { FieldScriptOpCode.GotoForward, GotoForwardOpcodeHandler },
                           // { FieldScriptOpCode.GotoForwardLong, GotoForwardLongOpcodeHandler },
                           // { FieldScriptOpCode.GotoBackward, GotoBackwardOpcodeHandler },
                           // { FieldScriptOpCode.GotoBackwardLong, GotoBackwardLongOpcodeHandler },
                           // { FieldScriptOpCode.CompareTwoValuesU8, CompareTwoValuesU8OpcodeHandler },
                           // { FieldScriptOpCode.CompareTwoValuesU16, CompareTwoValuesU16OpcodeHandler },
                           // { FieldScriptOpCode.CompareTwoValues16Bit, CompareTwoValues16BitOpcodeHandler },
                           // { FieldScriptOpCode.CompareTwoValues16BitBigJump, CompareTwoValues16BitBigJumpOpcodeHandler },
                           // { FieldScriptOpCode.CompareTwoValuesU32, CompareTwoValuesU32OpcodeHandler },
                           // { FieldScriptOpCode.CompareTwoValuesU32BitBigJump, CompareTwoValuesU32BitBigJumpOpcodeHandler },
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
                           // { FieldScriptOpCode.SetBattleModeOptions, SetBattleModeOptionsOpcodeHandler },
                           // { FieldScriptOpCode.LoadResultOfLastBattle, LoadResultOfLastBattleOpcodeHandler },
                           // { FieldScriptOpCode.SetBattleEncounterTable, SetBattleEncounterTableOpcodeHandler },
                           // { FieldScriptOpCode.JumpToAnotherMap, JumpToAnotherMapOpcodeHandler },
                           // { FieldScriptOpCode.GetLastFieldMap, GetLastFieldMapOpcodeHandler },
                           // { FieldScriptOpCode.StartBattle, StartBattleOpcodeHandler },
                           // { FieldScriptOpCode.RandomEncounters, RandomEncountersOpcodeHandler },
                           // { FieldScriptOpCode.SetBattleModeOptionsAgain, SetBattleModeOptionsAgainOpcodeHandler },
                           // { FieldScriptOpCode.GatewayTriggerActivation, GatewayTriggerActivationOpcodeHandler },
                           // { FieldScriptOpCode.GameOver, GameOverOpcodeHandler },

                           // Assignment and Mathematics
                           // { FieldScriptOpCode.Addition8BitClamped, Addition8BitClampedOpcodeHandler },
                           // { FieldScriptOpCode.Addition16BitClamped, Addition16BitClampedOpcodeHandler },
                           // { FieldScriptOpCode.Subtraction8BitClamped, Subtraction8BitClampedOpcodeHandler },
                           // { FieldScriptOpCode.Subtraction16BitClamped, Subtraction16BitClampedOpcodeHandler },
                           // { FieldScriptOpCode.Increment8BitClamped, Increment8BitClampedOpcodeHandler },
                           // { FieldScriptOpCode.Increment16BitClamped, Increment16BitClampedOpcodeHandler },
                           // { FieldScriptOpCode.Decrement8BitClamped, Decrement8BitClampedOpcodeHandler },
                           // { FieldScriptOpCode.Decrement16BitClamped, Decrement16BitClampedOpcodeHandler },
                           // { FieldScriptOpCode.RandomNumberSeed, RandomNumberSeedOpcodeHandler },
                           // { FieldScriptOpCode.AssignValue8Bit, AssignValue8BitOpcodeHandler },
                           // { FieldScriptOpCode.AssignValue16Bit, AssignValue16BitOpcodeHandler },
                           // { FieldScriptOpCode.SetBit, SetBitOpcodeHandler },
                           // { FieldScriptOpCode.UnsetBit, UnsetBitOpcodeHandler },
                           // { FieldScriptOpCode.Unused, UnusedOpcodeHandler },
                           // { FieldScriptOpCode.Addition8Bit, Addition8BitOpcodeHandler },
                           // { FieldScriptOpCode.Addition16Bit, Addition16BitOpcodeHandler },
                           // { FieldScriptOpCode.Subtraction8Bit, Subtraction8BitOpcodeHandler },
                           // { FieldScriptOpCode.Subtraction16Bit, Subtraction16BitOpcodeHandler },
                           // { FieldScriptOpCode.Multiplication8Bit, Multiplication8BitOpcodeHandler },
                           // { FieldScriptOpCode.Multiplication16Bit, Multiplication16BitOpcodeHandler },
                           // { FieldScriptOpCode.Division8Bit, Division8BitOpcodeHandler },
                           // { FieldScriptOpCode.Division16Bit, Division16BitOpcodeHandler },
                           // { FieldScriptOpCode.Remainder8Bit, Remainder8BitOpcodeHandler },
                           // { FieldScriptOpCode.Remainder16Bit, Remainder16BitOpcodeHandler },
                           // { FieldScriptOpCode.BitwiseAnd8Bit, BitwiseAnd8BitOpcodeHandler },
                           // { FieldScriptOpCode.BitwiseAnd16Bit, BitwiseAnd16BitOpcodeHandler },
                           // { FieldScriptOpCode.BitwiseOr8Bit, BitwiseOr8BitOpcodeHandler },
                           // { FieldScriptOpCode.BitwiseOr16Bit, BitwiseOr16BitOpcodeHandler },
                           // { FieldScriptOpCode.BitwiseXor8Bit, BitwiseXor8BitOpcodeHandler },
                           // { FieldScriptOpCode.BitwiseXor16Bit, BitwiseXor16BitOpcodeHandler },
                           // { FieldScriptOpCode.Increment8Bit, Increment8BitOpcodeHandler },
                           // { FieldScriptOpCode.Increment16Bit, Increment16BitOpcodeHandler },
                           // { FieldScriptOpCode.Decrement8Bit, Decrement8BitOpcodeHandler },
                           // { FieldScriptOpCode.Decrement16Bit, Decrement16BitOpcodeHandler },
                           // { FieldScriptOpCode.GetRandomNumber, GetRandomNumberOpcodeHandler },
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
                           // { FieldScriptOpCode.ShowWindowWithText, ShowWindowWithTextOpcodeHandler },
                           // { FieldScriptOpCode.SetWindowTextValue, SetWindowTextValueOpcodeHandler },
                           // { FieldScriptOpCode.SetWindowTextValue16Bit, SetWindowTextValue16BitOpcodeHandler },
                           // { FieldScriptOpCode.SetMapNameInMenu, SetMapNameInMenuOpcodeHandler },
                           // { FieldScriptOpCode.AskPlayerToMakeAChoice, AskPlayerToMakeAChoiceOpcodeHandler },
                           // { FieldScriptOpCode.MenuOperations, MenuOperationsOpcodeHandler },
                           // { FieldScriptOpCode.MainMenuAccessibility, MainMenuAccessibilityOpcodeHandler },
                           // { FieldScriptOpCode.CreateWindow, CreateWindowOpcodeHandler },
                           // { FieldScriptOpCode.SetWindowPosition, SetWindowPositionOpcodeHandler },
                           // { FieldScriptOpCode.SetWindowModes, SetWindowModesOpcodeHandler },
                           // { FieldScriptOpCode.ResetWindow, ResetWindowOpcodeHandler },
                           // { FieldScriptOpCode.CloseWindowAgain, CloseWindowAgainOpcodeHandler },
                           // { FieldScriptOpCode.SetNumberOfRowsInWindow, SetNumberOfRowsInWindowOpcodeHandler },

                           // Party and Inventory
                           // { FieldScriptOpCode.ChangePartyMembers, ChangePartyMembersOpcodeHandler },
                           // { FieldScriptOpCode.StorePartyMembers, StorePartyMembersOpcodeHandler },
                           // { FieldScriptOpCode.IncreaseGil, IncreaseGilOpcodeHandler },
                           // { FieldScriptOpCode.DecreaseGil, DecreaseGilOpcodeHandler },
                           // { FieldScriptOpCode.GetGilAmount, GetGilAmountOpcodeHandler },
                           // { FieldScriptOpCode.Unused1, Unused1OpcodeHandler },
                           // { FieldScriptOpCode.Unused2, Unused2OpcodeHandler },
                           // { FieldScriptOpCode.RestoreHPMP, RestoreHPMPOpcodeHandler },
                           // { FieldScriptOpCode.RestoreHPMPAgain, RestoreHPMPAgainOpcodeHandler },
                           // { FieldScriptOpCode.IncreaseMP, IncreaseMPOpcodeHandler },
                           // { FieldScriptOpCode.DecreaseMP, DecreaseMPOpcodeHandler },
                           // { FieldScriptOpCode.IncreaseHP, IncreaseHPOpcodeHandler },
                           // { FieldScriptOpCode.DecreaseHP, DecreaseHPOpcodeHandler },
                           // { FieldScriptOpCode.AddItemToInventory, AddItemToInventoryOpcodeHandler },
                           // { FieldScriptOpCode.RemoveItemFromInventory, RemoveItemFromInventoryOpcodeHandler },
                           // { FieldScriptOpCode.GetItemCountFromInventory, GetItemCountFromInventoryOpcodeHandler },
                           // { FieldScriptOpCode.AddMateriaToInventory, AddMateriaToInventoryOpcodeHandler },
                           // { FieldScriptOpCode.RemoveMateriaFromInventory, RemoveMateriaFromInventoryOpcodeHandler },
                           // { FieldScriptOpCode.MateriaOpC, MateriaOpCOpcodeHandler },
                           // { FieldScriptOpCode.GetPartyMembersIdentity, GetPartyMembersIdentityOpcodeHandler },
                           // { FieldScriptOpCode.AddCharacterToParty, AddCharacterToPartyOpcodeHandler },
                           // { FieldScriptOpCode.RemoveCharacterFromParty, RemoveCharacterFromPartyOpcodeHandler },
                           // { FieldScriptOpCode.SetAllPartyCharacters, SetAllPartyCharactersOpcodeHandler },
                           // { FieldScriptOpCode.IfCharacterIsInPartyAgain, IfCharacterIsInPartyAgainOpcodeHandler },
                           // { FieldScriptOpCode.IfCharacterIsAvailableAgain, IfCharacterIsAvailableAgainOpcodeHandler },
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
                           // { FieldScriptOpCode.PlayerMovability, PlayerMovabilityOpcodeHandler },
                           // { FieldScriptOpCode.FaceCharacter, FaceCharacterOpcodeHandler },
                           // { FieldScriptOpCode.TurnToPartyMember, TurnToPartyMemberOpcodeHandler },
                           // { FieldScriptOpCode.CollisionDetection, CollisionDetectionOpcodeHandler },
                           // { FieldScriptOpCode.GetPartyMemberDirection, GetPartyMemberDirectionOpcodeHandler },
                           // { FieldScriptOpCode.GetPartyMemberPosition, GetPartyMemberPositionOpcodeHandler },
                           // { FieldScriptOpCode.Interactibility, InteractibilityOpcodeHandler },
                           // { FieldScriptOpCode.InitAsFieldModel, InitAsFieldModelOpcodeHandler },
                           // { FieldScriptOpCode.InitAsCharacter, InitAsCharacterOpcodeHandler },
                           // { FieldScriptOpCode.PlayAnimationLooping, PlayAnimationLoopingOpcodeHandler },
                           // { FieldScriptOpCode.PlayAnimationOnceAndWait, PlayAnimationOnceAndWaitOpcodeHandler },
                           // { FieldScriptOpCode.Visibility, VisibilityOpcodeHandler },
                           // { FieldScriptOpCode.SetEntityLocationXYZI, SetEntityLocationXYZIOpcodeHandler },
                           // { FieldScriptOpCode.SetEntityLocationXYI, SetEntityLocationXYIOpcodeHandler },
                           // { FieldScriptOpCode.SetEntityLocationXYZ, SetEntityLocationXYZOpcodeHandler },
                           // { FieldScriptOpCode.MoveEntityToXYWalkAnimation, MoveEntityToXYWalkAnimationOpcodeHandler },
                           // { FieldScriptOpCode.MoveEntityToXYNoAnimation, MoveEntityToXYNoAnimationOpcodeHandler },
                           // { FieldScriptOpCode.MoveEntityToAnotherEntity, MoveEntityToAnotherEntityOpcodeHandler },
                           // { FieldScriptOpCode.TurnEntityToAnotherEntity, TurnEntityToAnotherEntityOpcodeHandler },
                           // { FieldScriptOpCode.WaitForAnimation, WaitForAnimationOpcodeHandler },
                           // { FieldScriptOpCode.MoveFieldObject, MoveFieldObjectOpcodeHandler },
                           // { FieldScriptOpCode.PlayAnimationAsync, PlayAnimationAsyncOpcodeHandler },
                           // { FieldScriptOpCode.PlayAnimationOnceAsync, PlayAnimationOnceAsyncOpcodeHandler },
                           // { FieldScriptOpCode.PlayPartialAnimation, PlayPartialAnimationOpcodeHandler },
                           // { FieldScriptOpCode.PlayPartialAnimationAgain, PlayPartialAnimationAgainOpcodeHandler },
                           // { FieldScriptOpCode.SetMovementSpeed, SetMovementSpeedOpcodeHandler },
                           // { FieldScriptOpCode.SetFacingDirection, SetFacingDirectionOpcodeHandler },
                           // { FieldScriptOpCode.RotateModel, RotateModelOpcodeHandler },
                           // { FieldScriptOpCode.SetDirectionToFaceEntity, SetDirectionToFaceEntityOpcodeHandler },
                           // { FieldScriptOpCode.GetEntityDirection, GetEntityDirectionOpcodeHandler },
                           // { FieldScriptOpCode.GetEntityLocationXY, GetEntityLocationXYOpcodeHandler },
                           // { FieldScriptOpCode.GetEntityDirectionI, GetEntityDirectionIOpcodeHandler },
                           // { FieldScriptOpCode.PlayAnimationStopOnLastFrameWait, PlayAnimationStopOnLastFrameWaitOpcodeHandler },
                           // { FieldScriptOpCode.PlayAnimationToDo, PlayAnimationToDoOpcodeHandler },
                           // { FieldScriptOpCode.PlayAnimationToDoAgain, PlayAnimationToDoAgainOpcodeHandler },
                           // { FieldScriptOpCode.SetAnimationSpeed, SetAnimationSpeedOpcodeHandler },
                           // { FieldScriptOpCode.SetEntityAsControllableCharacter, SetEntityAsControllableCharacterOpcodeHandler },
                           // { FieldScriptOpCode.MakeEntityJump, MakeEntityJumpOpcodeHandler },
                           // { FieldScriptOpCode.GetEntityPositionXYZI, GetEntityPositionXYZIOpcodeHandler },
                           // { FieldScriptOpCode.ClimbLadder, ClimbLadderOpcodeHandler },
                           // { FieldScriptOpCode.TransposeObjectVisualizationOnly, TransposeObjectVisualizationOnlyOpcodeHandler },
                           // { FieldScriptOpCode.WaitForTranspose, WaitForTransposeOpcodeHandler },
                           // { FieldScriptOpCode.SetInteractibilityRadius, SetInteractibilityRadiusOpcodeHandler },
                           // { FieldScriptOpCode.SetCollisionRadius, SetCollisionRadiusOpcodeHandler },
                           // { FieldScriptOpCode.Collidability, CollidabilityOpcodeHandler },
                           // { FieldScriptOpCode.LineTriggerInitialization, LineTriggerInitializationOpcodeHandler },
                           // { FieldScriptOpCode.LineTriggerActivation, LineTriggerActivationOpcodeHandler },
                           // { FieldScriptOpCode.SetLine, SetLineOpcodeHandler },
                           // { FieldScriptOpCode.SetInteractibilityRadiusAgain, SetInteractibilityRadiusAgainOpcodeHandler },
                           // { FieldScriptOpCode.SetCollisionRadiusAgain, SetCollisionRadiusAgainOpcodeHandler },
                           // { FieldScriptOpCode.FixFacingForward, FixFacingForwardOpcodeHandler },
                           // { FieldScriptOpCode.SetAnimationID, SetAnimationIDOpcodeHandler },
                           // { FieldScriptOpCode.StopAnimation, StopAnimationOpcodeHandler },
                           // { FieldScriptOpCode.WaitForTurn, WaitForTurnOpcodeHandler },

                           // Background and Palette
                           // { FieldScriptOpCode.SetBackgroundDepth, SetBackgroundDepthOpcodeHandler },
                           // { FieldScriptOpCode.ScrollBackground, ScrollBackgroundOpcodeHandler },
                           // { FieldScriptOpCode.MultiplyPaletteColors, MultiplyPaletteColorsOpcodeHandler },
                           // { FieldScriptOpCode.BackgroundOn, BackgroundOnOpcodeHandler },
                           // { FieldScriptOpCode.BackgroundOff, BackgroundOffOpcodeHandler },
                           // { FieldScriptOpCode.BackgroundRollForward, BackgroundRollForwardOpcodeHandler },
                           // { FieldScriptOpCode.BackgroundRollBackward, BackgroundRollBackwardOpcodeHandler },
                           // { FieldScriptOpCode.BackgroundClear, BackgroundClearOpcodeHandler },
                           // { FieldScriptOpCode.StorePalette, StorePaletteOpcodeHandler },
                           // { FieldScriptOpCode.LoadPalette, LoadPaletteOpcodeHandler },
                           // { FieldScriptOpCode.CopyPalette, CopyPaletteOpcodeHandler },
                           // { FieldScriptOpCode.CopyPalettePartial, CopyPalettePartialOpcodeHandler },
                           // { FieldScriptOpCode.AddToPaletteColorValues, AddToPaletteColorValuesOpcodeHandler },
                           // { FieldScriptOpCode.MultiplyPaletteColorValues, MultiplyPaletteColorValuesOpcodeHandler },
                           // { FieldScriptOpCode.StorePaletteOffset, StorePaletteOffsetOpcodeHandler },
                           // { FieldScriptOpCode.LoadPaletteOffset, LoadPaletteOffsetOpcodeHandler },
                           // { FieldScriptOpCode.CopyPaletteAgain, CopyPaletteAgainOpcodeHandler },
                           // { FieldScriptOpCode.ReturnPalette, ReturnPaletteOpcodeHandler },
                           // { FieldScriptOpCode.AddPalette, AddPaletteOpcodeHandler },

                           // Camera, Audio and Video
                           // { FieldScriptOpCode.FadeScreen, FadeScreenOpcodeHandler },
                           // { FieldScriptOpCode.ShakeScreen, ShakeScreenOpcodeHandler },
                           // { FieldScriptOpCode.ScrollScreen, ScrollScreenOpcodeHandler },
                           // { FieldScriptOpCode.ScrollScreenAgain, ScrollScreenAgainOpcodeHandler },
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
                           // { FieldScriptOpCode.MusicOperationAgain, MusicOperationAgainOpcodeHandler },
                           // { FieldScriptOpCode.MusicVT, MusicVTOpcodeHandler },
                           // { FieldScriptOpCode.MusicVM, MusicVMOpcodeHandler },
                           // { FieldScriptOpCode.MusicLockMode, MusicLockModeOpcodeHandler },
                           // { FieldScriptOpCode.SetBattleMusic, SetBattleMusicOpcodeHandler },
                           // { FieldScriptOpCode.Unknown, UnknownOpcodeHandler },
                           // { FieldScriptOpCode.MusicOpF, MusicOpFOpcodeHandler },
                           // { FieldScriptOpCode.MusicOpC, MusicOpCOpcodeHandler },
                           // { FieldScriptOpCode.CheckIfMusicIsPlaying, CheckIfMusicIsPlayingOpcodeHandler },

                           // Uncategorized
                           // { FieldScriptOpCode.Something, SomethingOpcodeHandler },
                           // { FieldScriptOpCode.SomethingAgain, SomethingAgainOpcodeHandler },
                           // { FieldScriptOpCode.SetX, SetXOpcodeHandler },
                           // { FieldScriptOpCode.GetX, GetXOpcodeHandler },
                           // { FieldScriptOpCode.SearchForValueInData, SearchForValueInDataOpcodeHandler },
                           // { FieldScriptOpCode.SetJumpFieldID, SetJumpFieldIDOpcodeHandler },
                           // { FieldScriptOpCode.SetJumpFieldIDAgain, SetJumpFieldIDAgainOpcodeHandler },
                   };
        }

        private static void ReturnOpcodeHandler(ScriptExecutionContext ctx)
        {
            // noop
        }

        private void RunAnotherEntityScriptUnlessBusyOpcodeHandler(ScriptExecutionContext ctx)
        {
            byte targetEntityId = ReadByte(ctx);
            byte targetScriptId = ReadByte(ctx);

            if (!IsScriptRunning(targetEntityId, targetScriptId))
            {
                m_Entities[targetEntityId].RequestScript(targetScriptId);
            }
        }

        private void RunAnotherEntityScriptWaitUntilStartedOpcodeHandler(ScriptExecutionContext ctx)
        {
            byte targetEntityId = ReadByte(ctx);
            byte targetScriptId = ReadByte(ctx);

            m_Entities[targetEntityId].RequestScript(targetScriptId);

            ctx.Block(WaitForScriptStartAsync(targetEntityId, targetScriptId));
        }

        private void RunAnotherEntityScriptWaitUntilFinishedOpcodeHandler(ScriptExecutionContext ctx)
        {
            byte targetEntityId = ReadByte(ctx);
            byte targetScriptId = ReadByte(ctx);

            m_Entities[targetEntityId].RequestScript(targetScriptId);

            ctx.Block(WaitForScriptFinishCondition(targetEntityId, targetScriptId));
        }

        private static void YieldOpcodeHandler(ScriptExecutionContext ctx)
        {
            ctx.Block(AwaitNextFrameAsync());
        }

        private void WaitSecondsOpcodeHandler(ScriptExecutionContext ctx)
        {
            float seconds = ReadFloat(ctx);
            ctx.Block(WaitForSecondsAsync(seconds));
        }

        private static void DoNothingOpcodeHandler(ScriptExecutionContext ctx)
        {
            // noop
        }

        private void PlayMusicOpcodeHandler(ScriptExecutionContext ctx)
        {
            int id = ReadInt(ctx);
            m_PlayMusicHandler(id).FireAndForget();
        }

        private void PlaySoundOpcodeHandler(ScriptExecutionContext ctx)
        {
            int id = ReadInt(ctx);
            m_PlaySfxHandler(id);
        }
    }
}