namespace RPGFramework.Field
{
    public interface IFieldDatabase
    {
        FieldDefinition Get(ulong fieldNameHash);
    }
}