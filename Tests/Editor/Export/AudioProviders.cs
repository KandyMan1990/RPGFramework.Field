using System.Collections.Generic;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Export
{
    /// <summary>
    /// A project whose providers list the tracks Town (Calm, Tense) and Battle (Fight) and the sound Step, and export's
    /// check of a script's lines against them.
    /// </summary>
    internal static class AudioProviders
    {
        internal static List<string> Problems(params string[] lines)
        {
            AudioNames audioNames = new AudioNames(new List<string> { "Town", "Battle" },
                                                   new List<string> { "Step" },
                                                   new Dictionary<string, List<string>>
                                                   {
                                                       { "Town", new List<string> { "Calm", "Tense" } },
                                                       { "Battle", new List<string> { "Fight" } }
                                                   });

            List<string> problems = new List<string>();

            FieldDatabase.ValidateAudioNames(audioNames, "script", lines, problems);

            return problems;
        }
    }
}
