namespace RPGFramework.Field
{
    public class FieldDatabaseAsset
    {
        public string   AssetName          { get; }
        public string   AssetPath          { get; }
        public string[] LocalisationSheets { get; }
        public ulong    LocationName       { get; }

        public FieldDatabaseAsset(string   assetName,
                                  string   assetPath,
                                  string[] localisationSheets,
                                  ulong    locationName)
        {
            AssetName          = assetName;
            AssetPath          = assetPath;
            LocalisationSheets = localisationSheets;
            LocationName       = locationName;
        }
    }
}