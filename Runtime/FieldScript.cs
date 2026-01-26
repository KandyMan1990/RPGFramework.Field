namespace RPGFramework.Field
{
    //(Temporary, Hardcoded)
    internal sealed class FieldScript
    {
        internal readonly byte[] Bytecode;

        internal FieldScript(byte[] bytecode)
        {
            Bytecode = bytecode;
        }
    }
}