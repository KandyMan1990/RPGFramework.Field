using System;
using System.Buffers.Binary;
using RPGFramework.Core;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.BlockState;
#if UNITY_EDITOR
using UnityEngine;
#endif

namespace RPGFramework.Field
{
    /// <summary>
    /// One running script, as every opcode's handler is given it, the framework's and a game's alike: its arguments,
    /// read in order with the read their <see cref="ArgumentType" /> takes, and the variables it writes results to.
    /// There is one per occupied priority slot, but only the entity's most urgent slot advances in a frame; the rest hold
    /// their place until the ones above them return.
    /// </summary>
    public sealed class FieldScriptContext
    {
        // Argument sources are packed two to a byte: the high nibble selects where the first argument
        // (the destination, for anything that writes) comes from, the low nibble the second.
        //
        //   0 = immediate, the value follows inline in the bytecode
        //   1 = Persistent bank  2 = Session bank      3 = Temp
        //
        // Bank addresses are ushort, matching IMemoryService, so a script can reach any variable the
        // variable map declares.
        internal const byte ARGUMENT_IMMEDIATE = 0;

        internal readonly int    EntityId;
        internal readonly byte   Priority;
        internal readonly int    ScriptId;
        internal readonly byte[] Bytecode;

        internal int  InstructionPointer;
        internal bool YieldRequested;

        private readonly IMemoryService m_MemoryService;
        private readonly int            m_TempBytes;

        private TempMemory  m_Temp;
        private IBlockState m_BlockingState;

        // A sources byte precedes every pair of arguments that can come from a variable, so the second argument of a
        // pair uses the low nibble left over from the first.
        private bool m_HasPendingSource;
        private byte m_PendingSource;

#if UNITY_EDITOR
        private ushort             m_OpCode;
        private GameArgumentRead[] m_Reads;
        private string             m_ScriptName;
        private int                m_Next;
        private bool               m_Failed;
#endif

        internal FieldScriptContext(int entityId, byte priority, int scriptId, byte[] bytecode, IMemoryService memoryService, int tempBytes)
        {
            EntityId           = entityId;
            Priority           = priority;
            ScriptId           = scriptId;
            Bytecode           = bytecode;
            InstructionPointer = 0;
            YieldRequested     = false;
            m_MemoryService    = memoryService;
            m_TempBytes        = tempBytes;
        }

        /// <summary>A <see cref="ArgumentType.Bool" />, written or from a variable.</summary>
        public bool ReadBool()
        {
#if UNITY_EDITOR
            if (!Expect(GameArgumentRead.Bool))
            {
                return default;
            }
#endif
            byte source = NextSource();

            return source == ARGUMENT_IMMEDIATE ? NextBool() : ReadVariableBool(ToMemoryBank(source), NextUshort());
        }

        /// <summary>
        /// A <see cref="ArgumentType.Byte" />, <see cref="ArgumentType.EntityId" />, <see cref="ArgumentType.Priority" />,
        /// <see cref="ArgumentType.BlockerId" /> or <see cref="ArgumentType.CameraId" />, written or from a variable.
        /// </summary>
        public byte ReadByte()
        {
#if UNITY_EDITOR
            if (!Expect(GameArgumentRead.Byte))
            {
                return default;
            }
#endif
            byte source = NextSource();

            return source == ARGUMENT_IMMEDIATE ? NextByte() : ReadVariableByte(ToMemoryBank(source), NextUshort());
        }

        /// <summary>A <see cref="ArgumentType.UShort" /> or <see cref="ArgumentType.EventId" />, written or from a variable.</summary>
        public ushort ReadUShort()
        {
#if UNITY_EDITOR
            if (!Expect(GameArgumentRead.UShort))
            {
                return default;
            }
#endif
            byte source = NextSource();

            return source == ARGUMENT_IMMEDIATE ? NextUshort() : ReadVariableUshort(ToMemoryBank(source), NextUshort());
        }

