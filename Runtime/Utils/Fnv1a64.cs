using System.Text;

namespace RPGFramework.Field.Utils
{
    // TODO: create a package
    internal static class Fnv1a64
    {
        private const ulong FNV_OFFSET_BASIS = 14695981039346656037UL;
        private const ulong FNV_PRIME        = 1099511628211UL;

        internal static ulong Hash(string input)
        {
            if (input == null)
            {
                return 0;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(input);

            return Hash(bytes);
        }

        private static ulong Hash(byte[] bytes)
        {
            if (bytes == null)
            {
                return 0;
            }

            ulong hash = FNV_OFFSET_BASIS;

            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= FNV_PRIME;
            }

            return hash;
        }
    }
}