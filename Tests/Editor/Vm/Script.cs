using System;
using System.Collections.Generic;
using RPGFramework.Core.Memory;

namespace RPGFramework.Field.Tests.Vm
{
    /// <summary>Bytecode as the compiler writes it: each opcode, then its arguments, little-endian.</summary>
    internal sealed class Script
    {
        private readonly List<byte> m_Bytes;

        internal Script()
        {
            m_Bytes = new List<byte>();
        }

        internal int Length => m_Bytes.Count;

        internal Script Op(FieldScriptOpCode opCode)
        {
            return UShort((ushort)opCode);
        }

        internal Script Byte(byte value)
        {
            m_Bytes.Add(value);

            return this;
        }

        internal Script Bytes(params byte[] values)
        {
            m_Bytes.AddRange(values);

            return this;
        }

        internal Script Bool(bool value)
        {
            return Byte(value ? (byte)1 : (byte)0);
        }

        internal Script UShort(ushort value)
        {
            return Bytes(BitConverter.GetBytes(value));
        }

        internal Script Int(int value)
        {
            return Bytes(BitConverter.GetBytes(value));
        }

        internal Script UInt(uint value)
        {
            return Bytes(BitConverter.GetBytes(value));
        }

        internal Script Long(long value)
        {
            return Bytes(BitConverter.GetBytes(value));
        }

        internal Script ULong(ulong value)
        {
            return Bytes(BitConverter.GetBytes(value));
        }

        internal Script Float(float value)
        {
            return Bytes(BitConverter.GetBytes(value));
        }

        /// <summary>One sources byte: the first argument's source in the high nibble, the second's in the low.</summary>
        internal Script Sources(Source first, Source second)
        {
            return Byte((byte)(((int)first << 4) | (int)second));
        }

        /// <summary>A value read from a variable: its width, then its address.</summary>
        internal Script Variable(VariableWidth width, ushort address)
        {
            return Byte((byte)width).UShort(address);
        }

        internal Script Return()
        {
            return Op(FieldScriptOpCode.Return);
        }

        internal Script Yield()
        {
            return Op(FieldScriptOpCode.Yield);
        }

        /// <summary>A jump by <paramref name="offset" /> bytes from the end of the jump.</summary>
        internal Script Goto(int offset)
        {
            return Op(FieldScriptOpCode.GotoJump).Int(offset);
        }

        /// <summary>A request for one of an entity's scripts, by its event.</summary>
        internal Script Request(FieldScriptOpCode opCode, byte entityId, ushort eventId)
        {
            return Op(opCode).Sources(Source.Immediate, Source.Immediate).Byte(entityId).UShort(eventId);
        }

        /// <summary>Hands this script's slot to another of its entity's scripts, by its event.</summary>
        internal Script ReturnTo(ushort eventId)
        {
            return Op(FieldScriptOpCode.ReturnToAnotherScript).Sources(Source.Immediate, Source.Immediate).UShort(eventId);
        }

        /// <summary>Sets the persistent byte at <paramref name="address" /> to the value.</summary>
        internal Script SetByte(ushort address, byte value)
        {
            return Op(FieldScriptOpCode.SetByte).Sources(Source.Persistent, Source.Immediate).UShort(address).Byte(value);
        }

        /// <summary>
        /// Adds one to the persistent int at <paramref name="address" />, once for each of <paramref name="times" />
        /// instructions, so how many ran is read off the count.
        /// </summary>
        internal Script Count(ushort address, int times)
        {
            for (int i = 0; i < times; i++)
            {
                Op(FieldScriptOpCode.IncrementInt).Sources(Source.Persistent, Source.Immediate).UShort(address);
            }

            return this;
        }

        internal byte[] ToArray()
        {
            return m_Bytes.ToArray();
        }
    }
}
