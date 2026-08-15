using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using RPGFramework.Hashing;

namespace RPGFramework.Field.Editor
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
                        bw.Write((ushort)FieldScriptOpCode.GotoJump);
                        bw.Write(int.Parse(parts[1], CultureInfo.InvariantCulture));
                        break;

                    case "GOTO_DIRECTLY":
                        bw.Write((ushort)FieldScriptOpCode.GotoDirectly);
                        bw.Write(int.Parse(parts[1], CultureInfo.InvariantCulture));
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
                        string   typeName   = nameof(FieldDesignerData);
                        string[] assetGuids = UnityEditor.AssetDatabase.FindAssets("t:" + typeName);
                        int      indexOfMap = -1;

                        foreach (string assetGuid in assetGuids)
                        {
                            string            assetPath     = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid);
                            FieldDesignerData fieldDesignerData = UnityEditor.AssetDatabase.LoadAssetAtPath<FieldDesignerData>(assetPath);

                            for (int i = 0; i < fieldDesignerData.FieldDatabase.Fields.Count; i++)
                            {
                                if (fieldDesignerData.FieldDatabase.Fields[i].Prefab.name == parts[1])
                                {
                                    indexOfMap = i;
                                    break;
                                }
                            }

                            if (indexOfMap != -1)
                            {
                                break;
                            }
                        }

                        if (indexOfMap == -1)
                        {
                            throw new KeyNotFoundException($"{nameof(FieldScriptCompiler)}::{nameof(Compile)} Could not find index for map {parts[1]} when compiling [JUMP_TO_MAP]");
                        }

                        int spawnId = int.Parse(parts[2], CultureInfo.InvariantCulture);

                        bw.Write((ushort)FieldScriptOpCode.JumpToAnotherMap);
                        bw.Write(indexOfMap);
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
                        bw.Write(Fnv1a64.Hash(parts[1]));
                        bw.Write(bool.Parse(parts[2]));
                        break;

                    case "ASK_PLAYER_TO_MAKE_A_CHOICE":
                        bw.Write((ushort)FieldScriptOpCode.AskPlayerToMakeAChoice);
                        bw.Write(byte.Parse(parts[1], CultureInfo.InvariantCulture));
                        bw.Write(ushort.Parse(parts[2], CultureInfo.InvariantCulture));
                        bw.Write(Fnv1a64.Hash(parts[3]));

                        byte count = (byte)(parts.Length - 4);
                        bw.Write(count);

                        for (int i = 4; i < parts.Length; i++)
                        {
                            bw.Write(Fnv1a64.Hash(parts[i]));
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
                        bw.Write(int.Parse(parts[1], CultureInfo.InvariantCulture));
                        break;

                    case "PLAY_SOUND":
                        bw.Write((ushort)FieldScriptOpCode.PlaySound);
                        bw.Write(int.Parse(parts[1], CultureInfo.InvariantCulture));
                        break;

                    default:
                        throw new Exception($"Unknown opcode '{parts[0]}'");
                }
            }

            return ms.ToArray();
        }
    }
}