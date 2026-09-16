using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Hashing;

namespace RPGFramework.Field.Editor
{
    public static class FieldScriptCompiler
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
                    case "IF_BYTE":
                        WriteComparison(bw, ms, FieldScriptOpCode.CompareTwoByteValues, parts, lineIndex + 1, VariableWidth.Byte, ref variableMap, openBlocks);
                        break;

                    case "IF_INT":
                        WriteComparison(bw, ms, FieldScriptOpCode.CompareTwoIntValues, parts, lineIndex + 1, VariableWidth.Int, ref variableMap, openBlocks);
                        break;

                    case "IF_BOOL":
                        WriteComparison(bw, ms, FieldScriptOpCode.CompareTwoBoolValues, parts, lineIndex + 1, VariableWidth.Bool, ref variableMap, openBlocks);
                        break;

                    case "ELSE":
                        OpenElse(bw, ms, lineIndex + 1, openBlocks);
                        break;

                    case "END_IF":
                        CloseComparison(bw, ms, lineIndex + 1, openBlocks);
                        break;

                    case "ASK_PLAYER_TO_MAKE_A_CHOICE":
                        if (parts.Length < 4)
                        {
                            throw new Exception($"line {lineIndex + 1}: ASK_PLAYER_TO_MAKE_A_CHOICE needs at least one answer key after the question key");
                        }

                        VariableDefinition choice = ResolveVariable(parts[1], parts[0], ref variableMap, VariableWidth.Byte);

                        bw.Write((ushort)FieldScriptOpCode.AskPlayerToMakeAChoice);
                        bw.Write((byte)choice.Bank);
                        bw.Write((ushort)choice.Offset);
                        bw.Write(HashDialogueKey(parts, 2, lineIndex + 1));

                        byte count = (byte)(parts.Length - 3);
                        bw.Write(count);

                        for (int i = 3; i < parts.Length; i++)
                        {
                            bw.Write(HashDialogueKey(parts, i, lineIndex + 1));
                        }

                        break;

                    case "ASSIGN_BYTE":
                        WriteBinaryArguments(bw, FieldScriptOpCode.AssignValue8Bit, parts, ref variableMap, VariableWidth.Byte);
                        break;

                    case "SET_BOOL":
                        WriteBinaryArguments(bw, FieldScriptOpCode.AssignValueBool, parts, ref variableMap, VariableWidth.Bool);
                        break;

                    case "ASSIGN_SHORT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.AssignValue16Bit, parts, ref variableMap, VariableWidth.UShort);
                        break;

