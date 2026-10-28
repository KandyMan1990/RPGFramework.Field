using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RPGFramework.Core.Dialogue;
using RPGFramework.Core.Memory;

namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// One instruction in a script being edited: which opcode, and the text of each argument.<br /><br />
    /// Arguments are held as text rather than typed values because that is what the compiler consumes
    /// and what a script file contains. A variable argument is <c>$name</c>, a literal is the number or
    /// <c>true</c>/<c>false</c> itself — so a block round trips through the text form without needing a
    /// second representation that could disagree with it. A name is held without the quotes a space in it
    /// needs, which <see cref="ToLine" /> adds; a list that runs to the end of the line is held as script
    /// text, quotes and all.
    /// </summary>
    internal sealed class FieldScriptBlock
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

        /// <summary>
        /// The name, for a <c>LABEL</c>: a place a jump can go to. Null for everything else. Not an opcode — it emits
        /// nothing — but a block of its own so it can be placed, moved and named like one.
        /// </summary>
        public string LabelName { get; private set; }

        public bool IsLabel => LabelName != null;

        /// <summary>The instructions that run when the comparison does not hold. Null when there is no ELSE.</summary>
        public List<FieldScriptBlock> ElseChildren { get; private set; }

        public bool HasElse => ElseChildren != null;

        public bool IsRecognised => OpCode != null;

        public bool OpensBlock => OpCode != null && OpCode.OpensBlock;

        public FieldScriptBlock(FieldOpCodeInfo opCode, List<string> arguments)
        {
            OpCode    = opCode;
            Arguments = arguments;
            RawLine   = null;
            Children  = opCode != null && opCode.OpensBlock ? new List<FieldScriptBlock>() : null;
        }

        public void AddElse()
        {
            ElseChildren ??= new List<FieldScriptBlock>();
        }

        public void RemoveElse()
        {
            ElseChildren = null;
        }

        public FieldScriptBlock(string rawLine)
        {
            OpCode    = null;
            Arguments = new List<string>();
            RawLine   = rawLine;
        }

        public static FieldScriptBlock CreateLabel(string name)
        {
            FieldScriptBlock label = new FieldScriptBlock(null, new List<string>()) { LabelName = name };

            return label;
        }

        public void RenameLabel(string name)
        {
            LabelName = name;
        }

        /// <summary>
        /// A block for an opcode with an empty slot per argument, ready to be filled in.
        /// </summary>
        public static FieldScriptBlock CreateEmpty(FieldOpCodeInfo opCode)
        {
            List<string> arguments = new List<string>(opCode.Arguments.Count);

            for (int i = 0; i < opCode.Arguments.Count; i++)
            {
                FieldArgumentInfo argument = opCode.Arguments[i];

                arguments.Add(DefaultFor(argument));
            }

            FieldScriptBlock block = new FieldScriptBlock(opCode, arguments);

            return block;
        }

        /// <summary>Writes an argument, padding a line written with fewer.</summary>
        public void SetArgument(int index, string value)
        {
            while (Arguments.Count <= index)
            {
                Arguments.Add(string.Empty);
            }

            Arguments[index] = value;
        }

        /// <summary>
        /// Gives a new block's names the first choice each picker offers, in order, so a stem state is its track's and
        /// a spawn point its field's. A picker with nothing to offer leaves its name empty.
        /// </summary>
        public void PickFirstChoices(IArgumentChoiceProvider choices)
        {
            for (int i = 0; i < OpCode.Arguments.Count; i++)
            {
                if (!IsPicked(OpCode.Arguments[i].Type))
                {
                    continue;
                }

                IReadOnlyList<string> offered = choices.ChoicesFor(this, i);

                if (offered.Count > 0)
                {
                    SetArgument(i, offered[0]);
                }
            }
        }

        /// <summary>
        /// After a track, a field or a question's answers change, moves a stem state, spawn point or answer the new ones
        /// do not offer to its first choice, and keeps one they do.
        /// </summary>
        public void KeepChoicesOffered(IArgumentChoiceProvider choices)
        {
            for (int i = 0; i < OpCode.Arguments.Count; i++)
            {
                ArgumentType type = OpCode.Arguments[i].Type;
                bool         isAnswer = type == ArgumentType.Answer || type == ArgumentType.CancelAnswer;

                if (type != ArgumentType.MusicStateName && type != ArgumentType.SpawnId && !isAnswer)
                {
                    continue;
                }

                IReadOnlyList<string> offered = isAnswer ? AnswerChoices(type) : choices.ChoicesFor(this, i);
                string                current = i < Arguments.Count ? Arguments[i] : string.Empty;

                if (offered.Count > 0 && !Offers(offered, current))
                {
                    SetArgument(i, offered[0]);
                }
            }
        }

        /// <summary>The question's answers, by key, from the list that ends its line.</summary>
        public string[] Answers()
        {
            for (int i = 0; i < OpCode.Arguments.Count; i++)
            {
                if (OpCode.Arguments[i].Type != ArgumentType.LocalisationKeyList)
                {
                    continue;
                }

                return i < Arguments.Count && !string.IsNullOrWhiteSpace(Arguments[i]) ? FieldScriptLine.Split(Arguments[i]) : Array.Empty<string>();
            }

            return Array.Empty<string>();
        }

        /// <summary>
        /// What a starting or cancel answer can be, as written: each answer's number, and for a cancel answer
        /// <c>Last</c> and <c>None</c> before them.
        /// </summary>
        public string[] AnswerChoices(ArgumentType type)
        {
            int      count  = Answers().Length;
            int      extra  = type == ArgumentType.CancelAnswer ? 2 : 0;
            string[] values = new string[count + extra];

            if (extra > 0)
            {
                values[0] = FieldScriptCompiler.CANCEL_LAST;
                values[1] = FieldScriptCompiler.CANCEL_NONE;
            }

            for (int i = 0; i < count; i++)
            {
                values[extra + i] = i.ToString(CultureInfo.InvariantCulture);
            }

            return values;
        }

        private static bool IsPicked(ArgumentType type)
        {
            switch (type)
            {
                case ArgumentType.FieldName:
                case ArgumentType.MusicName:
                case ArgumentType.SoundName:
                case ArgumentType.MusicNameHint:
                case ArgumentType.MusicStateName:
                case ArgumentType.AnimationName:
                case ArgumentType.LocalisationKey:
                case ArgumentType.SpawnId:
                    return true;

                default:
                    return false;
            }
        }

        private static bool Offers(IReadOnlyList<string> offered, string value)
        {
            for (int i = 0; i < offered.Count; i++)
            {
                if (offered[i] == value)
                {
                    return true;
                }
            }

            return false;
        }

        internal static string DefaultFor(FieldArgumentInfo argument)
        {
            if (argument.Default != null)
            {
                return argument.Default;
            }

            switch (argument.Type)
            {
                case ArgumentType.Bool:
                case ArgumentType.Value when argument.Width == VariableWidth.Bool:
                    return "false";

                case ArgumentType.Float:
                    return "0";

                case ArgumentType.Variable:
                    // A destination has to be a variable, so it starts with the marker already there.
                    return "$";

                // A name starts with nothing chosen, never a number, which would read as a choice.
                case ArgumentType.FieldName:
                case ArgumentType.MusicName:
                case ArgumentType.SoundName:
                case ArgumentType.MusicNameHint:
                case ArgumentType.MusicStateName:
                case ArgumentType.AnimationName:
                case ArgumentType.LocalisationKey:
                case ArgumentType.LocalisationKeyList:
                case ArgumentType.Label:
                case ArgumentType.TransitionName:
                    return string.Empty;

                case ArgumentType.Comparison:
                    return ScriptComparison.Equal.ToScriptText();

                case ArgumentType.Answer:
                    return "0";

                case ArgumentType.CancelAnswer:
                    return FieldScriptCompiler.CANCEL_LAST;

                case ArgumentType.DialogueWindowStyle:
                    return nameof(DialogueWindowStyle.Spoken);

                default:
                    return "0";
            }
        }

        public string ToLine()
        {
            if (IsLabel)
            {
                string labelLine = $"{FieldScriptBlocks.LABEL_BLOCK} {LabelName}".TrimEnd();

                return labelLine;
            }

            if (!IsRecognised)
            {
                return RawLine;
            }

            StringBuilder sb = new StringBuilder(OpCode.ScriptName);

            // Empty arguments at the end are left off, as a line written before they existed; one before an argument
            // that is written is written as "", so those after it keep their places.
            int last = Arguments.Count - 1;

            while (last >= 0 && string.IsNullOrWhiteSpace(Arguments[last]))
            {
                last--;
            }

            for (int i = 0; i <= last; i++)
            {
                string argument = Arguments[i];

                bool isScriptText = i < OpCode.Arguments.Count && OpCode.Arguments[i].Type == ArgumentType.LocalisationKeyList;

                sb.Append(' ');
                sb.Append(isScriptText ? argument.Trim() : FieldScriptLine.Quote(argument.Trim()));
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
    internal static class FieldScriptBlocks
    {
        /// <summary>
        /// Closes the body of a block-opening opcode. Not an opcode itself — it emits nothing, it only
        /// tells the compiler where a body stops so it can work out the jump distance.
        /// </summary>
        public const string END_BLOCK = "END_IF";

        public const string ELSE_BLOCK = "ELSE";

        /// <summary>
        /// Marks a place a jump can go to. Not an opcode either: it emits nothing, and the compiler turns a jump's
        /// label into the byte it needs.
        /// </summary>
        public const string LABEL_BLOCK = "LABEL";

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

            Stack<FieldScriptBlock> openBlocks = new Stack<FieldScriptBlock>();

            string[] rawLines = scriptText.Split('\n');

            for (int j = 0; j < rawLines.Length; j++)
            {
                string rawLine = rawLines[j];

                string line = rawLine.Trim();

                if (line.Length == 0)
                {
                    continue;
                }

                List<FieldScriptBlock> body = openBodies.Peek();

                if (line == END_BLOCK)
                {
                    // A stray END_IF would otherwise close the root list and lose everything after it.
                    if (openBlocks.Count > 0)
                    {
                        openBodies.Pop();
                        openBlocks.Pop();
                    }
                    else
                    {
                        body.Add(new FieldScriptBlock(line));
                    }

                    continue;
                }

                if (line == ELSE_BLOCK)
                {
                    if (openBlocks.Count > 0 && !openBlocks.Peek().HasElse)
                    {
                        FieldScriptBlock owner = openBlocks.Peek();
                        owner.AddElse();

                        openBodies.Pop();
                        openBodies.Push(owner.ElseChildren);
                    }
                    else
                    {
                        body.Add(new FieldScriptBlock(line));
                    }

                    continue;
                }

                // A line that does not split cleanly is kept as written, for the compiler to say what is wrong.
                if (!FieldScriptLine.TrySplit(line, out string[] parts, out _))
                {
                    body.Add(new FieldScriptBlock(line));
                    continue;
                }

                // Anything but exactly one name is kept as written rather than guessed at.
                if (parts[0] == LABEL_BLOCK && parts.Length == 2)
                {
                    body.Add(FieldScriptBlock.CreateLabel(parts[1]));
                    continue;
                }

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
                        arguments.Add(FieldScriptLine.Join(parts, i + 1));
                        break;
                    }

                    arguments.Add(i + 1 < parts.Length ? parts[i + 1] : string.Empty);
                }

                FieldScriptBlock block = new FieldScriptBlock(opCode, arguments);

                body.Add(block);

                if (block.OpensBlock)
                {
                    openBodies.Push(block.Children);
                    openBlocks.Push(block);
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

            for (int i = 0; i < blocks.Count; i++)
            {
                FieldScriptBlock block = blocks[i];

                sb.Append(indent);
                sb.AppendLine(block.ToLine());

                if (!block.OpensBlock)
                {
                    continue;
                }

                Write(block.Children, sb, depth + 1);

                if (block.HasElse)
                {
                    sb.Append(indent);
                    sb.AppendLine(ELSE_BLOCK);

                    Write(block.ElseChildren, sb, depth + 1);
                }

                sb.Append(indent);
                sb.AppendLine(END_BLOCK);
            }
        }
    }
}