        /// <summary>
        /// An <see cref="ArgumentType.Int" />, <see cref="ArgumentType.Seconds" /> or <see cref="ArgumentType.SpawnId" />,
        /// written or from a variable.
        /// </summary>
        public int ReadInt()
        {
#if UNITY_EDITOR
            if (!Expect(GameArgumentRead.Int))
            {
                return default;
            }
#endif
            byte source = NextSource();

            return source == ARGUMENT_IMMEDIATE ? NextInt() : ReadVariableInt(ToMemoryBank(source), NextUshort());
        }

        /// <summary>A <see cref="ArgumentType.Float" />, written or from a variable.</summary>
        public float ReadFloat()
        {
#if UNITY_EDITOR
            if (!Expect(GameArgumentRead.Float))
            {
                return default;
            }
#endif
            byte source = NextSource();

            return source == ARGUMENT_IMMEDIATE ? NextFloat() : ReadVariableFloat(ToMemoryBank(source), NextUshort());
        }

        /// <summary>The hash of a name or a localisation key: a field, a track, a sound, an action and the rest.</summary>
        public ulong ReadName()
        {
#if UNITY_EDITOR
            if (!Expect(GameArgumentRead.Name))
            {
                return default;
            }
#endif
            return NextUlong();
        }

        /// <summary>A <see cref="ArgumentType.Variable" />, the variable a result is written to.</summary>
        public FieldScriptDestination ReadDestination()
        {
#if UNITY_EDITOR
            if (!Expect(GameArgumentRead.Destination))
            {
                return default;
            }
#endif
            MemoryBank bank = ToMemoryBank(NextSource());

            return new FieldScriptDestination(bank, NextUshort());
        }

        /// <summary>Writes a result to a destination declared <c>Bool</c>.</summary>
        public void WriteBool(FieldScriptDestination destination, bool value)
        {
            WriteVariableBool(destination.Bank, destination.Address, value);
        }

        /// <summary>Writes a result to a destination declared <c>Byte</c>.</summary>
        public void WriteByte(FieldScriptDestination destination, byte value)
        {
            WriteVariableByte(destination.Bank, destination.Address, value);
        }

        /// <summary>Writes a result to a destination declared <c>UShort</c>.</summary>
        public void WriteUShort(FieldScriptDestination destination, ushort value)
        {
            WriteVariableUshort(destination.Bank, destination.Address, value);
        }

        /// <summary>Writes a result to a destination declared <c>Int</c>.</summary>
        public void WriteInt(FieldScriptDestination destination, int value)
        {
            WriteVariableInt(destination.Bank, destination.Address, value);
        }

        /// <summary>Writes a result to a destination declared <c>Float</c>.</summary>
        public void WriteFloat(FieldScriptDestination destination, float value)
        {
            WriteVariableFloat(destination.Bank, destination.Address, value);
        }

