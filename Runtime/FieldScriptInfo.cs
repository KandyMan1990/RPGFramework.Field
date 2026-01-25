namespace RPGFramework.Field
{
    internal struct FieldScriptInfo
    {
        internal ushort ScriptId;
        internal uint   Offset;
        internal uint   Length;

        internal FieldScriptInfo(ushort scriptId, uint offset, uint length)
        {
            ScriptId = scriptId;
            Offset   = offset;
            Length   = length;
        }
    }
}