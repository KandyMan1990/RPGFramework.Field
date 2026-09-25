using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using RPGFramework.Core.Dialogue;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Hashing;

namespace RPGFramework.Field.Editor
{
    internal static class FieldScriptCompiler
    {
        /// <summary>
        /// Prefix marking a token as a variable name to resolve against the <see cref="VariableMapAsset" />
        /// rather than a literal. <c>ADD_BYTE $money 100</c> adds the literal 100 to the variable named money;
        /// <c>ADD_BYTE $money $payslip</c> adds one variable to another.
        /// </summary>
        private const char VARIABLE_PREFIX = '$';

        private const byte ARGUMENT_IMMEDIATE = 0;

        /// <summary>
        /// Mirrors <c>FieldEntityRuntime.PRIORITY_COUNT</c>, which is internal to the runtime assembly.
        /// </summary>
        private const int SCRIPT_PRIORITY_COUNT = 8;

        public static byte[] Compile(string source)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);

            VariableMapAsset variableMap = null;

            // Every jump has to land on the first byte of an instruction. Nothing checked that, so a
            // hand-computed offset that was out by a byte would put the instruction pointer in the
            // middle of an argument, and the VM would execute that argument as an opcode. The offsets of
            // every instruction are collected as they are emitted, and every jump is resolved against
            // them once the whole script is known.
            List<int>      instructionStarts = new List<int>();
            List<JumpSite> jumpSites         = new List<JumpSite>();

            // Open IF blocks, innermost last. Each remembers where its jump distance was left blank so
            // END_IF can fill it in, once the size of the body is known.
            Stack<OpenBlock> openBlocks = new Stack<OpenBlock>();

            string[] lines = source.Split('\n');

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                string[] parts = line.Split(' ');

                int instructionStart = (int)ms.Position;
                instructionStarts.Add(instructionStart);

                RecordJumpSite(parts, lineIndex, instructionStart, jumpSites);

