namespace RPGFramework.Field
{
    public interface IFieldDatabase
    {
        FieldDatabaseAsset Get(ulong fieldNameHash);
    }
}