namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// The fields' bundles, built as the Field Designer's Export builds them, for a game's own build steps and tests: every
    /// field the Field Designer lists checked and bundled, the generated database script left as it is, so nothing
    /// recompiles. A field added or renamed needs Export itself, which writes the database.
    /// </summary>
    public interface IFieldBundleExport
    {
        /// <summary>Checks every field and builds the bundles; the problems that stopped it, none when it built.</summary>
        string[] BuildBundles();
    }

    /// <summary>The project's fields, from the Field Designer's data.</summary>
    public sealed class FieldBundleExport : IFieldBundleExport
    {
        string[] IFieldBundleExport.BuildBundles()
        {
            FieldDesignerData data = FieldDesignerDataUtility.FindData();

            if (data == null)
            {
                return new[] { "the project has no Field Designer data: open the Field Designer to make it" };
            }

            string[] problems = data.FieldDatabase.BuildBundles();

            data.Save();

            return problems;
        }
    }
}
