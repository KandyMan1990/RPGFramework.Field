using System.IO;

namespace RPGFramework.Field.Helpers
{
    internal static class HelperFunctions
    {
        internal static string CombinePath(params string[] parts)
        {
#if UNITY_ANDROID || UNITY_WEBGL
            return string.Join('/', parts.Select(p => p.Trim('/')));
#else
            return Path.Combine(parts);
#endif
        }
    }
}