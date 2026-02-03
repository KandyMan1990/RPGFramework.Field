using System;
using System.Text;

namespace RPGFramework.Field
{
    internal static class FieldProvider
    {
        internal const int FieldNameSize = 64;

        internal static byte[] ToBytes(string value)
        {
            byte[] buffer    = new byte[FieldNameSize];
            byte[] nameBytes = Encoding.UTF8.GetBytes(value);
            
            if (nameBytes.Length > FieldNameSize)
            {
                throw new Exception($"Field name too long: {value}");
            }
            
            Array.Copy(nameBytes, buffer, nameBytes.Length);
            
            return buffer;
        }

        internal static string FromBytes(byte[] buffer)
        {
            return Encoding.UTF8.GetString(buffer).TrimEnd('\0');
        }
    }
}