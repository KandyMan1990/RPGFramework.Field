using System.Runtime.CompilerServices;

// The editor builds, validates and migrates the entity records, which the game never needs to see.
[assembly: InternalsVisibleTo("RPGFramework.Field.Editor")]