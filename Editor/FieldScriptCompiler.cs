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
                    case "RETURN":
                        bw.Write((ushort)FieldScriptOpCode.Return);
                        break;

                    case "GOTO_JUMP":
                        bw.Write((ushort)FieldScriptOpCode.GotoJump);
                        bw.Write(int.Parse(parts[1], CultureInfo.InvariantCulture));
                        break;

                    case "GOTO_DIRECTLY":
                        bw.Write((ushort)FieldScriptOpCode.GotoDirectly);
                        bw.Write(int.Parse(parts[1], CultureInfo.InvariantCulture));
                        break;

                    case "IF_BYTE":
                        WriteComparison(bw, ms, FieldScriptOpCode.CompareTwoByteValues, parts, lineIndex + 1, false, ref variableMap, openBlocks);
                        break;

                    case "IF_INT":
                        WriteComparison(bw, ms, FieldScriptOpCode.CompareTwoIntValues, parts, lineIndex + 1, true, ref variableMap, openBlocks);
                        break;

                    case "END_IF":
                        CloseComparison(bw, ms, lineIndex + 1, openBlocks);
                        break;

                    case "NOP":
                        bw.Write((ushort)FieldScriptOpCode.DoNothing);
                        break;

                    case "YIELD":
                        bw.Write((ushort)FieldScriptOpCode.Yield);
                        break;

                    case "WAIT_SECONDS":
                        bw.Write((ushort)FieldScriptOpCode.WaitSeconds);
                        bw.Write(float.Parse(parts[1], CultureInfo.InvariantCulture));
                        break;

                    case "SET_BATTLE_MODE_OPTIONS":
                        bw.Write((ushort)FieldScriptOpCode.SetBattleModeOptions);
                        bw.Write(ushort.Parse(parts[1], CultureInfo.InvariantCulture));
                        bw.Write(ushort.Parse(parts[2], CultureInfo.InvariantCulture));
                        bw.Write(ushort.Parse(parts[3], CultureInfo.InvariantCulture));
                        bw.Write(byte.Parse(parts[4], CultureInfo.InvariantCulture));
                        break;

                    case "JUMP_TO_MAP":
                        int spawnId = int.Parse(parts[2], CultureInfo.InvariantCulture);

                        bw.Write((ushort)FieldScriptOpCode.JumpToAnotherMap);
                        bw.Write(HashFieldName(parts[1], lineIndex + 1));
                        bw.Write(spawnId);
                        break;

                    case "START_BATTLE":
                        bw.Write((ushort)FieldScriptOpCode.StartBattle);
                        break;

                    case "GATEWAY_TRIGGER_ACTIVATION":
                        bool gatewayTriggerActivation = bool.Parse(parts[1]);

                        bw.Write((ushort)FieldScriptOpCode.GatewayTriggerActivation);
                        bw.Write(gatewayTriggerActivation);
                        break;

                    case "SHOW_DIALOGUE_WINDOW":
                        bw.Write((ushort)FieldScriptOpCode.ShowDialogueWindow);
                        bw.Write(HashDialogueKey(parts, 1, lineIndex + 1));
                        bw.Write(bool.Parse(parts[2]));
                        break;

                    case "ASK_PLAYER_TO_MAKE_A_CHOICE":
                        bw.Write((ushort)FieldScriptOpCode.AskPlayerToMakeAChoice);
                        bw.Write(byte.Parse(parts[1], CultureInfo.InvariantCulture));
                        bw.Write(ushort.Parse(parts[2], CultureInfo.InvariantCulture));
                        bw.Write(HashDialogueKey(parts, 3, lineIndex + 1));

                        if (parts.Length < 5)
                        {
                            throw new Exception($"line {lineIndex + 1}: ASK_PLAYER_TO_MAKE_A_CHOICE needs at least one answer key after the question key");
                        }

                        byte count = (byte)(parts.Length - 4);
                        bw.Write(count);

                        for (int i = 4; i < parts.Length; i++)
                        {
                            bw.Write(HashDialogueKey(parts, i, lineIndex + 1));
                        }

                        break;

                    case "MAIN_MENU_ACCESSIBILITY":
                        bw.Write((ushort)FieldScriptOpCode.MainMenuAccessibility);
                        bw.Write(bool.Parse(parts[1]));
                        break;

                    case "CREATE_DIALOGUE_WINDOW":
                        bw.Write((ushort)FieldScriptOpCode.CreateDialogueWindow);
                        bw.Write(Fnv1a64.Hash(parts[1]));
                        bw.Write(int.Parse(parts[2], CultureInfo.InvariantCulture));
                        bw.Write(int.Parse(parts[3], CultureInfo.InvariantCulture));
                        bw.Write(int.Parse(parts[4], CultureInfo.InvariantCulture));
                        bw.Write(int.Parse(parts[5], CultureInfo.InvariantCulture));
                        break;

                    case "LOCK_INPUT":
                        bool lockInput = bool.Parse(parts[1]);

                        bw.Write((ushort)FieldScriptOpCode.LockInput);
                        bw.Write(lockInput);
                        break;

                    case "INTERACTION_TRIGGER_ACTIVATION":
                        bool interactionTriggerActivation = bool.Parse(parts[1]);

                        bw.Write((ushort)FieldScriptOpCode.InteractionTriggerActivation);
                        bw.Write(interactionTriggerActivation);
                        break;

                    case "INIT_CHARACTER":
                        bw.Write((ushort)FieldScriptOpCode.InitAsCharacter);
                        break;

                    case "VISIBILITY":
                        bool visibility = bool.Parse(parts[1]);

                        bw.Write((ushort)FieldScriptOpCode.Visibility);
                        bw.Write(visibility);
                        break;

                    case "SET_ENTITY_POSITION":
                        bw.Write((ushort)FieldScriptOpCode.SetEntityPosition);
                        bw.Write(float.Parse(parts[1], CultureInfo.InvariantCulture));
                        bw.Write(float.Parse(parts[2], CultureInfo.InvariantCulture));
                        bw.Write(float.Parse(parts[3], CultureInfo.InvariantCulture));
                        break;

                    case "SET_MOVEMENT_SPEED":
                        bw.Write((ushort)FieldScriptOpCode.SetMovementSpeed);
                        bw.Write(float.Parse(parts[1], CultureInfo.InvariantCulture));
                        break;

                    case "SET_ENTITY_ROTATION":
                        bw.Write((ushort)FieldScriptOpCode.SetEntityRotation);
                        bw.Write(float.Parse(parts[1], CultureInfo.InvariantCulture));
                        bw.Write(float.Parse(parts[2], CultureInfo.InvariantCulture));
                        bw.Write(float.Parse(parts[3], CultureInfo.InvariantCulture));
                        break;

                    case "SET_ENTITY_ROTATION_ASYNC":
                        bw.Write((ushort)FieldScriptOpCode.SetEntityRotationAsync);
                        bw.Write(float.Parse(parts[1], CultureInfo.InvariantCulture));
                        bw.Write(float.Parse(parts[2], CultureInfo.InvariantCulture));
                        bw.Write(float.Parse(parts[3], CultureInfo.InvariantCulture));
                        bw.Write(byte.Parse(parts[4], CultureInfo.InvariantCulture));
                        bw.Write(float.Parse(parts[5], CultureInfo.InvariantCulture));
                        bw.Write(byte.Parse(parts[6], CultureInfo.InvariantCulture));
                        break;

                    case "SET_DIRECTION_TO_FACE_ENTITY":
                        bw.Write((ushort)FieldScriptOpCode.SetDirectionToFaceEntity);
                        bw.Write(byte.Parse(parts[1], CultureInfo.InvariantCulture));
                        break;

                    case "SET_INTERACTION_RANGE":
                        float interactionTriggerSize = float.Parse(parts[1], CultureInfo.InvariantCulture);

                        bw.Write((ushort)FieldScriptOpCode.SetInteractionRange);
                        bw.Write(interactionTriggerSize);
                        break;

                    case "PLAY_MUSIC":
                        bw.Write((ushort)FieldScriptOpCode.PlayMusic);
                        bw.Write(Fnv1a64.Hash(parts[1]));
                        bw.Write(Fnv1a64.Hash(parts[2]));
                        break;

                    // parts[1] is the track the author had in mind. It is not emitted: the opcode applies
                    // to whatever is playing, and it is in the script only so the editor can offer that
                    // track's states rather than every track's.
                    case "MUSIC_STEM_STATE":
                        bw.Write((ushort)FieldScriptOpCode.SetMusicStemState);
                        bw.Write(Fnv1a64.Hash(parts[2]));
                        bw.Write(float.Parse(parts[3], CultureInfo.InvariantCulture));
                        break;

                    case "PLAY_SOUND":
                        bw.Write((ushort)FieldScriptOpCode.PlaySound);
                        bw.Write(Fnv1a64.Hash(parts[1]));
                        break;

                    case "ASSIGN_BYTE":
                        WriteBinaryArguments(bw, FieldScriptOpCode.AssignValue8Bit, parts, ref variableMap, false);
                        break;

                    case "ASSIGN_SHORT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.AssignValue16Bit, parts, ref variableMap, true);
                        break;

                    case "ADD_BYTE":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Addition8Bit, parts, ref variableMap, false);
                        break;

                    case "ADD_SHORT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Addition16Bit, parts, ref variableMap, true);
                        break;

                    case "ADD_BYTE_CLAMPED":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Addition8BitClamped, parts, ref variableMap, false);
                        break;

                    case "ADD_SHORT_CLAMPED":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Addition16BitClamped, parts, ref variableMap, true);
                        break;

                    case "SUB_BYTE":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Subtraction8Bit, parts, ref variableMap, false);
                        break;

                    case "SUB_SHORT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Subtraction16Bit, parts, ref variableMap, true);
                        break;

                    case "SUB_BYTE_CLAMPED":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Subtraction8BitClamped, parts, ref variableMap, false);
                        break;

                    case "SUB_SHORT_CLAMPED":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Subtraction16BitClamped, parts, ref variableMap, true);
                        break;

                    case "MUL_BYTE":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Multiplication8Bit, parts, ref variableMap, false);
                        break;

                    case "MUL_SHORT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Multiplication16Bit, parts, ref variableMap, true);
                        break;

                    case "DIV_BYTE":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Division8Bit, parts, ref variableMap, false);
                        break;

                    case "DIV_SHORT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Division16Bit, parts, ref variableMap, true);
                        break;

                    case "MOD_BYTE":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Remainder8Bit, parts, ref variableMap, false);
                        break;

                    case "MOD_SHORT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.Remainder16Bit, parts, ref variableMap, true);
                        break;

                    case "AND_BYTE":
                        WriteBinaryArguments(bw, FieldScriptOpCode.BitwiseAnd8Bit, parts, ref variableMap, false);
                        break;

                    case "AND_SHORT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.BitwiseAnd16Bit, parts, ref variableMap, true);
                        break;

                    case "OR_BYTE":
                        WriteBinaryArguments(bw, FieldScriptOpCode.BitwiseOr8Bit, parts, ref variableMap, false);
                        break;

                    case "OR_SHORT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.BitwiseOr16Bit, parts, ref variableMap, true);
                        break;

                    case "XOR_BYTE":
                        WriteBinaryArguments(bw, FieldScriptOpCode.BitwiseXor8Bit, parts, ref variableMap, false);
                        break;

                    case "XOR_SHORT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.BitwiseXor16Bit, parts, ref variableMap, true);
                        break;


                    case "SET_BIT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.SetBit, parts, ref variableMap, false);
                        break;

                    case "UNSET_BIT":
                        WriteBinaryArguments(bw, FieldScriptOpCode.UnsetBit, parts, ref variableMap, false);
                        break;

                    case "GET_RANDOM":
                        WriteBinaryArguments(bw, FieldScriptOpCode.GetRandomNumber, parts, ref variableMap, false);
                        break;

                    case "INC_BYTE":
                        WriteDestinationOnly(bw, FieldScriptOpCode.Increment8Bit, parts, ref variableMap, false);
                        break;

                    case "INC_SHORT":
                        WriteDestinationOnly(bw, FieldScriptOpCode.Increment16Bit, parts, ref variableMap, true);
                        break;

                    case "INC_BYTE_CLAMPED":
                        WriteDestinationOnly(bw, FieldScriptOpCode.Increment8BitClamped, parts, ref variableMap, false);
                        break;

                    case "INC_SHORT_CLAMPED":
                        WriteDestinationOnly(bw, FieldScriptOpCode.Increment16BitClamped, parts, ref variableMap, true);
                        break;

                    case "DEC_BYTE":
                        WriteDestinationOnly(bw, FieldScriptOpCode.Decrement8Bit, parts, ref variableMap, false);
                        break;

                    case "DEC_SHORT":
                        WriteDestinationOnly(bw, FieldScriptOpCode.Decrement16Bit, parts, ref variableMap, true);
                        break;

                    case "DEC_BYTE_CLAMPED":
                        WriteDestinationOnly(bw, FieldScriptOpCode.Decrement8BitClamped, parts, ref variableMap, false);
                        break;

                    case "DEC_SHORT_CLAMPED":
                        WriteDestinationOnly(bw, FieldScriptOpCode.Decrement16BitClamped, parts, ref variableMap, true);
                        break;

                    case "REQUEST_SCRIPT":
                        WriteScriptRequest(bw, FieldScriptOpCode.RunAnotherEntityScriptUnlessBusy, parts, lineIndex + 1);
                        break;
                    case "REQUEST_SCRIPT_WAIT_START":
                        WriteScriptRequest(bw, FieldScriptOpCode.RunAnotherEntityScriptWaitUntilStarted, parts, lineIndex + 1);
                        break;
                    case "REQUEST_SCRIPT_WAIT_END":
                        WriteScriptRequest(bw, FieldScriptOpCode.RunAnotherEntityScriptWaitUntilFinished, parts, lineIndex + 1);
                        break;

                    case "RETURN_TO_SCRIPT":
                        if (parts.Length < 2)
                        {
                            throw new Exception($"line {lineIndex + 1}: RETURN_TO_SCRIPT needs an event id");
                        }

                        bw.Write((ushort)FieldScriptOpCode.ReturnToAnotherScript);
                        bw.Write(ushort.Parse(parts[1], CultureInfo.InvariantCulture));
                        break;

                    case "RANDOM_SEED":
                        bw.Write((ushort)FieldScriptOpCode.RandomNumberSeed);
                        bw.Write((byte)ARGUMENT_IMMEDIATE);
                        bw.Write(int.Parse(parts[1], CultureInfo.InvariantCulture));
                        break;

                    default:
                        throw new Exception($"Unknown opcode '{parts[0]}'");
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
        /// Emit <c>opcode, targetEntityId, priority, targetScriptId</c>, the shape shared by the three
        /// request opcodes.
        /// </summary>
        private static void WriteScriptRequest(BinaryWriter bw, FieldScriptOpCode opCode, string[] parts, int lineNumber)
        {
            if (parts.Length < 4)
            {
                throw new Exception($"line {lineNumber}: {parts[0]} needs a target entity id, a priority (0-7) and an event id, for example '{parts[0]} 3 0 1' to run entity 3's second script");
            }

            byte priority = byte.Parse(parts[2], CultureInfo.InvariantCulture);

            if (priority >= SCRIPT_PRIORITY_COUNT)
            {
                throw new Exception($"line {lineNumber}: {parts[0]} priority [{priority}] is outside 0..{SCRIPT_PRIORITY_COUNT - 1}");
            }

            bw.Write((ushort)opCode);
            bw.Write(byte.Parse(parts[1], CultureInfo.InvariantCulture));
            bw.Write(priority);
            bw.Write(ushort.Parse(parts[3], CultureInfo.InvariantCulture));
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

            internal OpenBlock(int lineNumber, int jumpBytePosition, int bodyStart)
            {
                LineNumber       = lineNumber;
                JumpByteposition = jumpBytePosition;
                BodyStart        = bodyStart;
            }
        }

        /// <summary>
        /// Emit a comparison and open a block. The instructions that follow are its body, and the jump
        /// distance written here is the number of bytes to skip when the comparison does not hold — so
        /// it is filled in by <see cref="CloseComparison" /> rather than written by an author.
        /// </summary>
        private static void WriteComparison(BinaryWriter bw, MemoryStream ms, FieldScriptOpCode opCode, string[] parts, int lineNumber, bool isInt, ref VariableMapAsset variableMap, Stack<OpenBlock> openBlocks)
        {
            if (parts.Length < 4)
            {
                throw new Exception($"line {lineNumber}: {parts[0]} needs a value, a comparison and a second value, for example '{parts[0]} $flag == 1'");
            }

            if (!ScriptComparisonExtensions.TryParse(parts[2], out ScriptComparison comparison))
            {
                throw new Exception($"line {lineNumber}: '{parts[2]}' is not a comparison. Use == != > < >= <= & ^ | bit_set bit_clear");
            }

            byte   aSource  = ARGUMENT_IMMEDIATE;
            byte   bSource  = ARGUMENT_IMMEDIATE;
            ushort aAddress = 0;
            ushort bAddress = 0;

            if (IsVariableToken(parts[1]))
            {
                VariableDefinition a = ResolveVariable(parts[1], parts[0], ref variableMap, isInt);
                aSource  = ToArgumentSource(a.Bank);
                aAddress = (ushort)a.Offset;
            }

            if (IsVariableToken(parts[3]))
            {
                VariableDefinition b = ResolveVariable(parts[3], parts[0], ref variableMap, isInt);
                bSource  = ToArgumentSource(b.Bank);
                bAddress = (ushort)b.Offset;
            }

            bw.Write((ushort)opCode);
            bw.Write((byte)((aSource << 4) | bSource));

            WriteComparisonValue(bw, parts[1], aSource, aAddress, isInt, lineNumber, parts[0]);
            WriteComparisonValue(bw, parts[3], bSource, bAddress, isInt, lineNumber, parts[0]);

            bw.Write((byte)comparison);

            int jumpBytePosition = (int)ms.Position;

            bw.Write((byte)0);

            openBlocks.Push(new OpenBlock(lineNumber, jumpBytePosition, (int)ms.Position));
        }

        private static void WriteComparisonValue(BinaryWriter bw, string token, byte source, ushort address, bool isInt, int lineNumber, string scriptName)
        {
            if (source != ARGUMENT_IMMEDIATE)
            {
                bw.Write(address);
                return;
            }

            if (isInt)
            {
                if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                {
                    throw new Exception($"line {lineNumber}: {scriptName} expected a number or a $variable, got '{token}'");
                }

                bw.Write(intValue);
                return;
            }

            if (!byte.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte byteValue))
            {
                throw new Exception($"line {lineNumber}: {scriptName} expected a number 0-255 or a $variable, got '{token}'");
            }

            bw.Write(byteValue);
        }

        /// <summary>
        /// Close the innermost IF by writing back how many bytes its body occupies. The VM adds that to
        /// the instruction pointer when the comparison fails, landing on the instruction after END_IF.
        /// </summary>
        private static void CloseComparison(BinaryWriter bw, MemoryStream ms, int lineNumber, Stack<OpenBlock> openBlocks)
        {
            if (openBlocks.Count == 0)
            {
                throw new Exception($"line {lineNumber}: END_IF with no IF open");
            }

            OpenBlock block = openBlocks.Pop();

            bw.Flush();

            int bodyLength = (int)ms.Position - block.BodyStart;

            // The distance is a single byte, so a body longer than 255 bytes cannot be skipped.
            if (bodyLength > byte.MaxValue)
            {
                throw new Exception($"line {lineNumber}: the IF opened on line {block.LineNumber} has a {bodyLength} byte body, more than the {byte.MaxValue} a jump distance can hold. Move some of it into another script and call that instead");
            }

            long resume = ms.Position;

            ms.Position = block.JumpByteposition;
            bw.Write((byte)bodyLength);
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
        /// Global/Session/Temp and a ushort address follows instead.
        /// </summary>
        private static void WriteBinaryArguments(BinaryWriter bw, FieldScriptOpCode opCode, string[] parts, ref VariableMapAsset variableMap, bool is16Bit)
        {
            if (parts.Length < 3)
            {
                throw new Exception($"{parts[0]} needs a destination variable and an argument, for example '{parts[0]} $myVariable 1'");
            }

            VariableDefinition destination = ResolveVariable(parts[1], parts[0], ref variableMap, is16Bit);

            bool   argumentIsVariable = IsVariableToken(parts[2]);
            byte   argumentSource     = ARGUMENT_IMMEDIATE;
            ushort argumentAddress    = 0;

            if (argumentIsVariable)
            {
                // The argument's width only has to match when it is the same size as the destination;
                // SET_BIT and GET_RANDOM take a byte-sized argument whatever the destination is.
                VariableDefinition argument = ResolveVariable(parts[2], parts[0], ref variableMap, is16Bit);

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

            if (is16Bit)
            {
                bw.Write(ushort.Parse(parts[2], CultureInfo.InvariantCulture));
                return;
            }

            bw.Write(byte.Parse(parts[2], CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Emit <c>opcode, sources, destinationAddress</c> for opcodes that only name a destination.
        /// </summary>
        private static void WriteDestinationOnly(BinaryWriter bw, FieldScriptOpCode opCode, string[] parts, ref VariableMapAsset variableMap, bool is16Bit)
        {
            if (parts.Length < 2)
            {
                throw new Exception($"{parts[0]} needs a destination variable, for example '{parts[0]} $myVariable'");
            }

            VariableDefinition destination = ResolveVariable(parts[1], parts[0], ref variableMap, is16Bit);

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
        private static VariableDefinition ResolveVariable(string token, string scriptName, ref VariableMapAsset variableMap, bool is16Bit)
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

            bool definitionIs16Bit = definition.Width == VariableWidth.UShort;

            if (is16Bit != definitionIs16Bit)
            {
                string expected = is16Bit ? "a 16 bit" : "an 8 bit";
                throw new Exception($"[{scriptName}] needs {expected} variable but '{name}' is declared as {definition.Width}");
            }

            return definition;
        }

        /// <summary>
        /// Bank as the VM's argument source nibble: 0 is reserved for immediates, so Global is 1.
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