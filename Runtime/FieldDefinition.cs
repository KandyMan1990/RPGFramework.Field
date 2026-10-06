namespace RPGFramework.Field
{
    public class FieldDefinition
    {
        internal string   AssetName          { get; }
        internal string   AssetPath          { get; }
        internal string[] LocalisationSheets { get; }
        internal ulong    LocationName       { get; }

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