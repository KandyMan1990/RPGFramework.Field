namespace RPGFramework.Field
{
    public sealed class FieldDefinition
    {
        internal string   AssetName          { get; }
        internal string   AssetPath          { get; }
        internal string[] LocalisationSheets { get; }
        internal ulong    LocationName       { get; }

        /// <summary>The transition that covers the screen as the player leaves for another field; 0 for the default.</summary>
        internal ulong ExitTransition { get; }

        /// <summary>The transition that reveals this field as the player arrives from another; 0 for the default.</summary>
        internal ulong EntryTransition { get; }

        public FieldDefinition(string   assetName,
                               string   assetPath,
                               string[] localisationSheets,
                               ulong    locationName,
                               ulong    exitTransition,
                               ulong    entryTransition)
        {
            AssetName          = assetName;
            AssetPath          = assetPath;
            LocalisationSheets = localisationSheets;
            LocationName       = locationName;
            ExitTransition     = exitTransition;
            EntryTransition    = entryTransition;
        }
    }
}