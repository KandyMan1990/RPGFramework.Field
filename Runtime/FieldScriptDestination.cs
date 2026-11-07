using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field
{
    /// <summary>
    /// The variable one of a game's opcodes writes a result to, as <see cref="FieldScriptContext.ReadDestination" />
    /// read it from the script.
    /// </summary>
    public readonly struct FieldScriptDestination
    {
        internal readonly MemoryBank Bank;
        internal readonly ushort     Address;

        internal FieldScriptDestination(MemoryBank bank, ushort address)
        {
            Bank    = bank;
            Address = address;
        }
    }
}