        /// <summary>
        /// Starts the opcode at the instruction pointer: its arguments' first sources byte is still to be read. In the
        /// editor, a game's opcode's reads are checked against its attributes, as no test of the framework's can check a
        /// game's handler.
        /// </summary>
        internal void BeginInstruction(ushort opCode)
        {
            m_HasPendingSource = false;

#if UNITY_EDITOR
            m_OpCode = opCode;
            m_Next   = 0;
            m_Failed = false;

            if (FieldScriptOpCodeRanges.IsGameOpCode(opCode) && FieldOpCodeTables.TryGetGameOpCode(opCode, out DescribedGameOpCode described))
            {
                m_Reads      = described.Reads;
                m_ScriptName = described.ScriptName;
            }
            else
            {
                m_Reads      = null;
                m_ScriptName = null;
            }
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Whether a game's handler read every argument, each as described; logs it when not. A handler that read
        /// otherwise has left the instruction pointer inside its arguments.
        /// </summary>
        internal bool ReadAsDescribed()
        {
            if (m_Reads != null && !m_Failed && m_Next < m_Reads.Length)
            {
                Fail($"left argument {m_Next} of its {m_Reads.Length} unread: a handler reads every argument, in order, whether it uses it or not");
            }

            return !m_Failed;
        }

        private bool Expect(GameArgumentRead read)
        {
            if (m_Reads == null)
            {
                return true;
            }

            if (m_Failed)
            {
                return false;
            }

            if (m_Next >= m_Reads.Length)
            {
                Fail($"read argument {m_Next} as {read}, but its attributes describe {m_Reads.Length}");
                return false;
            }

            if (m_Reads[m_Next] != read)
            {
                Fail($"read argument {m_Next} as {read}, which its attributes describe as {m_Reads[m_Next]}");
                return false;
            }

            m_Next++;

            return true;
        }

        private void Fail(string problem)
        {
            m_Failed = true;

            Debug.LogError($"{m_ScriptName} (0x{m_OpCode:X4})'s handler {problem}. The script stops here");
        }
#endif

        internal void Block(IBlockState blockingState)
        {
            m_BlockingState = blockingState;
        }

        internal bool IsBlocked()
        {
            return m_BlockingState != null;
        }

        internal void UpdateBlock(float deltaTime)
        {
            if (m_BlockingState == null)
            {
                return;
            }

            m_BlockingState.Update(deltaTime);

            if (m_BlockingState.IsComplete)
            {
                m_BlockingState = null;
            }
        }

        internal static byte GetFirstArgumentSource(byte sources)
        {
            byte source = (byte)(sources >> 4);

            return source;
        }

        internal static byte GetSecondArgumentSource(byte sources)
        {
            byte source = (byte)(sources & 0x0F);

            return source;
        }

        internal static MemoryBank ToMemoryBank(byte source)
        {
            if (source == ARGUMENT_IMMEDIATE)
            {
                throw new InvalidOperationException($"{nameof(FieldScriptContext)}::{nameof(ToMemoryBank)} Argument source [{source}] is an immediate value, not a bank. A destination argument must name a bank");
            }

            MemoryBank bank = (MemoryBank)(source - 1);

            return bank;
        }

        private byte NextSource()
        {
            if (m_HasPendingSource)
            {
                m_HasPendingSource = false;

                return m_PendingSource;
            }

            byte packed = NextByte();

            m_PendingSource    = GetSecondArgumentSource(packed);
            m_HasPendingSource = true;

            byte source = GetFirstArgumentSource(packed);

            return source;
        }

        // Bytecode reads are chosen for .NET's CoreCLR: an int and a ulong are one load, a byte and a ushort index a span.
        internal byte NextByte()
        {
            ReadOnlySpan<byte> bytecode = Bytecode.AsSpan();

            byte value = bytecode[InstructionPointer];
            InstructionPointer += sizeof(byte);

            return value;
        }

        internal bool NextBool()
        {
            ReadOnlySpan<byte> bytecode = Bytecode.AsSpan();

            byte value = bytecode[InstructionPointer];
            InstructionPointer += sizeof(byte);

            return value != 0;
        }

        internal ushort NextUshort()
        {
            ReadOnlySpan<byte> bytecode = Bytecode.AsSpan();

            ushort value = (ushort)(bytecode[InstructionPointer] | bytecode[InstructionPointer + 1] << 8);

            InstructionPointer += sizeof(ushort);

            return value;
        }

        internal int NextInt()
        {
            int value = BinaryPrimitives.ReadInt32LittleEndian(Bytecode.AsSpan(InstructionPointer));

            InstructionPointer += sizeof(int);

            return value;
        }

        internal float NextFloat()
        {
            int   bits  = NextInt();
            float value = BitConverter.Int32BitsToSingle(bits);

            return value;
        }

        internal ulong NextUlong()
        {
            ulong value = BinaryPrimitives.ReadUInt64LittleEndian(Bytecode.AsSpan(InstructionPointer));

            InstructionPointer += sizeof(ulong);

            return value;
        }

        // Created on first use: most scripts never touch temp memory.
        private TempMemory Temp => m_Temp ??= new TempMemory(m_TempBytes);

        // Temp variables live in the running script's own memory; Persistent and Session in the memory service.
        internal byte ReadVariableByte(MemoryBank bank, ushort address)
        {
            return bank == MemoryBank.Temp ? Temp.ReadByte(address) : m_MemoryService.ReadByte(bank, address);
        }

        internal void WriteVariableByte(MemoryBank bank, ushort address, byte value)
        {
            if (bank == MemoryBank.Temp)
            {
                Temp.WriteByte(address, value);
                return;
            }

            m_MemoryService.WriteByte(bank, address, value);
        }

        internal bool ReadVariableBool(MemoryBank bank, ushort address)
        {
            return bank == MemoryBank.Temp ? Temp.ReadBool(address) : m_MemoryService.ReadBool(bank, address);
        }

        internal void WriteVariableBool(MemoryBank bank, ushort address, bool value)
        {
            if (bank == MemoryBank.Temp)
            {
                Temp.WriteBool(address, value);
                return;
            }

            m_MemoryService.WriteBool(bank, address, value);
        }

        internal ushort ReadVariableUshort(MemoryBank bank, ushort address)
        {
            return bank == MemoryBank.Temp ? Temp.ReadUshort(address) : m_MemoryService.ReadUshort(bank, address);
        }

        internal void WriteVariableUshort(MemoryBank bank, ushort address, ushort value)
        {
            if (bank == MemoryBank.Temp)
            {
                Temp.WriteUshort(address, value);
                return;
            }

            m_MemoryService.WriteUshort(bank, address, value);
        }

        internal int ReadVariableInt(MemoryBank bank, ushort address)
        {
            return bank == MemoryBank.Temp ? Temp.ReadInt(address) : m_MemoryService.ReadInt(bank, address);
        }

        internal void WriteVariableInt(MemoryBank bank, ushort address, int value)
        {
            if (bank == MemoryBank.Temp)
            {
                Temp.WriteInt(address, value);
                return;
            }

            m_MemoryService.WriteInt(bank, address, value);
        }

        internal float ReadVariableFloat(MemoryBank bank, ushort address)
        {
            return bank == MemoryBank.Temp ? Temp.ReadFloat(address) : m_MemoryService.ReadFloat(bank, address);
        }

        internal void WriteVariableFloat(MemoryBank bank, ushort address, float value)
        {
            if (bank == MemoryBank.Temp)
            {
                Temp.WriteFloat(address, value);
                return;
            }

            m_MemoryService.WriteFloat(bank, address, value);
        }

        internal ulong ReadVariableUlong(MemoryBank bank, ushort address)
        {
            return bank == MemoryBank.Temp ? Temp.ReadUlong(address) : m_MemoryService.ReadUlong(bank, address);
        }

        internal void WriteVariableUlong(MemoryBank bank, ushort address, ulong value)
        {
            if (bank == MemoryBank.Temp)
            {
                Temp.WriteUlong(address, value);
                return;
            }

            m_MemoryService.WriteUlong(bank, address, value);
        }

        // A variable's bits at its width, zero-extended.
        internal ulong ReadVariableBits(MemoryBank bank, ushort address, VariableWidth width)
        {
            ulong bits = width.GetByteCount() switch
                         {
                             1 => ReadVariableByte(bank, address),
                             2 => ReadVariableUshort(bank, address),
                             4 => (uint)ReadVariableInt(bank, address),
                             _ => ReadVariableUlong(bank, address)
                         };

            return bits;
        }

        internal void WriteVariableBits(MemoryBank bank, ushort address, VariableWidth width, ulong bits)
        {
            switch (width.GetByteCount())
            {
                case 1:
                    WriteVariableByte(bank, address, (byte)bits);
                    return;

                case 2:
                    WriteVariableUshort(bank, address, (ushort)bits);
                    return;

                case 4:
                    WriteVariableInt(bank, address, unchecked((int)bits));
                    return;

                default:
                    WriteVariableUlong(bank, address, bits);
                    return;
            }
        }
    }
}