                    case "ADD_BYTE":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Addition8Bit, parts, ref variableMap, VariableWidth.Byte);
                        break;

                    case "ADD_SHORT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Addition16Bit, parts, ref variableMap, VariableWidth.UShort);
                        break;

                    case "ADD_BYTE_CLAMPED":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Addition8BitClamped, parts, ref variableMap, VariableWidth.Byte);
                        break;

                    case "ADD_SHORT_CLAMPED":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Addition16BitClamped, parts, ref variableMap, VariableWidth.UShort);
                        break;

                    case "SUB_BYTE":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Subtraction8Bit, parts, ref variableMap, VariableWidth.Byte);
                        break;

                    case "SUB_SHORT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Subtraction16Bit, parts, ref variableMap, VariableWidth.UShort);
                        break;

                    case "SUB_BYTE_CLAMPED":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Subtraction8BitClamped, parts, ref variableMap, VariableWidth.Byte);
                        break;

                    case "SUB_SHORT_CLAMPED":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Subtraction16BitClamped, parts, ref variableMap, VariableWidth.UShort);
                        break;

                    case "MUL_BYTE":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Multiplication8Bit, parts, ref variableMap, VariableWidth.Byte);
                        break;

                    case "MUL_SHORT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Multiplication16Bit, parts, ref variableMap, VariableWidth.UShort);
                        break;

                    case "DIV_BYTE":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Division8Bit, parts, ref variableMap, VariableWidth.Byte);
                        break;

                    case "DIV_SHORT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Division16Bit, parts, ref variableMap, VariableWidth.UShort);
                        break;

                    case "MOD_BYTE":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Remainder8Bit, parts, ref variableMap, VariableWidth.Byte);
                        break;

                    case "MOD_SHORT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Remainder16Bit, parts, ref variableMap, VariableWidth.UShort);
                        break;

                    case "AND_BYTE":
                        WriteBinaryArguments(bw, FieldScriptOpCode.BitwiseAnd8Bit, parts, ref variableMap, VariableWidth.Byte);
                        break;

                    case "AND_SHORT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.BitwiseAnd16Bit, parts, ref variableMap, VariableWidth.UShort);
                        break;

                    case "OR_BYTE":
                        WriteBinaryArguments(bw, FieldScriptOpCode.BitwiseOr8Bit, parts, ref variableMap, VariableWidth.Byte);
                        break;

                    case "OR_SHORT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.BitwiseOr16Bit, parts, ref variableMap, VariableWidth.UShort);
                        break;

                    case "XOR_BYTE":
                        WriteBinaryArguments(bw, FieldScriptOpCode.BitwiseXor8Bit, parts, ref variableMap, VariableWidth.Byte);
                        break;

                    case "XOR_SHORT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.BitwiseXor16Bit, parts, ref variableMap, VariableWidth.UShort);
                        break;

                    case "SET_BIT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.SetBit, parts, ref variableMap, VariableWidth.Byte);
                        break;

                    case "UNSET_BIT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.UnsetBit, parts, ref variableMap, VariableWidth.Byte);
                        break;

                    case "GET_RANDOM":
                        WriteDestinationOnly(bw, FieldScriptOpCode.GetRandomNumber, parts, ref variableMap, VariableWidth.Byte);
                        break;

                    case "INC_BYTE":
                        WriteDestinationOnly(bw, FieldScriptOpCode.Increment8Bit, parts, ref variableMap, VariableWidth.Byte);
                        break;

                    case "INC_SHORT":
                        WriteDestinationOnly(bw, FieldScriptOpCode.Increment16Bit, parts, ref variableMap, VariableWidth.UShort);
                        break;

                    case "INC_BYTE_CLAMPED":
                        WriteDestinationOnly(bw, FieldScriptOpCode.Increment8BitClamped, parts, ref variableMap, VariableWidth.Byte);
                        break;

                    case "INC_SHORT_CLAMPED":
                        WriteDestinationOnly(bw, FieldScriptOpCode.Increment16BitClamped, parts, ref variableMap, VariableWidth.UShort);
                        break;

                    case "DEC_BYTE":
                        WriteDestinationOnly(bw, FieldScriptOpCode.Decrement8Bit, parts, ref variableMap, VariableWidth.Byte);
                        break;

                    case "DEC_SHORT":
                        WriteDestinationOnly(bw, FieldScriptOpCode.Decrement16Bit, parts, ref variableMap, VariableWidth.UShort);
                        break;

                    case "DEC_BYTE_CLAMPED":
                        WriteDestinationOnly(bw, FieldScriptOpCode.Decrement8BitClamped, parts, ref variableMap, VariableWidth.Byte);
                        break;

                    case "DEC_SHORT_CLAMPED":
                        WriteDestinationOnly(bw, FieldScriptOpCode.Decrement16BitClamped, parts, ref variableMap, VariableWidth.UShort);
                        break;

                    case "RANDOM_SEED":
                        bw.Write((ushort)FieldScriptOpCode.RandomNumberSeed);
                        bw.Write((byte)ARGUMENT_IMMEDIATE);
                        bw.Write(int.Parse(parts[1], CultureInfo.InvariantCulture));
                        break;

                    default:
                        if (!FieldOpCodeCatalogue.TryGet(parts[0], out FieldOpCodeInfo opCode) || opCode.Layout != ArgumentLayout.Sequential)
                        {
                            throw new Exception($"Unknown opcode '{parts[0]}'");
                        }

                        WriteSequential(bw, ms, opCode, parts, lineIndex + 1, ref variableMap);
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

        /// <summary>
        /// Encode an opcode from its argument attributes. See <see cref="ArgumentLayout.Sequential" /> for the layout.
        /// </summary>
        private static void WriteSequential(BinaryWriter bw, MemoryStream ms, FieldOpCodeInfo opCode, string[] parts, int lineNumber, ref VariableMapAsset variableMap)
        {
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
                ArgumentTypes.TryGetVariableWidth(type, out VariableWidth width);

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
                    bw.Write(Fnv1a64.Hash(parts[part]));
                    break;

                // Named so the editor can offer the right states; the opcode applies to whatever is playing.
                case ArgumentType.MusicNameHint:
                    break;

                case ArgumentType.LocalisationKey:
                    bw.Write(HashDialogueKey(parts, part, lineNumber));
                    break;

                default:
                    throw new Exception($"line {lineNumber}: {parts[0]} has a {type} argument, which a sequential opcode cannot encode");
            }
        }

        /// <summary>
        /// An IF whose body is still being written. The jump distance cannot be known until the body
        /// ends, so a placeholder byte is written and its position kept until END_IF.
        /// </summary>
        private readonly struct OpenBlock
        {
            internal readonly int LineNumber;
            internal readonly int JumpByteposition;
            internal readonly int BodyStart;

            // An else body is skipped by a GOTO_JUMP with an int distance; an IF body by the comparison's byte.
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
        private static void WriteComparison(BinaryWriter bw, MemoryStream ms, FieldScriptOpCode opCode, string[] parts, int lineNumber, VariableWidth width, ref VariableMapAsset variableMap, Stack<OpenBlock> openBlocks)
        {
            if (parts.Length < 4)
            {
                throw new Exception($"line {lineNumber}: {parts[0]} needs a value, a comparison and a second value, for example '{parts[0]} $flag == 1'");
            }

            if (!ScriptComparisonExtensions.TryParse(parts[2], out ScriptComparison comparison))
            {
                throw new Exception($"line {lineNumber}: '{parts[2]}' is not a comparison. Use == != > < >= <= & ^ | bit_set bit_clear");
            }

            if (width == VariableWidth.Bool && comparison != ScriptComparison.Equal && comparison != ScriptComparison.NotEqual)
            {
                throw new Exception($"line {lineNumber}: {parts[0]} can only compare with == or !=, not '{parts[2]}'");
            }

            byte   aSource  = ARGUMENT_IMMEDIATE;
            byte   bSource  = ARGUMENT_IMMEDIATE;
            ushort aAddress = 0;
            ushort bAddress = 0;

            if (IsVariableToken(parts[1]))
            {
                VariableDefinition a = ResolveVariable(parts[1], parts[0], ref variableMap, width);
                aSource  = ToArgumentSource(a.Bank);
                aAddress = (ushort)a.Offset;
            }

            if (IsVariableToken(parts[3]))
            {
                VariableDefinition b = ResolveVariable(parts[3], parts[0], ref variableMap, width);
                bSource  = ToArgumentSource(b.Bank);
                bAddress = (ushort)b.Offset;
            }

            bw.Write((ushort)opCode);
            bw.Write((byte)((aSource << 4) | bSource));

            WriteComparisonValue(bw, parts[1], aSource, aAddress, width, lineNumber, parts[0]);
            WriteComparisonValue(bw, parts[3], bSource, bAddress, width, lineNumber, parts[0]);

            bw.Write((byte)comparison);

            int jumpBytePosition = (int)ms.Position;

            bw.Write((byte)0);

            openBlocks.Push(new OpenBlock(lineNumber, jumpBytePosition, (int)ms.Position));
        }

        private static void WriteComparisonValue(BinaryWriter bw, string token, byte source, ushort address, VariableWidth width, int lineNumber, string scriptName)
        {
            if (source != ARGUMENT_IMMEDIATE)
            {
                bw.Write(address);
                return;
            }

            switch (width)
            {
                case VariableWidth.Int:
                    if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                    {
                        throw new Exception($"line {lineNumber}: {scriptName} expected a number or a $variable, got '{token}'");
                    }

                    bw.Write(intValue);
                    return;

                case VariableWidth.Bool:
                    if (!bool.TryParse(token, out bool boolValue))
                    {
                        throw new Exception($"line {lineNumber}: {scriptName} expected true, false or a $variable, got '{token}'");
                    }

                    bw.Write(boolValue);
                    return;

                default:
                    if (!byte.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte byteValue))
                    {
                        throw new Exception($"line {lineNumber}: {scriptName} expected a number 0-255 or a $variable, got '{token}'");
                    }

                    bw.Write(byteValue);
                    return;
            }
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

            WriteSkipDistance(bw, ms, ifBlock, lineNumber);

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

            WriteSkipDistance(bw, ms, openBlocks.Pop(), lineNumber);
        }

        private static void WriteSkipDistance(BinaryWriter bw, MemoryStream ms, OpenBlock block, int lineNumber)
        {
            bw.Flush();

            int bodyLength = (int)ms.Position - block.BodyStart;

            if (!block.IsElse && bodyLength > byte.MaxValue)
            {
                throw new Exception($"line {lineNumber}: the IF opened on line {block.LineNumber} has a {bodyLength} byte body, more than the {byte.MaxValue} a jump distance can hold. Move some of it into another script and call that instead");
            }

            long resume = ms.Position;

            ms.Position = block.JumpByteposition;

            if (block.IsElse)
            {
                bw.Write(bodyLength);
            }
            else
            {
                bw.Write((byte)bodyLength);
            }

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
        /// Emit <c>opcode, sources, destinationAddress, argument</c>.<br /><br />
        /// <c>sources</c> packs where each argument comes from, two nibbles to a byte: high for the
        /// destination, low for the second argument. 0 means the value follows inline, 1..3 select
        /// Persistent/Session/Temp and a ushort address follows instead.
        /// </summary>
        private static void WriteBinaryArguments(BinaryWriter bw, FieldScriptOpCode opCode, string[] parts, ref VariableMapAsset variableMap, VariableWidth width)
        {
            if (parts.Length < 3)
            {
                throw new Exception($"{parts[0]} needs a destination variable and an argument, for example '{parts[0]} $myVariable 1'");
            }

            VariableDefinition destination = ResolveVariable(parts[1], parts[0], ref variableMap, width);

            bool   argumentIsVariable = IsVariableToken(parts[2]);
            byte   argumentSource     = ARGUMENT_IMMEDIATE;
            ushort argumentAddress    = 0;

            if (argumentIsVariable)
            {
                VariableDefinition argument = ResolveVariable(parts[2], parts[0], ref variableMap, width);

                argumentSource  = ToArgumentSource(argument.Bank);
                argumentAddress = (ushort)argument.Offset;
            }

            byte sources = (byte)((ToArgumentSource(destination.Bank) << 4) | argumentSource);

            bw.Write((ushort)opCode);
            bw.Write(sources);
            bw.Write((ushort)destination.Offset);

            if (argumentIsVariable)
            {
                bw.Write(argumentAddress);
                return;
            }

            switch (width)
            {
                case VariableWidth.UShort:
                    bw.Write(ushort.Parse(parts[2], CultureInfo.InvariantCulture));
                    return;

                case VariableWidth.Bool:
                    bw.Write(bool.Parse(parts[2]));
                    return;

                default:
                    bw.Write(byte.Parse(parts[2], CultureInfo.InvariantCulture));
                    return;
            }
        }

        /// <summary>
        /// Emit <c>opcode, sources, destinationAddress</c> for opcodes that only name a destination.
        /// </summary>
        private static void WriteDestinationOnly(BinaryWriter bw, FieldScriptOpCode opCode, string[] parts, ref VariableMapAsset variableMap, VariableWidth width)
        {
            if (parts.Length < 2)
            {
                throw new Exception($"{parts[0]} needs a destination variable, for example '{parts[0]} $myVariable'");
            }

            VariableDefinition destination = ResolveVariable(parts[1], parts[0], ref variableMap, width);

            byte sources = (byte)(ToArgumentSource(destination.Bank) << 4);

            bw.Write((ushort)opCode);
            bw.Write(sources);
            bw.Write((ushort)destination.Offset);
        }

        private static bool IsVariableToken(string token)
        {
            bool isVariable = token.Length > 1 && token[0] == VARIABLE_PREFIX;

            return isVariable;
        }

        /// <summary>
        /// Turn a <c>$name</c> token into the variable the map declares under that name, checking that its
        /// width matches the opcode being used. This is the whole point of the variable map: a script names
        /// what it means and the offset is resolved at build time.
        /// </summary>
        private static VariableDefinition ResolveVariable(string token, string scriptName, ref VariableMapAsset variableMap, VariableWidth width)
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

            if (definition.Width != width)
            {
                throw new Exception($"[{scriptName}] needs a {width} variable but '{name}' is declared as {definition.Width}");
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