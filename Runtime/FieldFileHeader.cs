using System.Runtime.InteropServices;

namespace RPGFramework.Field
{
    /// <summary>
    /// Script Format
    /// [ScriptCount] repeat ScriptCount [ScriptId] [Offset] [Length] - [Bytes...]
    /// Entity Format
    /// [EntityCount] repeat EntityCount [EntityId] [ScriptCount] repeat ScriptCount [ScriptId] [EntryScriptId]
    /// Text Format
    /// [TextCount] repeat TextCount [Offset] [Length] (This should just be ulong Hash)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct FieldFileHeader
    {
        internal byte Version;
        internal uint ScriptBlockOffset;
        internal uint ScriptBlockSize;
        internal uint EntityTableOffset;
        internal uint EntityTableSize;
        internal uint TextTableOffset;
        internal uint TextTableSize;
    }
}