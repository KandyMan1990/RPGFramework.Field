namespace RPGFramework.Field
{
    /// <summary>
    /// Every field the game has, by its name's hash: the generated <c>FieldDatabase</c>, from export.
    /// </summary>
    public interface IFieldDatabase
    {
        /// <summary>The field with this name hash: its bundle, its location name and the sheets it loads.</summary>
        FieldDefinition Get(ulong fieldNameHash);
    }
}