using System;
using System.Buffers.Binary;
using RPGFramework.Core;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm
{
    /// <summary>The persistent and session banks, held in memory.</summary>
    internal sealed class FakeMemoryService : IMemoryService
    {
        private readonly byte[] m_Persistent;
        private readonly byte[] m_Session;

        internal FakeMemoryService(int persistentBytes, int sessionBytes)
        {
            m_Persistent = new byte[persistentBytes];
            m_Session    = new byte[sessionBytes];
        }

        byte IMemoryService.ReadByte(MemoryBank bank, ushort address)
        {
            return Bank(bank)[address];
        }

        void IMemoryService.WriteByte(MemoryBank bank, ushort address, byte value)
        {
            Bank(bank)[address] = value;
        }

        bool IMemoryService.ReadBool(MemoryBank bank, ushort address)
        {
            return Bank(bank)[address] != 0;
        }

        void IMemoryService.WriteBool(MemoryBank bank, ushort address, bool value)
        {
            Bank(bank)[address] = value ? (byte)1 : (byte)0;
        }

        ushort IMemoryService.ReadUshort(MemoryBank bank, ushort address)
        {
            return BinaryPrimitives.ReadUInt16LittleEndian(Span(bank, address));
        }

        void IMemoryService.WriteUshort(MemoryBank bank, ushort address, ushort value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(Span(bank, address), value);
        }

        int IMemoryService.ReadInt(MemoryBank bank, ushort address)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(Span(bank, address));
        }

        void IMemoryService.WriteInt(MemoryBank bank, ushort address, int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(Span(bank, address), value);
        }

        float IMemoryService.ReadFloat(MemoryBank bank, ushort address)
        {
            return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(Span(bank, address)));
        }

        void IMemoryService.WriteFloat(MemoryBank bank, ushort address, float value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(Span(bank, address), BitConverter.SingleToInt32Bits(value));
        }

        ulong IMemoryService.ReadUlong(MemoryBank bank, ushort address)
        {
            return BinaryPrimitives.ReadUInt64LittleEndian(Span(bank, address));
        }

        void IMemoryService.WriteUlong(MemoryBank bank, ushort address, ulong value)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(Span(bank, address), value);
        }

        private byte[] Bank(MemoryBank bank)
        {
            return bank == MemoryBank.Persistent ? m_Persistent : m_Session;
        }

        private Span<byte> Span(MemoryBank bank, ushort address)
        {
            return Bank(bank).AsSpan(address);
        }
    }
}
