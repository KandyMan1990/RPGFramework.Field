using System;
using System.Collections.Generic;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Catalogue
{
    /// <summary>
    /// What the block editor's pickers would offer in a small project: the tracks Town (Calm, Tense), Night (Calm) and
    /// Battle (Fight), the sounds Step and Door, and the fields Village (spawn points 1 and 2) and Cave (5).
    /// </summary>
    internal sealed class Choices : IArgumentChoiceProvider
    {
        private static readonly string[] m_Tracks = { "Town", "Night", "Battle" };
        private static readonly string[] m_Sounds = { "Step", "Door" };
        private static readonly string[] m_Fields = { "Village", "Cave" };

        IReadOnlyList<string> IArgumentChoiceProvider.ChoicesFor(FieldScriptBlock block, int argumentIndex)
        {
            switch (block.OpCode.Arguments[argumentIndex].Type)
            {
                case ArgumentType.MusicName:
                case ArgumentType.MusicNameHint:
                    return m_Tracks;

                case ArgumentType.MusicStateName:
                    return StatesOf(Named(block, ArgumentType.MusicName, ArgumentType.MusicNameHint));

                case ArgumentType.SoundName:
                    return m_Sounds;

                case ArgumentType.FieldName:
                    return m_Fields;

                case ArgumentType.SpawnId:
                    return SpawnPointsOf(Named(block, ArgumentType.FieldName, ArgumentType.FieldName));

                default:
                    return Array.Empty<string>();
            }
        }

        private static string[] StatesOf(string track)
        {
            switch (track)
            {
                case "Town":   return new[] { "Calm", "Tense" };
                case "Night":  return new[] { "Calm" };
                case "Battle": return new[] { "Fight" };
                default:       return new[] { "Calm", "Tense", "Fight" };
            }
        }

        private static string[] SpawnPointsOf(string field)
        {
            switch (field)
            {
                case "Village": return new[] { "1", "2" };
                case "Cave":    return new[] { "5" };
                default:        return Array.Empty<string>();
            }
        }

        private static string Named(FieldScriptBlock block, ArgumentType type, ArgumentType otherType)
        {
            for (int i = 0; i < block.OpCode.Arguments.Count && i < block.Arguments.Count; i++)
            {
                ArgumentType argumentType = block.OpCode.Arguments[i].Type;

                if (argumentType == type || argumentType == otherType)
                {
                    return block.Arguments[i];
                }
            }

            return string.Empty;
        }
    }
}
