#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;

namespace RPGFramework.Field
{
    public static class FieldScriptCompiler
    {
        public static byte[] Compile(string source)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);

            string[] lines = source.Split('\n');

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                string[] parts = line.Split(' ');

                switch (parts[0])
                {
                    case "RETURN":
                        bw.Write((ushort)FieldScriptOpCode.Return);
                        break;

                    case "GOTO_JUMP":
                        bw.Write((ushort)FieldScriptOpCode.Goto);
                        bw.Write(int.Parse(parts[1]));
                        break;

                    case "GOTO_DIRECTLY":
                        bw.Write((ushort)FieldScriptOpCode.GotoDirectly);
                        bw.Write(int.Parse(parts[1]));
                        break;

                    case "YIELD":
                        bw.Write((ushort)FieldScriptOpCode.Yield);
                        break;

                    case "WAIT_SECONDS":
                        bw.Write((ushort)FieldScriptOpCode.WaitSeconds);
                        bw.Write(float.Parse(parts[1], CultureInfo.InvariantCulture));
                        break;

                    case "LOCK_INPUT":
                        bool lockInput = bool.Parse(parts[1]);

                        bw.Write((ushort)FieldScriptOpCode.LockInput);
                        bw.Write(lockInput);
                        break;

                    case "JUMP_TO_MAP":
                        byte[] fieldIdBytes = FieldProvider.ToBytes(parts[1]);
                        int    spawnId      = int.Parse(parts[2], CultureInfo.InvariantCulture);

                        bw.Write((ushort)FieldScriptOpCode.JumpToAnotherMap);
                        bw.Write(fieldIdBytes);
                        bw.Write(spawnId);
                        break;

                    case "GATEWAY_TRIGGER_ACTIVATION":
                        bool gatewayTriggerActivation = bool.Parse(parts[1]);

                        bw.Write((ushort)FieldScriptOpCode.GatewayTriggerActivation);
                        bw.Write(gatewayTriggerActivation);
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
                        bw.Write(int.Parse(parts[1]));
                        break;

                    case "PLAY_SOUND":
                        bw.Write((ushort)FieldScriptOpCode.PlaySound);
                        bw.Write(int.Parse(parts[1]));
                        break;

                    default:
                        throw new Exception($"Unknown opcode '{parts[0]}'");
                }
            }

            return ms.ToArray();
        }
    }
}
#endif