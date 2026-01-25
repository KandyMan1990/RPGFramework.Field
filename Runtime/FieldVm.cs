using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGFramework.Audio;
using Unity.Mathematics;

namespace RPGFramework.Field
{
    internal delegate void OpCodeHandler(ref FieldVmContext ctx);

    /*  Op Code Parameter Conventions
        | Type     | Size | Notes                   |
        | -------- | ---- | ----------------------- |
        | `byte`   | 1    | IDs, enums              |
        | `ushort` | 2    | Flags, vars, script IDs |
        | `short`  | 2    | Signed deltas           |
        | `uint`   | 4    | Jump offsets            |
        | `int`    | 4    | Values                  |
        | `float`  | 4    | Seconds, speeds         |
        | `bool`   | 1    | Rare, explicit          |
     */

    /*  Op Code Method Signatures
        // Offset is relative to next instruction

        Nop [ushort opcode]
        End [ushort opcode]
        Call [ushort opcode] [ushort scriptId]
        Jump [ushort opcode] [int relativeOffset]

        IfFlagSet [ushort opcode] [ushort flagId] [int relativeOffset]
        IfFlagClear [ushort opcode] [ushort flagId] [int relativeOffset]
        IfVarEqual [ushort opcode] [ushort varId] [int compareValue] [int relativeOffset]

        SetFlag [ushort opcode] [ushort flagId]
        ClearFlag [ushort opcode] [ushort flagId]
        SetVar [ushort opcode] [ushort varId] [int value]
        AddVar [ushort opcode] [ushort varId] [int delta]
        CopyVar [ushort opcode] [ushort srcVarId] [ushort dstVarId]
        RandomToVar [ushort opcode] [int min] [int max] [ushort varId]

        WaitSeconds [ushort opcode] [float seconds]
        WaitForInput [ushort opcode] [ushort inputMask]
        SetTimer [ushort opcode] [ushort timerId] [float seconds]
        GetTimer [ushort opcode] [ushort timerId] [ushort varId]

        MoveNpc [ushort opcode] [ushort npcId] [short dx] [short dy]
        MoveNpcAbs [ushort opcode] [ushort npcId] [short x] [short y]

        FaceNpc [ushort opcode] [ushort npcId] [byte direction]
        SetNpcSpeed [ushort opcode] [ushort npcId] [float speed]

        PlayBgm [ushort opcode] [ushort bgmId]
        StopBgm [ushort opcode] [float fadeSeconds]
        PlaySfx [ushort opcode] [ushort sfxId]

        ShowText [ushort opcode] [ushort textId]
        ShowTextSync [ushort opcode] [ushort textId]
        AskChoice [ushort opcode] [ushort choiceId] [ushort resultVarId]

        Scroll2D [ushort opcode] [short dx] [short dy] [float seconds]
        ShakeScreen [ushort opcode] [float intensity] [float seconds]
     */

    internal sealed class FieldVm
    {
        private readonly Dictionary<FieldScriptOpcode, OpCodeHandler> m_OpCodeTable;

        private readonly IMusicPlayer m_MusicPlayer;
        private readonly ISfxPlayer   m_SfxPlayer;

        internal FieldVm(IMusicPlayer musicPlayer, ISfxPlayer sfxPlayer)
        {
            m_MusicPlayer = musicPlayer;
            m_SfxPlayer   = sfxPlayer;

            m_OpCodeTable = CreateOpCodeTable();
        }

        internal void Update(ref FieldVmContext ctx, float deltaTime)
        {
            if (ctx.IsSuspended)
            {
                ctx.WaitSeconds = math.max(0f, ctx.WaitSeconds - deltaTime);
            }

            Execute(ref ctx);
        }

        private void Execute(ref FieldVmContext ctx)
        {
            if (!ctx.IsActive)
            {
                return;
            }

            if (ctx.IsSuspended && !ctx.TryResume())
            {
                return;
            }

            while (!ctx.IsSuspended)
            {
                FieldScriptOpcode opcode = (FieldScriptOpcode)ReadUShort(ref ctx);
                if (!m_OpCodeTable.TryGetValue(opcode, out OpCodeHandler handler))
                {
                    throw new InvalidOperationException($"{nameof(FieldVm)}::{nameof(Execute)}: Unknown opcode [{opcode}]");
                }

                handler(ref ctx);

                if (!ctx.IsActive)
                {
                    return;
                }
            }
        }

