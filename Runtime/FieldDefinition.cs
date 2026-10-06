namespace RPGFramework.Field
{
    public class FieldDefinition
    {
        public string   AssetName          { get; }
        public string   AssetPath          { get; }
        public string[] LocalisationSheets { get; }
        public ulong    LocationName       { get; }

        public FieldDefinition(string   assetName,
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