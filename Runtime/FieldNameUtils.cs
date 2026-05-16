using System;
using System.Text;

namespace RPGFramework.Field
{
    public static class FieldNameUtils
    {
        internal const int FIELD_NAME_SIZE = 64;

        public static byte[] ToBytes(string value)
        {
            byte[] buffer    = new byte[FIELD_NAME_SIZE];
            byte[] nameBytes = Encoding.UTF8.GetBytes(value);

            if (nameBytes.Length > FIELD_NAME_SIZE)
            {
                throw new Exception($"Field name too long: {value}");
            }

            Array.Copy(nameBytes, buffer, nameBytes.Length);

            return buffer;
        }

        public static string FromBytes(byte[] buffer)
        {
            return FromBytes(buffer.AsSpan());
        }

        public static string FromBytes(ReadOnlySpan<byte> buffer)
        {
            int length = buffer.IndexOf((byte)0);
            if (length == -1)
            {
                length = buffer.Length;
            }

            return Encoding.UTF8.GetString(buffer[..length]);
        }

        public static string[] FromBytes(byte[] buffer, int count)
        {
            string[] strings = new string[count];

            for (int i = 0; i < count; i++)
            {
                ReadOnlySpan<byte> bytes = buffer.AsSpan(i * FIELD_NAME_SIZE, FIELD_NAME_SIZE);

                strings[i] = FromBytes(bytes);
            }

            return strings;
        }
    }
}