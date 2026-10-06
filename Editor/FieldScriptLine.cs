using System.Collections.Generic;
using System.Text;

namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// Reads a line of script text into its parts, the script name and then each argument, and writes a value back as
    /// one.<br /><br />
    /// Parts are separated by spaces. A part in double quotes is read whole, spaces and all, so a name with a space in
    /// it can be an argument: <c>PLAY_MUSIC "Town Theme" AllStems</c>. Inside quotes, <c>\"</c> is a quote and
    /// <c>\\</c> a backslash. Every reader of script text splits it here, so none can disagree about where an argument
    /// ends.
    /// </summary>
    internal static class FieldScriptLine
    {
        private const char QUOTE  = '"';
        private const char ESCAPE = '\\';

        /// <summary>
        /// The line's parts, read as <see cref="TrySplit" /> reads them but ignoring what is wrong, for text that has
        /// already compiled. There is always at least one part, the script name, which is empty on a blank line.
        /// </summary>
        internal static string[] Split(string line)
        {
            TrySplit(line, out string[] parts, out _);

            return parts;
        }

        /// <returns>False, with what is wrong, when a quote is never closed or runs straight into more text.</returns>
        internal static bool TrySplit(string line, out string[] parts, out string problem)
        {
            List<string>  read = new List<string>();
            StringBuilder part = new StringBuilder();

            problem = null;

            int i = 0;

            while (i < line.Length)
            {
                if (char.IsWhiteSpace(line[i]))
                {
                    i++;
                    continue;
                }

                part.Clear();

                if (line[i] != QUOTE)
                {
                    while (i < line.Length && !char.IsWhiteSpace(line[i]))
                    {
                        part.Append(line[i++]);
                    }

                    read.Add(part.ToString());
                    continue;
                }

                bool closed = false;

                for (i++; i < line.Length && !closed; i++)
                {
                    char c = line[i];

                    if (c == ESCAPE && i + 1 < line.Length && (line[i + 1] == QUOTE || line[i + 1] == ESCAPE))
                    {
                        part.Append(line[++i]);
                    }
                    else if (c == QUOTE)
                    {
                        closed = true;
                    }
                    else
                    {
                        part.Append(c);
                    }
                }

                read.Add(part.ToString());

                if (!closed)
                {
                    problem ??= $"the quote before [{part}] is never closed";
                }
                else if (i < line.Length && !char.IsWhiteSpace(line[i]))
                {
                    problem ??= $"[{part}] is quoted, then runs straight into more text. Leave a space after its closing quote";
                }
            }

            if (read.Count == 0)
            {
                read.Add(string.Empty);
            }

            parts = read.ToArray();

            bool isWellFormed = problem == null;

            return isWellFormed;
        }

        /// <summary>
        /// A value as it is written as one part: in quotes if it is empty, holds a space or starts with a quote, and
        /// as it is otherwise.
        /// </summary>
        internal static string Quote(string value)
        {
            bool needsQuotes = value.Length == 0 || value[0] == QUOTE;

            for (int i = 0; !needsQuotes && i < value.Length; i++)
            {
                needsQuotes = char.IsWhiteSpace(value[i]);
            }

            if (!needsQuotes)
            {
                return value;
            }

            StringBuilder quoted = new StringBuilder(value.Length + 2);
            quoted.Append(QUOTE);

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == QUOTE || value[i] == ESCAPE)
                {
                    quoted.Append(ESCAPE);
                }

                quoted.Append(value[i]);
            }

            quoted.Append(QUOTE);

            string written = quoted.ToString();

            return written;
        }

        /// <summary>
        /// The parts from <paramref name="start" /> on, written back as script text, for a list that runs to the end of
        /// the line.
        /// </summary>
        internal static string Join(IReadOnlyList<string> parts, int start)
        {
            StringBuilder joined = new StringBuilder();

            for (int i = start; i < parts.Count; i++)
            {
                if (i > start)
                {
                    joined.Append(' ');
                }

                joined.Append(Quote(parts[i]));
            }

            string text = joined.ToString();

            return text;
        }
    }
}
