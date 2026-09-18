using System.Collections.Generic;
using System.Globalization;

namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// Which dialogue channels a field's scripts give a rectangle, and which they show dialogue on.<br /><br />
    /// A channel needs its rectangle set by <c>CREATE_DIALOGUE_WINDOW</c> before anything is shown on it. Whether
    /// that happens first depends on the order scripts run in, which export cannot know — but a channel that no
    /// script in the field ever sets can never work, and that much is certain from the text.
    /// </summary>
    internal sealed class DialogueChannelUse
    {
        private readonly HashSet<int>            m_Set   = new HashSet<int>();
        private readonly Dictionary<int, string> m_Shown = new Dictionary<int, string>();

        internal void Read(string[] lines, string scriptDescription)
        {
            foreach (string line in lines)
            {
                string[] parts = line.Trim().Split(' ');

                if (!FieldOpCodeCatalogue.TryGet(parts[0], out FieldOpCodeInfo opCode))
                {
                    continue;
                }

                for (int i = 0; i < opCode.Arguments.Count; i++)
                {
                    if (opCode.Arguments[i].Type != ArgumentType.DialogueChannel || !TryReadChannel(parts, i + 1, out int channel))
                    {
                        continue;
                    }

                    if (opCode.OpCode == FieldScriptOpCode.CreateDialogueWindow)
                    {
                        m_Set.Add(channel);
                    }
                    else if (opCode.OpCode == FieldScriptOpCode.ShowDialogueWindow       ||
                             opCode.OpCode == FieldScriptOpCode.ShowDialogueWindowNoWait ||
                             opCode.OpCode == FieldScriptOpCode.AskPlayerToMakeAChoice)
                    {
                        m_Shown.TryAdd(channel, scriptDescription);
                    }
                }
            }
        }

        internal void Validate(string fieldName, List<string> problems)
        {
            foreach (KeyValuePair<int, string> shown in m_Shown)
            {
                if (!m_Set.Contains(shown.Key))
                {
                    problems.Add($"{shown.Value} shows dialogue on channel [{shown.Key}], but nothing in {fieldName} sets that channel's rectangle with CREATE_DIALOGUE_WINDOW, so it could never be shown");
                }
            }
        }

        private static bool TryReadChannel(string[] parts, int index, out int channel)
        {
            channel = 0;

            bool read = index < parts.Length && int.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out channel);

            return read;
        }
    }
}