        private Dictionary<FieldScriptOpcode, OpCodeHandler> CreateOpCodeTable()
        {
            return new Dictionary<FieldScriptOpcode, OpCodeHandler>
                   {
                           // { FieldScriptOpcode.Nop, HandleNop },
                           { FieldScriptOpcode.End, HandleEnd },
                           { FieldScriptOpcode.Call, HandleCall },
                           { FieldScriptOpcode.Jump, HandleJump },

                           { FieldScriptOpcode.IfFlagSet, HandleIfFlagSet },
                           { FieldScriptOpcode.IfFlagClear, HandleIfFlagClear },

                           { FieldScriptOpcode.IfVarEqual, HandleIfVarEqual },
                           { FieldScriptOpcode.IfVarNotEqual, HandleIfVarNotEqual },
                           // { FieldScriptOpcode.IfVarGreater, HandleIfVarGreater },
                           // { FieldScriptOpcode.IfVarGreaterEqual, HandleIfVarGreaterEqual },
                           // { FieldScriptOpcode.IfVarLess, HandleIfVarLess },
                           // { FieldScriptOpcode.IfVarLessEqual, HandleIfVarLessEqual },

                           { FieldScriptOpcode.SetFlag, HandleSetFlag },
                           { FieldScriptOpcode.ClearFlag, HandleClearFlag },

                           { FieldScriptOpcode.SetVar, HandleSetVar },
                           { FieldScriptOpcode.AddVar, HandleAddVar },
                           // { FieldScriptOpcode.CopyVar, HandleCopyVar },

                           // { FieldScriptOpcode.RandomToVar, HandleRandomToVar },

                           { FieldScriptOpcode.WaitSeconds, HandleWaitSeconds },
                           // { FieldScriptOpcode.WaitForInput, HandleWaitForInput },
                           { FieldScriptOpcode.Yield, HandleYield },

                           // { FieldScriptOpcode.SetTimer, HandleSetTimer },
                           // { FieldScriptOpcode.GetTimer, HandleGetTimer },

                           // { FieldScriptOpcode.MoveNpc, HandleMoveNpc },
                           // { FieldScriptOpcode.MoveNpcAbs, HandleMoveNpcAbs },
                           // { FieldScriptOpcode.FaceNpc, HandleFaceNpc },
                           // { FieldScriptOpcode.SetNpcSpeed, HandleSetNpcSpeed },

                           // { FieldScriptOpcode.PlayerJump, HandlePlayerJump },
                           // { FieldScriptOpcode.LockPlayer, HandleLockPlayer },
                           // { FieldScriptOpcode.UnlockPlayer, HandleUnlockPlayer },

                           { FieldScriptOpcode.PlayBgm, HandlePlayBgm },
                           // { FieldScriptOpcode.StopBgm, HandleStopBgm },
                           { FieldScriptOpcode.PlaySfx, HandlePlaySfx },

                           // { FieldScriptOpcode.ShowText, HandleShowText },
                           // { FieldScriptOpcode.ShowTextSync, HandleShowTextSync },
                           // { FieldScriptOpcode.AskChoice, HandleAskChoice },
                           //
                           // { FieldScriptOpcode.WindowSize, HandleWindowSize },
                           // { FieldScriptOpcode.WindowClose, HandleWindowClose },
                           // { FieldScriptOpcode.SetMessSpeed, HandleSetMessSpeed },
                           //
                           // { FieldScriptOpcode.OffsetModel, HandleOffsetModel },
                           // { FieldScriptOpcode.Scroll2D, HandleScroll2D },
                           // { FieldScriptOpcode.ShakeScreen, HandleShakeScreen },
                   };
        }

        private static ushort ReadUShort(ref FieldVmContext ctx)
        {
            ushort value = BitConverter.ToUInt16(ctx.Script, ctx.InstructionPointer);
            ctx.InstructionPointer += 2;
            return value;
        }