                switch (parts[0])
                {
                    case "ELSE":
                        OpenElse(bw, ms, lineIndex + 1, openBlocks);
                        break;

                    case "END_IF":
                        CloseComparison(bw, ms, lineIndex + 1, openBlocks);
                        break;

                    case "ASK_PLAYER_TO_MAKE_A_CHOICE":
                        if (parts.Length < 5)
                        {
                            throw new Exception($"line {lineIndex + 1}: ASK_PLAYER_TO_MAKE_A_CHOICE needs a destination, a channel, a question key and at least one answer key");
                        }

                        VariableDefinition choice = ResolveVariable(parts[1], parts[0], ref variableMap, VariableWidth.Byte);

                        bw.Write((ushort)FieldScriptOpCode.AskPlayerToMakeAChoice);
                        bw.Write((byte)choice.Bank);
                        bw.Write((ushort)choice.Offset);
                        bw.Write(ParseIndex(parts, 2, ArgumentTypes.DIALOGUE_CHANNEL_COUNT, "dialogue channel", lineIndex + 1));
                        bw.Write(HashDialogueKey(parts, 3, lineIndex + 1));

                        byte count = (byte)(parts.Length - 4);
                        bw.Write(count);

                        for (int i = 4; i < parts.Length; i++)
                        {
                            bw.Write(HashDialogueKey(parts, i, lineIndex + 1));
                        }

                        break;

                    default:
                        if (!FieldOpCodeCatalogue.TryGet(parts[0], out FieldOpCodeInfo opCode))
                        {
                            throw new Exception($"Unknown opcode '{parts[0]}'");
                        }

                        WriteOpCode(bw, ms, opCode, parts, lineIndex + 1, ref variableMap, openBlocks);
                        break;
                }
            }

            bw.Flush();

            if (openBlocks.Count > 0)
            {
                OpenBlock unclosed = openBlocks.Peek();

                throw new Exception($"{nameof(FieldScriptCompiler)}::{nameof(Compile)} {openBlocks.Count} unclosed IF block(s); the one opened on line {unclosed.LineNumber} has no END_IF");
            }

            ValidateJumpTargets(jumpSites, instructionStarts, (int)ms.Length);

            return ms.ToArray();
        }

        /// <summary>
        /// Where a jump was written, and what it will resolve to. Recorded while emitting because the
        /// destination may not have been emitted yet.
        /// </summary>
        private readonly struct JumpSite
        {
            internal readonly string ScriptName;
            internal readonly int    LineNumber;
            internal readonly int    InstructionStart;
            internal readonly int    Argument;
            internal readonly bool   IsAbsolute;

            internal JumpSite(string scriptName, int lineNumber, int instructionStart, int argument, bool isAbsolute)
            {
                ScriptName       = scriptName;
                LineNumber       = lineNumber;
                InstructionStart = instructionStart;
                Argument         = argument;
                IsAbsolute       = isAbsolute;
            }

            /// <summary>
            /// The byte the instruction pointer will hold after this jump runs.<br /><br />
            /// <c>GOTO_DIRECTLY</c> assigns the argument outright. <c>GOTO_JUMP</c> adds it to the pointer,
            /// which by then has already advanced past this instruction — two bytes of opcode and four of
            /// argument.
            /// </summary>
            internal int ResolveTarget()
            {
                if (IsAbsolute)
                {
                    return Argument;
                }

                const int jumpInstructionSize = sizeof(ushort) + sizeof(int);

                int target = InstructionStart + jumpInstructionSize + Argument;

                return target;
            }
        }

        /// <summary>
        /// Resolve a field by the name an author wrote and return that name's hash.<br /><br />
        /// The hash is what goes in the bytecode. The lookup is only to fail here, at compile time, on a
        /// name no field answers to
        /// </summary>
        private static ulong HashFieldName(string fieldName, int lineNumber)
        {
            string[] assetGuids = UnityEditor.AssetDatabase.FindAssets("t:" + nameof(FieldDesignerData));

            foreach (string assetGuid in assetGuids)
            {
                string            assetPath         = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid);
                FieldDesignerData fieldDesignerData = UnityEditor.AssetDatabase.LoadAssetAtPath<FieldDesignerData>(assetPath);

                foreach (FieldDatabaseAssetAuthoring field in fieldDesignerData.FieldDatabase.Fields)
                {
                    if (field.Prefab.name == fieldName)
                    {
                        ulong hash = Fnv1a64.Hash(fieldName);

                        return hash;
                    }
                }
            }

            throw new KeyNotFoundException($"line {lineNumber}: no field is named [{fieldName}]. JUMP_TO_MAP names a field, and the name has to match a field in the field database");
        }

        /// <summary>
        /// Hash a localisation key argument, having first checked there is one.<br /><br />
        /// A key is hashed at compile time and the runtime only ever sees the hash, so a missing or
        /// blank key produced a hash of nothing.<br /><br />
        /// <b>What this does not yet check is that the key actually resolves</b> in the localisation
        /// sheets the field declares. That needs the set of keys in a sheet, which nothing exposes at
        /// editor time — the sheets are fetched during binary generation and only the generated C#
        /// constants survive. It is the natural companion to the field editor's planned "choose a
        /// dialogue key from a dropdown of this field's sheets", which needs exactly the same
        /// enumeration and would make an unresolvable key impossible to author in the first place.
        /// </summary>
        private static ulong HashDialogueKey(string[] parts, int index, int lineNumber)
        {
            if (index >= parts.Length)
            {
                throw new Exception($"line {lineNumber}: {parts[0]} is missing its localisation key");
            }

            string key = parts[index];

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new Exception($"line {lineNumber}: {parts[0]} has a blank localisation key");
            }

            ulong hash = Fnv1a64.Hash(key);

            return hash;
        }

        private static void WriteOpCode(BinaryWriter bw, MemoryStream ms, FieldOpCodeInfo opCode, string[] parts, int lineNumber, ref VariableMapAsset variableMap, Stack<OpenBlock> openBlocks)
        {
            switch (opCode.Layout)
            {
                case ArgumentLayout.Sequential:
                    WriteSequential(bw, ms, opCode, parts, lineNumber, ref variableMap);
                    return;

                case ArgumentLayout.BankBinary:
                    WriteBinaryArguments(bw, opCode, parts, lineNumber, ref variableMap);
                    return;

                case ArgumentLayout.BankUnary:
                    WriteDestinationOnly(bw, opCode, parts, ref variableMap);
                    return;

                case ArgumentLayout.BankCompare:
                    WriteComparison(bw, ms, opCode, parts, lineNumber, ref variableMap, openBlocks);
                    return;

                default:
                    WriteSeed(bw, opCode, parts, lineNumber, ref variableMap);
                    return;
            }
        }

        /// <summary>
        /// Encode an opcode from its argument attributes. See <see cref="ArgumentLayout.Sequential" /> for the layout.
        /// </summary>
        private static void WriteSequential(BinaryWriter bw, MemoryStream ms, FieldOpCodeInfo opCode, string[] parts, int lineNumber, ref VariableMapAsset variableMap)
        {
            // A surplus argument would otherwise be dropped without a word, and the ones before it read as meaning
            // something else — as an old REQUEST_SCRIPT's slot would read as its event.
            if (parts.Length - 1 > opCode.Arguments.Count)
            {
                throw new Exception($"line {lineNumber}: {parts[0]} takes {opCode.Arguments.Count} argument(s), but was given {parts.Length - 1}");
            }

            bw.Write((ushort)opCode.OpCode);

            int sourcesPosition = -1;

            for (int i = 0; i < opCode.Arguments.Count; i++)
            {
                FieldArgumentInfo argument = opCode.Arguments[i];
                int               part     = i + 1;

                if (ArgumentTypes.TakesSource(argument.Type))
                {
                    WriteSourcedArgument(bw, ms, argument.Type, parts[part], parts[0], lineNumber, ref sourcesPosition, ref variableMap);
                }
                else
                {
                    WriteUnsourcedArgument(bw, argument.Type, parts, part, lineNumber);
                }
            }
        }

        private static void WriteSourcedArgument(BinaryWriter bw, MemoryStream ms, ArgumentType type, string token, string scriptName, int lineNumber, ref int sourcesPosition, ref VariableMapAsset variableMap)
        {
            byte   source  = ARGUMENT_IMMEDIATE;
            ushort address = 0;

            if (IsVariableToken(token))
            {
                ArgumentTypes.TryGetVariableWidth(type, default, out VariableWidth width);

                VariableDefinition variable = ResolveVariable(token, scriptName, ref variableMap, width);

                source  = ToArgumentSource(variable.Bank);
                address = (ushort)variable.Offset;
            }

            if (sourcesPosition < 0)
            {
                sourcesPosition = (int)ms.Position;
                bw.Write((byte)(source << 4));
            }
            else
            {
                bw.Flush();

                long resume = ms.Position;

                ms.Position = sourcesPosition;
                int firstSource = ms.ReadByte();

                ms.Position = sourcesPosition;
                bw.Write((byte)(firstSource | source));
                bw.Flush();

                ms.Position     = resume;
                sourcesPosition = -1;
            }

            if (source != ARGUMENT_IMMEDIATE)
            {
                bw.Write(address);
                return;
            }

            switch (type)
            {
                case ArgumentType.Bool:
                    bw.Write(bool.Parse(token));
                    break;

                case ArgumentType.Byte:
                case ArgumentType.EntityId:
                    bw.Write(byte.Parse(token, CultureInfo.InvariantCulture));
                    break;

                case ArgumentType.Priority:
                    byte priority = byte.Parse(token, CultureInfo.InvariantCulture);

                    if (priority >= SCRIPT_PRIORITY_COUNT)
                    {
                        throw new Exception($"line {lineNumber}: {scriptName} priority [{priority}] is outside 0..{SCRIPT_PRIORITY_COUNT - 1}");
                    }

                    bw.Write(priority);
                    break;

                case ArgumentType.UShort:
                case ArgumentType.EventId:
                    bw.Write(ushort.Parse(token, CultureInfo.InvariantCulture));
                    break;

                case ArgumentType.Int:
                case ArgumentType.SpawnId:
                    bw.Write(int.Parse(token, CultureInfo.InvariantCulture));
                    break;

                case ArgumentType.Float:
                    bw.Write(float.Parse(token, CultureInfo.InvariantCulture));
                    break;
            }
        }

        private static void WriteUnsourcedArgument(BinaryWriter bw, ArgumentType type, string[] parts, int part, int lineNumber)
        {
            switch (type)
            {
                case ArgumentType.JumpDistance:
                case ArgumentType.JumpTarget:
                    bw.Write(int.Parse(parts[part], CultureInfo.InvariantCulture));
                    break;

                case ArgumentType.FieldName:
                    bw.Write(HashFieldName(parts[part], lineNumber));
                    break;

                case ArgumentType.MusicName:
                case ArgumentType.SoundName:
                case ArgumentType.MusicStateName:
                case ArgumentType.AnimationName:
                    bw.Write(Fnv1a64.Hash(parts[part]));
                    break;

                // Named so the editor can offer the right states; the opcode applies to whatever is playing.
                case ArgumentType.MusicNameHint:
                    break;

                case ArgumentType.LocalisationKey:
                    bw.Write(HashDialogueKey(parts, part, lineNumber));
                    break;

                case ArgumentType.DialogueChannel:
                    bw.Write(ParseIndex(parts, part, ArgumentTypes.DIALOGUE_CHANNEL_COUNT, "dialogue channel", lineNumber));
                    break;

                case ArgumentType.MessageVariableSlot:
                    bw.Write(ParseIndex(parts, part, DialogueMarkup.MESSAGE_VARIABLE_COUNT, "message variable", lineNumber));
                    break;

                case ArgumentType.DialogueWindowStyle:
                    bw.Write((byte)ParseWindowStyle(parts, part, lineNumber));
                    break;

                default:
                    throw new Exception($"line {lineNumber}: {parts[0]} has a {type} argument, which a sequential opcode cannot encode");
            }
        }

        private static byte ParseIndex(string[] parts, int part, int count, string what, int lineNumber)
        {
            if (part >= parts.Length)
            {
                throw new Exception($"line {lineNumber}: {parts[0]} is missing its {what}");
            }

            if (!byte.TryParse(parts[part], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte index) || index >= count)
            {
                throw new Exception($"line {lineNumber}: {parts[0]} needs a {what} from 0 to {count - 1}, got '{parts[part]}'");
            }

            return index;
        }

        /// <summary>
        /// A style is written by name, as the block editor writes it.
        /// </summary>
        private static DialogueWindowStyle ParseWindowStyle(string[] parts, int part, int lineNumber)
        {
            if (part >= parts.Length)
            {
                throw new Exception($"line {lineNumber}: {parts[0]} is missing its window style");
            }

            if (!Enum.TryParse(parts[part], false, out DialogueWindowStyle style) || !Enum.IsDefined(typeof(DialogueWindowStyle), style) || char.IsDigit(parts[part][0]))
            {
                throw new Exception($"line {lineNumber}: {parts[0]} needs a window style — {string.Join(", ", Enum.GetNames(typeof(DialogueWindowStyle)))} — got '{parts[part]}'");
            }

            return style;
        }

        /// <summary>
        /// An IF whose body is still being written. The jump distance cannot be known until the body
        /// ends, so a placeholder int is written and its position kept until END_IF.
        /// </summary>
        private readonly struct OpenBlock
        {
            internal readonly int LineNumber;
            internal readonly int JumpByteposition;
            internal readonly int BodyStart;

            // So a second ELSE for the same IF is refused.
            internal readonly bool IsElse;

            internal OpenBlock(int lineNumber, int jumpBytePosition, int bodyStart, bool isElse = false)
            {
                LineNumber       = lineNumber;
                JumpByteposition = jumpBytePosition;
                BodyStart        = bodyStart;
                IsElse           = isElse;
            }
        }

        /// <summary>
        /// Emit a comparison and open a block. The instructions that follow are its body, and the jump
        /// distance written here is the number of bytes to skip when the comparison does not hold — so
        /// it is filled in by <see cref="CloseComparison" /> rather than written by an author.
        /// </summary>
        private static void WriteComparison(BinaryWriter bw, MemoryStream ms, FieldOpCodeInfo opCode, string[] parts, int lineNumber, ref VariableMapAsset variableMap, Stack<OpenBlock> openBlocks)
        {
            if (parts.Length < 4)
            {
                throw new Exception($"line {lineNumber}: {parts[0]} needs a value, a comparison and a second value, for example '{parts[0]} $flag == 1'");
            }

            if (!ScriptComparisonExtensions.TryParse(parts[2], out ScriptComparison comparison))
            {
                throw new Exception($"line {lineNumber}: '{parts[2]}' is not a comparison. Use == != > < >= <= & ^ | bit_set bit_clear");
            }

            VariableWidth width = opCode.Arguments[0].Width;

            if (width == VariableWidth.Bool && comparison != ScriptComparison.Equal && comparison != ScriptComparison.NotEqual)
            {
                throw new Exception($"line {lineNumber}: {parts[0]} can only compare with == or !=, not '{parts[2]}'");
            }

            if (width == VariableWidth.Float && comparison.IsBitTest())
            {
                throw new Exception($"line {lineNumber}: {parts[0]} cannot test bits, so '{parts[2]}' is not allowed");
            }

            VariableDefinition a = IsVariableToken(parts[1]) ? ResolveReadableVariable(parts[1], parts[0], ref variableMap, width) : null;
            VariableDefinition b = IsVariableToken(parts[3]) ? ResolveReadableVariable(parts[3], parts[0], ref variableMap, width) : null;

            bw.Write((ushort)opCode.OpCode);
            bw.Write((byte)((SourceOf(a) << 4) | SourceOf(b)));

            WriteOperand(bw, parts[1], a, width, lineNumber, parts[0]);
            WriteOperand(bw, parts[3], b, width, lineNumber, parts[0]);

            bw.Write((byte)comparison);

            int jumpPosition = (int)ms.Position;

            bw.Write(0);

            openBlocks.Push(new OpenBlock(lineNumber, jumpPosition, (int)ms.Position));
        }

        /// <summary>
        /// End the IF body with a jump over the else body, and point the failed comparison at the else body.
        /// </summary>
        private static void OpenElse(BinaryWriter bw, MemoryStream ms, int lineNumber, Stack<OpenBlock> openBlocks)
        {
            if (openBlocks.Count == 0)
            {
                throw new Exception($"line {lineNumber}: ELSE with no IF open");
            }

            if (openBlocks.Peek().IsElse)
            {
                throw new Exception($"line {lineNumber}: a second ELSE for the IF opened on line {openBlocks.Peek().LineNumber}");
            }

            OpenBlock ifBlock = openBlocks.Pop();

            bw.Write((ushort)FieldScriptOpCode.GotoJump);

            int jumpPosition = (int)ms.Position;

            bw.Write(0);

            WriteSkipDistance(bw, ms, ifBlock);

            openBlocks.Push(new OpenBlock(ifBlock.LineNumber, jumpPosition, (int)ms.Position, true));
        }

        /// <summary>
        /// Close the innermost IF or ELSE by writing back how many bytes its body occupies.
        /// </summary>
        private static void CloseComparison(BinaryWriter bw, MemoryStream ms, int lineNumber, Stack<OpenBlock> openBlocks)
        {
            if (openBlocks.Count == 0)
            {
                throw new Exception($"line {lineNumber}: END_IF with no IF open");
            }

            WriteSkipDistance(bw, ms, openBlocks.Pop());
        }

        private static void WriteSkipDistance(BinaryWriter bw, MemoryStream ms, OpenBlock block)
        {
            bw.Flush();

            int bodyLength = (int)ms.Position - block.BodyStart;

            long resume = ms.Position;

            ms.Position = block.JumpByteposition;

            bw.Write(bodyLength);

            bw.Flush();

            ms.Position = resume;
        }

        private static void RecordJumpSite(string[] parts, int lineIndex, int instructionStart, List<JumpSite> jumpSites)
        {
            bool isAbsolute;

            switch (parts[0])
            {
                case "GOTO_JUMP":
                    isAbsolute = false;
                    break;
                case "GOTO_DIRECTLY":
                    isAbsolute = true;
                    break;
                default:
                    return;
            }

            if (parts.Length < 2 || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int argument))
            {
                // The switch below emits this line and will raise its own error on a bad argument.
                return;
            }

            jumpSites.Add(new JumpSite(parts[0], lineIndex + 1, instructionStart, argument, isAbsolute));
        }

        private static void ValidateJumpTargets(List<JumpSite> jumpSites, List<int> instructionStarts, int bytecodeLength)
        {
            if (jumpSites.Count == 0)
            {
                return;
            }

            HashSet<int> validTargets = new HashSet<int>(instructionStarts);
            List<string> problems     = new List<string>();

            foreach (JumpSite jumpSite in jumpSites)
            {
                int target = jumpSite.ResolveTarget();

                if (target < 0 || target >= bytecodeLength)
                {
                    problems.Add($"line {jumpSite.LineNumber}: {jumpSite.ScriptName} resolves to byte {target}, outside the script (0..{bytecodeLength - 1})");
                    continue;
                }

                if (!validTargets.Contains(target))
                {
                    problems.Add($"line {jumpSite.LineNumber}: {jumpSite.ScriptName} resolves to byte {target}, which is inside an instruction rather than at the start of one");
                }
            }

            if (problems.Count > 0)
            {
                throw new Exception($"{nameof(FieldScriptCompiler)}::{nameof(ValidateJumpTargets)} {problems.Count} bad jump target(s):\n  {string.Join("\n  ", problems)}");
            }
        }

        /// <summary>
        /// Emit <c>opcode, sources, destinationAddress, value</c>.<br /><br />
        /// <c>sources</c> packs where each comes from, two nibbles to a byte: high for the destination, low for the
        /// value. 0 means the value follows inline at its width; 1..3 select Persistent/Session/Temp, and the value is
        /// the variable's width byte and address instead.
        /// </summary>
        private static void WriteBinaryArguments(BinaryWriter bw, FieldOpCodeInfo opCode, string[] parts, int lineNumber, ref VariableMapAsset variableMap)
        {
            if (parts.Length < 3)
            {
                throw new Exception($"{parts[0]} needs a destination variable and an argument, for example '{parts[0]} $myVariable 1'");
            }

            VariableWidth valueWidth = opCode.Arguments[1].Width;

            VariableDefinition destination = ResolveVariable(parts[1], parts[0], ref variableMap, opCode.Arguments[0].Width);
            VariableDefinition value       = IsVariableToken(parts[2]) ? ResolveReadableVariable(parts[2], parts[0], ref variableMap, valueWidth) : null;

            bw.Write((ushort)opCode.OpCode);
            bw.Write((byte)((ToArgumentSource(destination.Bank) << 4) | SourceOf(value)));
            bw.Write((ushort)destination.Offset);

            WriteOperand(bw, parts[2], value, valueWidth, lineNumber, parts[0]);
        }

        /// <summary>
        /// Emit <c>opcode, sources, destinationAddress</c> for opcodes that only name a destination.
        /// </summary>
        private static void WriteDestinationOnly(BinaryWriter bw, FieldOpCodeInfo opCode, string[] parts, ref VariableMapAsset variableMap)
        {
            if (parts.Length < 2)
            {
                throw new Exception($"{parts[0]} needs a destination variable, for example '{parts[0]} $myVariable'");
            }

            VariableDefinition destination = ResolveVariable(parts[1], parts[0], ref variableMap, opCode.Arguments[0].Width);

            bw.Write((ushort)opCode.OpCode);
            bw.Write((byte)(ToArgumentSource(destination.Bank) << 4));
            bw.Write((ushort)destination.Offset);
        }

        /// <summary>
        /// Emit <c>opcode, sources, value</c> for opcodes that only read a value. The value's source is the low nibble.
        /// </summary>
        private static void WriteSeed(BinaryWriter bw, FieldOpCodeInfo opCode, string[] parts, int lineNumber, ref VariableMapAsset variableMap)
        {
            if (parts.Length < 2)
            {
                throw new Exception($"{parts[0]} needs a value, for example '{parts[0]} 1'");
            }

            VariableWidth      width = opCode.Arguments[0].Width;
            VariableDefinition value = IsVariableToken(parts[1]) ? ResolveReadableVariable(parts[1], parts[0], ref variableMap, width) : null;

            bw.Write((ushort)opCode.OpCode);
            bw.Write(SourceOf(value));

            WriteOperand(bw, parts[1], value, width, lineNumber, parts[0]);
        }

        private static byte SourceOf(VariableDefinition variable)
        {
            byte source = variable == null ? ARGUMENT_IMMEDIATE : ToArgumentSource(variable.Bank);

            return source;
        }

        /// <summary>
        /// A bank-layout value: the variable's width and address when it reads one, otherwise an immediate at the
        /// value's width.
        /// </summary>
        private static void WriteOperand(BinaryWriter bw, string token, VariableDefinition variable, VariableWidth width, int lineNumber, string scriptName)
        {
            if (variable != null)
            {
                bw.Write((byte)variable.Width);
                bw.Write((ushort)variable.Offset);
                return;
            }

            bool fits;

            switch (width)
            {
                case VariableWidth.Bool:
                    fits = bool.TryParse(token, out bool boolValue);
                    bw.Write(boolValue);
                    break;

                case VariableWidth.SByte:
                    fits = sbyte.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out sbyte sbyteValue);
                    bw.Write(sbyteValue);
                    break;

                case VariableWidth.Byte:
                    fits = byte.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte byteValue);
                    bw.Write(byteValue);
                    break;

                case VariableWidth.Short:
                    fits = short.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out short shortValue);
                    bw.Write(shortValue);
                    break;

                case VariableWidth.UShort:
                    fits = ushort.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort ushortValue);
                    bw.Write(ushortValue);
                    break;

                case VariableWidth.Int:
                    fits = int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue);
                    bw.Write(intValue);
                    break;

                case VariableWidth.UInt:
                    fits = uint.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint uintValue);
                    bw.Write(uintValue);
                    break;

                case VariableWidth.Long:
                    fits = long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue);
                    bw.Write(longValue);
                    break;

                case VariableWidth.ULong:
                    fits = ulong.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong ulongValue);
                    bw.Write(ulongValue);
                    break;

                default:
                    fits = float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue);
                    bw.Write(floatValue);
                    break;
            }

            if (!fits)
            {
                throw new Exception($"line {lineNumber}: {scriptName} expected {DescribeLiteral(width)} or a $variable, got '{token}'");
            }
        }

        private static string DescribeLiteral(VariableWidth width)
        {
            string description = width switch
                                 {
                                     VariableWidth.Bool   => "true, false",
                                     VariableWidth.SByte  => $"a whole number from {sbyte.MinValue} to {sbyte.MaxValue}",
                                     VariableWidth.Byte   => $"a whole number from {byte.MinValue} to {byte.MaxValue}",
                                     VariableWidth.Short  => $"a whole number from {short.MinValue} to {short.MaxValue}",
                                     VariableWidth.UShort => $"a whole number from {ushort.MinValue} to {ushort.MaxValue}",
                                     VariableWidth.Int    => $"a whole number from {int.MinValue} to {int.MaxValue}",
                                     VariableWidth.UInt   => $"a whole number from {uint.MinValue} to {uint.MaxValue}",
                                     VariableWidth.Long   => $"a whole number from {long.MinValue} to {long.MaxValue}",
                                     VariableWidth.ULong  => $"a whole number from {ulong.MinValue} to {ulong.MaxValue}",
                                     _                    => "a number"
                                 };

            return description;
        }

        private static bool IsVariableToken(string token)
        {
            bool isVariable = token.Length > 1 && token[0] == VARIABLE_PREFIX;

            return isVariable;
        }

        /// <summary>
        /// Turn a <c>$name</c> token into the variable the map declares under that name, checking its width is
        /// exactly <paramref name="width" />, as a destination's must be. This is the whole point of the variable
        /// map: a script names what it means and the offset is resolved at build time.
        /// </summary>
        private static VariableDefinition ResolveVariable(string token, string scriptName, ref VariableMapAsset variableMap, VariableWidth width)
        {
            VariableDefinition definition = LookUpVariable(token, scriptName, ref variableMap);

            if (definition.Width != width)
            {
                throw new Exception($"[{scriptName}] needs a {width} variable but '{definition.Name}' is declared as {definition.Width}");
            }

            return definition;
        }

        /// <summary>
        /// As <see cref="ResolveVariable" />, for a variable read as a value of <paramref name="width" />, which may be
        /// narrower — see <see cref="ArgumentTypes.CanRead" />.
        /// </summary>
        private static VariableDefinition ResolveReadableVariable(string token, string scriptName, ref VariableMapAsset variableMap, VariableWidth width)
        {
            VariableDefinition definition = LookUpVariable(token, scriptName, ref variableMap);

            if (!ArgumentTypes.CanRead(width, definition.Width))
            {
                throw new Exception($"[{scriptName}] reads a {width} value, and '{definition.Name}' is a {definition.Width} variable, which does not fit");
            }

            return definition;
        }

        private static VariableDefinition LookUpVariable(string token, string scriptName, ref VariableMapAsset variableMap)
        {
            if (!IsVariableToken(token))
            {
                throw new Exception($"{scriptName} expected a variable name prefixed with '{VARIABLE_PREFIX}', got '{token}'");
            }

            variableMap ??= LoadVariableMap();

            string name = token.Substring(1);

            if (!variableMap.TryGetVariable(name, out VariableDefinition definition))
            {
                throw new KeyNotFoundException($"{nameof(FieldScriptCompiler)}::{nameof(ResolveVariable)} No variable named '{name}' in the variable map, required by [{scriptName}]");
            }

            return definition;
        }

        /// <summary>
        /// Bank as the VM's argument source nibble: 0 is reserved for immediates, so Persistent is 1.
        /// </summary>
        private static byte ToArgumentSource(MemoryBank bank)
        {
            byte source = (byte)((int)bank + 1);

            return source;
        }

        private static VariableMapAsset LoadVariableMap()
        {
            string[] assetGuids = UnityEditor.AssetDatabase.FindAssets("t:" + nameof(VariableMapAsset));

            if (assetGuids.Length == 0)
            {
                throw new Exception($"{nameof(FieldScriptCompiler)}::{nameof(LoadVariableMap)} No {nameof(VariableMapAsset)} found in the project. Create one to declare the variables scripts can name");
            }

            if (assetGuids.Length > 1)
            {
                throw new Exception($"{nameof(FieldScriptCompiler)}::{nameof(LoadVariableMap)} Found {assetGuids.Length} {nameof(VariableMapAsset)} assets. Scripts resolve variable names against exactly one map");
            }

            string           assetPath   = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuids[0]);
            VariableMapAsset variableMap = UnityEditor.AssetDatabase.LoadAssetAtPath<VariableMapAsset>(assetPath);

            return variableMap;
        }
    }
}