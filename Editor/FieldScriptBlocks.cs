using System.Collections.Generic;
using System.Text;

namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// One instruction in a script being edited: which opcode, and the text of each argument.<br /><br />
    /// Arguments are held as text rather than typed values because that is what the compiler consumes
    /// and what a script file contains. A variable argument is <c>$name</c>, a literal is the number or
    /// <c>true</c>/<c>false</c> itself — so a block round trips through the text form without needing a
    /// second representation that could disagree with it.
    /// </summary>
    public sealed class FieldScriptBlock
    {
        /// <summary>
        /// The opcode this block writes, or null when the line was not recognised. An unrecognised line
        /// is kept verbatim in <see cref="RawLine" /> so that opening a script in the editor and saving
        /// it cannot quietly delete something.
        /// </summary>
        public FieldOpCodeInfo OpCode { get; }

        public List<string> Arguments { get; }

        public string RawLine { get; }

        /// <summary>
        /// The instructions inside this block, for an opcode that opens one. Null for everything else,
        /// so "has a body" and "has an empty body" stay distinguishable.
        /// </summary>
        public List<FieldScriptBlock> Children { get; }

        public bool IsRecognised => OpCode != null;

        public bool OpensBlock => OpCode != null && OpCode.OpensBlock;

        public FieldScriptBlock(FieldOpCodeInfo opCode, List<string> arguments)
        {
            OpCode    = opCode;
            Arguments = arguments;
            RawLine   = null;
            Children  = opCode != null && opCode.OpensBlock ? new List<FieldScriptBlock>() : null;
        }

        public FieldScriptBlock(string rawLine)
        {
            OpCode    = null;
            Arguments = new List<string>();
            RawLine   = rawLine;
        }

        /// <summary>
        /// A block for an opcode with an empty slot per argument, ready to be filled in.
        /// </summary>
        public static FieldScriptBlock CreateEmpty(FieldOpCodeInfo opCode)
        {
            List<string> arguments = new List<string>(opCode.Arguments.Count);

            foreach (FieldArgumentInfo argument in opCode.Arguments)
            {
                arguments.Add(DefaultFor(argument.Type));
            }

            FieldScriptBlock block = new FieldScriptBlock(opCode, arguments);

            return block;
        }

        private static string DefaultFor(ArgumentType type)
        {
            switch (type)
            {
                case ArgumentType.Bool:
                    return "false";

                case ArgumentType.Float:
                    return "0";

                case ArgumentType.Variable8:
                case ArgumentType.Variable16:
                    // A destination has to be a variable, so it starts with the marker already there.
                    return "$";

                case ArgumentType.LocalisationKeyList:
                    return string.Empty;

                case ArgumentType.Comparison:
                    return ScriptComparison.Equal.ToScriptText();

                default:
                    return "0";
            }
        }

        public string ToLine()
        {
            if (!IsRecognised)
            {
                return RawLine;
            }

            StringBuilder sb = new StringBuilder(OpCode.ScriptName);

            foreach (string argument in Arguments)
            {
                if (string.IsNullOrWhiteSpace(argument))
                {
                    continue;
                }

                sb.Append(' ');
                sb.Append(argument.Trim());
            }

            string line = sb.ToString();

            return line;
        }
    }

    /// <summary>
    /// Converts between a script's text and the blocks the editor shows.<br /><br />
    /// The text is the source of truth — it is what is stored on the asset and what the compiler reads.
    /// The editor is a way of writing it without typing, not a separate format.
    /// </summary>
    public static class FieldScriptBlocks
    {
        /// <summary>
        /// Closes the body of a block-opening opcode. Not an opcode itself — it emits nothing, it only
        /// tells the compiler where a body stops so it can work out the jump distance.
        /// </summary>
        public const string END_BLOCK = "END_IF";

        private const string INDENT = "    ";

        public static List<FieldScriptBlock> Parse(string scriptText)
        {
            List<FieldScriptBlock> blocks = new List<FieldScriptBlock>();

            if (string.IsNullOrEmpty(scriptText))
            {
                return blocks;
            }

            // Where new blocks are being added. The root list until an IF opens, that IF's children
            // until it closes, and so on outwards.
            Stack<List<FieldScriptBlock>> openBodies = new Stack<List<FieldScriptBlock>>();
            openBodies.Push(blocks);

            foreach (string rawLine in scriptText.Split('\n'))
            {
                string line = rawLine.Trim();

                if (line.Length == 0)
                {
                    continue;
                }

                List<FieldScriptBlock> body = openBodies.Peek();

                if (line == END_BLOCK)
                {
                    // A stray END_IF would otherwise close the root list and lose everything after it.
                    if (openBodies.Count > 1)
                    {
                        openBodies.Pop();
                    }
                    else
                    {
                        body.Add(new FieldScriptBlock(line));
                    }

                    continue;
                }

                string[] parts = line.Split(' ');

                if (!FieldOpCodeCatalogue.TryGet(parts[0], out FieldOpCodeInfo opCode))
                {
                    body.Add(new FieldScriptBlock(line));
                    continue;
                }

                List<string> arguments = new List<string>(opCode.Arguments.Count);

                for (int i = 0; i < opCode.Arguments.Count; i++)
                {
                    bool isLastAndVariadic = i                        == opCode.Arguments.Count - 1 &&
                                             opCode.Arguments[i].Type == ArgumentType.LocalisationKeyList;

                    if (isLastAndVariadic)
                    {
                        // The list runs to the end of the line, so it is one field holding the rest.
                        arguments.Add(string.Join(" ", parts, i + 1, System.Math.Max(0, parts.Length - i - 1)));
                        break;
                    }

                    arguments.Add(i + 1 < parts.Length ? parts[i + 1] : string.Empty);
                }

                FieldScriptBlock block = new FieldScriptBlock(opCode, arguments);

                body.Add(block);

                if (block.OpensBlock)
                {
                    openBodies.Push(block.Children);
                }
            }

            return blocks;
        }

        public static string ToScriptText(IReadOnlyList<FieldScriptBlock> blocks)
        {
            StringBuilder sb = new StringBuilder();

            Write(blocks, sb, 0);

            string scriptText = sb.ToString().TrimEnd();

            return scriptText;
        }

        private static void Write(IReadOnlyList<FieldScriptBlock> blocks, StringBuilder sb, int depth)
        {
            string indent = string.Concat(System.Linq.Enumerable.Repeat(INDENT, depth));

            foreach (FieldScriptBlock block in blocks)
            {
                sb.Append(indent);
                sb.AppendLine(block.ToLine());

                if (!block.OpensBlock)
                {
                    continue;
                }

                Write(block.Children, sb, depth + 1);

                sb.Append(indent);
                sb.AppendLine(END_BLOCK);
            }
        }
    }
}