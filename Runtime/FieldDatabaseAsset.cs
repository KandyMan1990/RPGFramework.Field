namespace RPGFramework.Field
{
    public class FieldDatabaseAsset
    {
        public string   AssetName          { get; }
        public string   AssetPath          { get; }
        public string[] LocalisationSheets { get; }

        public FieldDatabaseAsset(string assetName, string assetPath, string[] localisationSheets)
        {
            AssetName          = assetName;
            AssetPath          = assetPath;
            LocalisationSheets = localisationSheets;
        }
    }
}