        private static int ReadInt(ref FieldVmContext ctx)
        {
            int value = BitConverter.ToInt32(ctx.Script, ctx.InstructionPointer);
            ctx.InstructionPointer += 4;
            return value;
        }

        private static float ReadFloat(ref FieldVmContext ctx)
        {
            float value = BitConverter.ToSingle(ctx.Script, ctx.InstructionPointer);
            ctx.InstructionPointer += 4;
            return value;
        }

        private static void HandleEnd(ref FieldVmContext ctx)
        {
            if (ctx.HasReturnAddress)
            {
                ctx.InstructionPointer = ctx.PopReturnAddress();
                return;
            }

            ctx.IsActive = false;
        }

        private static void HandleCall(ref FieldVmContext ctx)
        {
            ushort scriptId = ReadUShort(ref ctx);

            FieldScriptInfo target = ctx.Field.GetScript(scriptId);

            ctx.PushReturnAddress(ctx.InstructionPointer);

            ctx.InstructionPointer = (int)target.Offset;
        }

        private static void HandleJump(ref FieldVmContext ctx)
        {
            int offset = ReadInt(ref ctx);
            ctx.InstructionPointer += offset;
        }

        private static void HandleIfFlagSet(ref FieldVmContext ctx)
        {
            ushort flagId = ReadUShort(ref ctx);
            int    offset = ReadInt(ref ctx);

            if (ctx.Field.GetFlag(flagId))
            {
                ctx.InstructionPointer += offset;
            }
        }

        private static void HandleIfFlagClear(ref FieldVmContext ctx)
        {
            ushort flagId = ReadUShort(ref ctx);
            int    offset = ReadInt(ref ctx);

            if (!ctx.Field.GetFlag(flagId))
            {
                ctx.InstructionPointer += offset;
            }
        }

        private static void HandleIfVarEqual(ref FieldVmContext ctx)
        {
            ushort id      = ReadUShort(ref ctx);
            int    compare = ReadInt(ref ctx);
            int    offset  = ReadInt(ref ctx);

            if (ctx.Field.GetVar(id) == compare)
            {
                ctx.InstructionPointer += offset;
            }
        }

        private static void HandleIfVarNotEqual(ref FieldVmContext ctx)
        {
            ushort id      = ReadUShort(ref ctx);
            int    compare = ReadInt(ref ctx);
            int    offset  = ReadInt(ref ctx);

            if (ctx.Field.GetVar(id) != compare)
            {
                ctx.InstructionPointer += offset;
            }
        }

        private static void HandleSetFlag(ref FieldVmContext ctx)
        {
            ushort id = ReadUShort(ref ctx);
            ctx.Field.SetFlag(id);
        }

        private static void HandleClearFlag(ref FieldVmContext ctx)
        {
            ushort id = ReadUShort(ref ctx);
            ctx.Field.ClearFlag(id);
        }

        private static void HandleSetVar(ref FieldVmContext ctx)
        {
            ushort id    = ReadUShort(ref ctx);
            int    value = ReadInt(ref ctx);
            ctx.Field.SetVar(id, value);
        }

        private static void HandleAddVar(ref FieldVmContext ctx)
        {
            ushort id    = ReadUShort(ref ctx);
            int    delta = ReadInt(ref ctx);
            ctx.Field.AddVar(id, delta);
        }

        private static void HandleWaitSeconds(ref FieldVmContext ctx)
        {
            float seconds = ReadFloat(ref ctx);
            ctx.IsSuspended = true;
            ctx.WaitSeconds = seconds;
        }

        private static void HandleYield(ref FieldVmContext ctx)
        {
            ctx.IsSuspended = true;
            ctx.WaitSeconds = 0f;
        }

        private void HandlePlayBgm(ref FieldVmContext ctx)
        {
            int  id   = ReadInt(ref ctx);
            Task task = m_MusicPlayer.Play(id);
            ctx.BlockUntil(task);
        }

        private void HandlePlaySfx(ref FieldVmContext ctx)
        {
            int id = ReadInt(ref ctx);
            m_SfxPlayer.Play(id);
        }
    }
}