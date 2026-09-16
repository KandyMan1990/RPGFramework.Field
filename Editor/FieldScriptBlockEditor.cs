using System;
using System.Collections.Generic;
using RPGFramework.Core.Memory;
using RPGFramework.Audio.Music;
using RPGFramework.Audio.Sfx;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// Builds a script by stacking blocks rather than typing text.<br /><br />
    /// Every opcode the engine can run offers itself here, with one labelled input per argument, taken
    /// from the attributes on <see cref="FieldScriptOpCode" />. Nothing describes the opcodes twice: add
    /// an opcode to the enum with its attributes and it appears in the picker.<br /><br />
    /// The script text stays the source of truth. This edits it, so a script can still be read, diffed
    /// and hand-edited, and a line the editor does not recognise is preserved rather than dropped.
    /// </summary>
    public sealed class FieldScriptBlockEditor : VisualElement
    {
        private readonly List<FieldScriptBlock> m_Blocks;
        private readonly VisualElement          m_BlockList;
        private readonly Action<string>         m_OnChanged;

        private VariableMapAsset m_VariableMap;

        // Gathered once per editor rather than per control: a script with twenty blocks would otherwise
        // sweep the AssetDatabase twenty times.
        private List<string> m_FieldNames;

        // Keyed by field name: which spawn points exist depends on which field is being jumped to.
        private readonly Dictionary<string, List<SpawnChoice>> m_SpawnChoices = new Dictionary<string, List<SpawnChoice>>();
        private          List<string>                          m_MusicNames;
        private          List<string>                          m_SoundNames;
        private          List<string>                          m_MusicStateNames;

        // Keyed by track name: which stem states exist depends on which track is being layered.
        private readonly Dictionary<string, List<string>> m_StateNamesByTrack = new Dictionary<string, List<string>>();

        public FieldScriptBlockEditor(string scriptText, Action<string> onChanged)
        {
            m_OnChanged = onChanged;
            m_Blocks    = FieldScriptBlocks.Parse(scriptText);

            Add(BuildToolbar());

            m_BlockList = new VisualElement();
            Add(m_BlockList);

            RebuildBlockList();
        }

        private VisualElement BuildToolbar()
        {
            VisualElement toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.marginBottom  = 6;

            Button addButton = new Button(() => ShowOpCodePicker(m_Blocks)) { text = "Add block" };
            addButton.style.flexGrow = 1;

            toolbar.Add(addButton);

            return toolbar;
        }

        /// <summary>
        /// A searchable list of every opcode that can actually run, grouped the way the enum groups them.
        /// </summary>
        private void ShowOpCodePicker(List<FieldScriptBlock> target)
        {
            GenericMenu menu = new GenericMenu();

            foreach (FieldOpCodeInfo opCode in FieldOpCodeCatalogue.All)
            {
                string category = GetCategory(opCode.OpCode);
                string label = string.IsNullOrEmpty(opCode.Summary)
                                   ? $"{category}/{opCode.ScriptName}"
                                   : $"{category}/{opCode.ScriptName}  —  {opCode.Summary}";

                FieldOpCodeInfo captured = opCode;

                menu.AddItem(new GUIContent(label), false, () =>
                                                           {
                                                               target.Add(FieldScriptBlock.CreateEmpty(captured));
                                                               RebuildBlockList();
                                                               NotifyChanged();
                                                           });
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// Opcode ids are laid out in a block of 256 per category, so the high byte names the group
        /// without the enum needing to repeat it in an attribute.
        /// </summary>
        private static string GetCategory(FieldScriptOpCode opCode)
        {
            switch ((ushort)opCode >> 8)
            {
                case 0x00: return "Flow";
                case 0x01: return "System";
                case 0x02: return "Maths";
                case 0x03: return "Windows and menus";
                case 0x04: return "Party and inventory";
                case 0x05: return "Entities";
                case 0x06: return "Background";
                case 0x07: return "Camera";
                case 0x08: return "Audio";
                case 0x09: return "Video";
                case 0x0A: return "Timer";
                case 0x0B: return "Input";
                default:   return "Other";
            }
        }

        private void RebuildBlockList()
        {
            m_BlockList.Clear();

            if (m_Blocks.Count == 0)
            {
                m_BlockList.Add(new HelpBox("This script is empty. Use Add block to begin.", HelpBoxMessageType.Info));
                return;
            }

            AddBlocks(m_BlockList, m_Blocks);
        }

        private void AddBlocks(VisualElement container, List<FieldScriptBlock> blocks)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                container.Add(BuildBlock(blocks[i], i, blocks));
            }
        }

        private VisualElement BuildBlock(FieldScriptBlock block, int index, List<FieldScriptBlock> owner)
        {
            VisualElement root = new VisualElement();
            root.style.marginBottom    = 4;
            root.style.paddingTop      = 4;
            root.style.paddingBottom   = 4;
            root.style.paddingLeft     = 6;
            root.style.paddingRight    = 6;
            root.style.borderLeftWidth = 3;
            root.style.borderLeftColor = block.IsRecognised ? new Color(0.35f, 0.55f, 0.85f) : new Color(0.85f, 0.55f, 0.2f);

            root.Add(BuildBlockHeader(block, index, owner));

            if (!block.IsRecognised)
            {
                root.Add(new HelpBox($"'{block.RawLine}' is not an opcode this engine has. It is left exactly as written.", HelpBoxMessageType.Warning));
                return root;
            }

            for (int i = 0; i < block.OpCode.Arguments.Count; i++)
            {
                root.Add(BuildArgumentField(block, i));
            }

            if (block.OpensBlock)
            {
                root.Add(BuildBranch(block.Children, "Add block inside"));
                root.Add(BuildElse(block));
            }

            return root;
        }

        private VisualElement BuildElse(FieldScriptBlock block)
        {
            VisualElement section = new VisualElement();

            if (!block.HasElse)
            {
                Button addElse = new Button(() =>
                                            {
                                                block.AddElse();
                                                RebuildBlockList();
                                                NotifyChanged();
                                            })
                                 {
                                     text = "Add else"
                                 };

                addElse.style.marginTop = 4;
                section.Add(addElse);

                return section;
            }

            VisualElement header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems    = Align.Center;
            header.style.marginTop     = 4;

            Label label = new Label(FieldScriptBlocks.ELSE_BLOCK);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.flexGrow                = 1;

            header.Add(label);
            header.Add(MakeSmallButton("✕", "Remove else", () => RemoveElse(block)));

            section.Add(header);
            section.Add(BuildBranch(block.ElseChildren, "Add block to else"));

            return section;
        }

        private void RemoveElse(FieldScriptBlock block)
        {
            if (block.ElseChildren.Count > 0)
            {
                bool confirmed = EditorUtility.DisplayDialog("Remove else", $"This removes the else and the {block.ElseChildren.Count} instruction(s) inside it.", "Remove", "Cancel");

                if (!confirmed)
                {
                    return;
                }
            }

            block.RemoveElse();

            RebuildBlockList();
            NotifyChanged();
        }

        private VisualElement BuildBranch(List<FieldScriptBlock> children, string addLabel)
        {
            VisualElement body = new VisualElement();
            body.style.marginLeft      = 12;
            body.style.marginTop       = 4;
            body.style.borderLeftWidth = 1;
            body.style.borderLeftColor = new Color(0.35f, 0.55f, 0.85f, 0.4f);
            body.style.paddingLeft     = 8;

            if (children.Count == 0)
            {
                Label emptyLabel = new Label("nothing yet");
                emptyLabel.style.opacity = 0.5f;

                body.Add(emptyLabel);
            }

            AddBlocks(body, children);

            Button addInside = new Button(() => ShowOpCodePicker(children)) { text = addLabel };
            addInside.style.marginTop = 4;

            body.Add(addInside);

            return body;
        }

        private VisualElement BuildBlockHeader(FieldScriptBlock block, int index, List<FieldScriptBlock> owner)
        {
            VisualElement header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems    = Align.Center;

            Label name = new Label(block.IsRecognised ? block.OpCode.ScriptName : "Unrecognised");
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.flexGrow                = 1;
            name.tooltip                       = block.IsRecognised ? block.OpCode.Summary : block.RawLine;

            header.Add(name);
            header.Add(MakeSmallButton("▲", "Move up",   () => Move(owner, index, -1)));
            header.Add(MakeSmallButton("▼", "Move down", () => Move(owner, index, 1)));
            header.Add(MakeSmallButton("✕", "Remove",    () => Remove(owner, index)));

            return header;
        }

        private static Button MakeSmallButton(string text, string tooltip, Action onClick)
        {
            Button button = new Button(onClick) { text = text, tooltip = tooltip };
            button.style.width        = 24;
            button.style.marginLeft   = 2;
            button.style.paddingLeft  = 0;
            button.style.paddingRight = 0;

            return button;
        }

        private VisualElement BuildArgumentField(FieldScriptBlock block, int argumentIndex)
        {
            FieldArgumentInfo argument = block.OpCode.Arguments[argumentIndex];

            string label = ObjectNames.NicifyVariableName(argument.Name);

            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;

            VisualElement control = BuildControl(block, argumentIndex, argument, label);
            control.style.flexGrow = 1;
            control.tooltip        = argument.Description ?? $"{argument.Type}";

            row.Add(control);

            if (ArgumentTypes.TryGetVariableWidth(argument.Type, argument.Width, out _))
            {
                row.Add(MakeSmallButton("▼", "Choose a variable", () => ShowVariablePicker(block, argumentIndex, argument)));
            }

            if (HasLiteralControl(argument) && IsVariableToken(block, argumentIndex))
            {
                row.Add(MakeSmallButton("#", "Use a value instead", () => Set(block, argumentIndex, FieldScriptBlock.DefaultFor(argument), true)));
            }

            return row;
        }

        private VisualElement BuildControl(FieldScriptBlock block, int argumentIndex, FieldArgumentInfo argument, string label)
        {
            string current = argumentIndex < block.Arguments.Count ? block.Arguments[argumentIndex] : string.Empty;

            if (HasLiteralControl(argument) && IsVariableToken(block, argumentIndex))
            {
                TextField variable = new TextField(label) { value = current };
                variable.RegisterValueChangedCallback(e => Set(block, argumentIndex, e.newValue));

                return variable;
            }

            switch (argument.Type)
            {
                case ArgumentType.Bool:
                case ArgumentType.Value when argument.Width == VariableWidth.Bool:
                {
                    Toggle toggle = new Toggle(label) { value = current == "true" };
                    toggle.RegisterValueChangedCallback(e => Set(block, argumentIndex, e.newValue ? "true" : "false"));
                    return toggle;
                }

                case ArgumentType.Float:
                {
                    FloatField field = new FloatField(label) { value = ParseFloat(current) };
                    field.RegisterValueChangedCallback(e => Set(block, argumentIndex, e.newValue.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                    return field;
                }

                case ArgumentType.Byte:
                case ArgumentType.UShort:
                case ArgumentType.Int:
                case ArgumentType.EntityId:
                case ArgumentType.EventId:
                case ArgumentType.Priority:
                {
                    IntegerField field = new IntegerField(label) { value = ParseInt(current) };
                    field.RegisterValueChangedCallback(e => Set(block, argumentIndex, e.newValue.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                    return field;
                }

                case ArgumentType.SpawnId:
                {
                    // The choices depend on the field named earlier in the same block, so the whole block
                    // is rebuilt when that changes and this is asked again.
                    List<SpawnChoice> spawns = GetSpawnChoices(block);

                    if (spawns.Count == 0)
                    {
                        // No field named yet, or that field's prefab has no spawn points. Typing a number
                        // beats an empty list.
                        IntegerField typed = new IntegerField(label) { value = ParseInt(current) };
                        typed.RegisterValueChangedCallback(e => Set(block, argumentIndex, e.newValue.ToString(System.Globalization.CultureInfo.InvariantCulture)));

                        return typed;
                    }

                    List<string> labels = new List<string>(spawns.Count);

                    foreach (SpawnChoice spawn in spawns)
                    {
                        labels.Add(spawn.Label);
                    }

                    DropdownField spawnField = new DropdownField(label, labels, 0);

                    // Show whichever spawn point the written id belongs to. An id nothing answers to is
                    // shown as the bare number rather than being quietly changed.
                    spawnField.SetValueWithoutNotify(LabelForSpawnId(spawns, current));

                    spawnField.RegisterValueChangedCallback(e =>
                                                            {
                                                                int chosen = labels.IndexOf(e.newValue);

                                                                Set(block, argumentIndex, spawns[chosen].Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
                                                            });

                    return spawnField;
                }

                case ArgumentType.FieldName:
                case ArgumentType.MusicName:
                case ArgumentType.SoundName:
                case ArgumentType.MusicNameHint:
                case ArgumentType.MusicStateName:
                {
                    // Offered as a list so a name cannot be mistyped. A field name is checked when the
                    // script compiles, but a music or sound name is not — it is hashed as written, and a
                    // typo becomes a missing key at play time. This is where that is prevented.
                    List<string> choices = GetNameChoices(block, argument.Type);

                    if (choices.Count == 0)
                    {
                        // Nothing in the project to offer yet, so let it be typed rather than showing an
                        // empty list that cannot be used.
                        TextField typed = new TextField(label) { value = current };
                        typed.RegisterValueChangedCallback(e => Set(block, argumentIndex, e.newValue));

                        return typed;
                    }

                    DropdownField field = new DropdownField(label, choices, 0);

                    // Whatever is already written stays shown, even when nothing answers to it any more.
                    // Opening a script must not quietly repoint it at the first name in the list.
                    field.SetValueWithoutNotify(current);

                    // Naming a field decides which spawn points exist, and naming a track decides which
                    // stem states exist, so changing either rebuilds the block to refresh the list beside
                    // it.
                    bool rebuildOnChange = argument.Type == ArgumentType.FieldName ||
                                           argument.Type == ArgumentType.MusicName ||
                                           argument.Type == ArgumentType.MusicNameHint;

                    field.RegisterValueChangedCallback(e => Set(block, argumentIndex, e.newValue, rebuildOnChange));

                    return field;
                }

                case ArgumentType.Comparison:
                {
                    // Offered as a list rather than typed, so "==" cannot be misspelled and the bit tests
                    // are discoverable instead of having to be known.
                    List<string> choices = new List<string>();

                    // A comparison takes its width from the values either side of it.
                    VariableWidth comparedWidth = block.OpCode.Arguments[0].Width;

                    foreach (ScriptComparison comparison in Enum.GetValues(typeof(ScriptComparison)))
                    {
                        bool offered = comparedWidth switch
                                       {
                                           VariableWidth.Bool  => comparison == ScriptComparison.Equal || comparison == ScriptComparison.NotEqual,
                                           VariableWidth.Float => !comparison.IsBitTest(),
                                           _                   => true
                                       };

                        if (!offered)
                        {
                            continue;
                        }

                        choices.Add(comparison.ToScriptText());
                    }

                    int selected = Mathf.Max(0, choices.IndexOf(current));

                    DropdownField field = new DropdownField(label, choices, selected);
                    field.RegisterValueChangedCallback(e => Set(block, argumentIndex, e.newValue));
                    return field;
                }

                default:
                {
                    // Variables, localisation keys, field names and key lists are all authored as text.
                    TextField field = new TextField(label) { value = current };
                    field.RegisterValueChangedCallback(e => Set(block, argumentIndex, e.newValue));
                    return field;
                }
            }
        }

        private static bool HasLiteralControl(FieldArgumentInfo argument)
        {
            bool hasLiteralControl = ArgumentTypes.TakesSource(argument.Type) ||
                                     (argument.Type == ArgumentType.Value && argument.Width == VariableWidth.Bool);

            return hasLiteralControl;
        }

        private static bool IsVariableToken(FieldScriptBlock block, int argumentIndex)
        {
            bool isVariable = argumentIndex < block.Arguments.Count && block.Arguments[argumentIndex].StartsWith("$");

            return isVariable;
        }

        /// <summary>
        /// Offer the variables the map declares that this argument can take: a destination's exact width, or for
        /// a value, any width it can read.
        /// </summary>
        private void ShowVariablePicker(FieldScriptBlock block, int argumentIndex, FieldArgumentInfo argument)
        {
            if (m_VariableMap == null)
            {
                m_VariableMap = FindVariableMap();
            }

            GenericMenu menu = new GenericMenu();

            if (m_VariableMap == null)
            {
                menu.AddDisabledItem(new GUIContent("No VariableMapAsset in the project"));
                menu.ShowAsContext();
                return;
            }

            ArgumentTypes.TryGetVariableWidth(argument.Type, argument.Width, out VariableWidth width);

            int offered = 0;

            foreach (VariableDefinition variable in m_VariableMap.Variables)
            {
                bool fits = argument.Type == ArgumentType.Value ? ArgumentTypes.CanRead(width, variable.Width) : variable.Width == width;

                if (!fits)
                {
                    continue;
                }

                string label = $"{variable.Bank}/{variable.Name}";
                string token = "$" + variable.Name;

                menu.AddItem(new GUIContent(label), false, () => Set(block, argumentIndex, token, true));
                offered++;
            }

            if (offered == 0)
            {
                menu.AddDisabledItem(new GUIContent($"No {width} variables declared"));
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// The names this kind of argument can hold. Audio names come from the providers rather than from
        /// every asset of that type in the project, because the provider's list is what the runtime can
        /// actually resolve — an asset nobody wired in would be offered and then fail.
        /// </summary>
        private List<string> GetNameChoices(FieldScriptBlock block, ArgumentType type)
        {
            switch (type)
            {
                case ArgumentType.FieldName:
                    return m_FieldNames ??= GatherFieldNames();

                case ArgumentType.MusicName:
                    return m_MusicNames ??= GatherProviderNames<MusicAssetProvider>(provider => provider.AssetNames);

                case ArgumentType.SoundName:
                    return m_SoundNames ??= GatherProviderNames<SfxAssetProvider>(provider => provider.AssetNames);

                case ArgumentType.MusicNameHint:
                    return m_MusicNames ??= GatherProviderNames<MusicAssetProvider>(provider => provider.AssetNames);

                case ArgumentType.MusicStateName:
                    return GetMusicStateChoices(block);

                default:
                    return new List<string>();
            }
        }

        /// <summary>One spawn point a field offers: what the author sees, and the id that gets written.</summary>
        private readonly struct SpawnChoice
        {
            internal readonly string Label;
            internal readonly int    Id;

            internal SpawnChoice(string label, int id)
            {
                Label = label;
                Id    = id;
            }
        }

        /// <summary>
        /// The spawn points in the field this block jumps to.<br /><br />
        /// The id is what the bytecode carries, but an id is not something anyone should be picking by
        /// hand, so the author chooses a game object and the id comes with it. The label carries the id
        /// too: two spawn points can share a name, and seeing what was written matters.
        /// </summary>
        private List<SpawnChoice> GetSpawnChoices(FieldScriptBlock block)
        {
            string fieldName = FieldNameArgumentOf(block);

            if (string.IsNullOrEmpty(fieldName))
            {
                return new List<SpawnChoice>();
            }

            if (m_SpawnChoices.TryGetValue(fieldName, out List<SpawnChoice> cached))
            {
                return cached;
            }

            List<SpawnChoice> choices = GatherSpawnChoices(fieldName);

            m_SpawnChoices[fieldName] = choices;

            return choices;
        }

        /// <summary>
        /// The field name earlier in the same block. A block with no field argument — nothing does today,
        /// but an opcode could — has no field to read spawn points from.
        /// </summary>
        private static string FieldNameArgumentOf(FieldScriptBlock block)
        {
            for (int i = 0; i < block.OpCode.Arguments.Count && i < block.Arguments.Count; i++)
            {
                if (block.OpCode.Arguments[i].Type == ArgumentType.FieldName)
                {
                    return block.Arguments[i];
                }
            }

            return string.Empty;
        }

        private static List<SpawnChoice> GatherSpawnChoices(string fieldName)
        {
            List<SpawnChoice> choices = new List<SpawnChoice>();

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(FieldDesignerData)))
            {
                FieldDesignerData designerData = AssetDatabase.LoadAssetAtPath<FieldDesignerData>(AssetDatabase.GUIDToAssetPath(guid));

                if (designerData == null)
                {
                    continue;
                }

                foreach (FieldDatabaseAssetAuthoring field in designerData.FieldDatabase.Fields)
                {
                    if (field.Prefab == null || field.Prefab.name != fieldName)
                    {
                        continue;
                    }

                    // Inactive ones included: a spawn point is a marker, and one parked under a disabled
                    // object is still somewhere a script can send the player.
                    foreach (SpawnPoint spawnPoint in field.Prefab.GetComponentsInChildren<SpawnPoint>(true))
                    {
                        choices.Add(new SpawnChoice($"{spawnPoint.gameObject.name} ({spawnPoint.Id})", spawnPoint.Id));
                    }

                    return choices;
                }
            }

            return choices;
        }

        private static string LabelForSpawnId(List<SpawnChoice> spawns, string written)
        {
            foreach (SpawnChoice spawn in spawns)
            {
                if (spawn.Id.ToString(System.Globalization.CultureInfo.InvariantCulture) == written)
                {
                    return spawn.Label;
                }
            }

            return written;
        }

        /// <summary>
        /// The stem states to offer for this block.<br /><br />
        /// Narrowed to the track the block names, which is the whole reason <c>MUSIC_STEM_STATE</c> carries
        /// a track it never compiles. With no track named, or one nothing answers to, the only honest
        /// answer is every state across every track — which will include states the track that ends up
        /// playing does not have.
        /// </summary>
        private List<string> GetMusicStateChoices(FieldScriptBlock block)
        {
            string trackName = MusicTrackArgumentOf(block);

            if (string.IsNullOrEmpty(trackName))
            {
                return m_MusicStateNames ??= GatherProviderNames<MusicAssetProvider>(provider => provider.StemStateNames);
            }

            if (m_StateNamesByTrack.TryGetValue(trackName, out List<string> cached))
            {
                return cached;
            }

            List<string> states = GatherProviderNames<MusicAssetProvider>(provider => provider.StemStateNamesOf(trackName));

            if (states.Count == 0)
            {
                states = m_MusicStateNames ??= GatherProviderNames<MusicAssetProvider>(provider => provider.StemStateNames);
            }

            m_StateNamesByTrack[trackName] = states;

            return states;
        }

        /// <summary>The track this block names, whether it compiles that name or only offers it.</summary>
        private static string MusicTrackArgumentOf(FieldScriptBlock block)
        {
            for (int i = 0; i < block.OpCode.Arguments.Count && i < block.Arguments.Count; i++)
            {
                ArgumentType type = block.OpCode.Arguments[i].Type;

                if (type == ArgumentType.MusicName || type == ArgumentType.MusicNameHint)
                {
                    return block.Arguments[i];
                }
            }

            return string.Empty;
        }

        private static List<string> GatherFieldNames()
        {
            List<string> names = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(FieldDesignerData)))
            {
                FieldDesignerData designerData = AssetDatabase.LoadAssetAtPath<FieldDesignerData>(AssetDatabase.GUIDToAssetPath(guid));

                if (designerData == null)
                {
                    continue;
                }

                foreach (FieldDatabaseAssetAuthoring field in designerData.FieldDatabase.Fields)
                {
                    if (field.Prefab == null || names.Contains(field.Prefab.name))
                    {
                        continue;
                    }

                    names.Add(field.Prefab.name);
                }
            }

            names.Sort(StringComparer.Ordinal);

            return names;
        }

        private static List<string> GatherProviderNames<T>(Func<T, IEnumerable<string>> namesOf) where T : ScriptableObject
        {
            List<string> names = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                T provider = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));

                if (provider == null)
                {
                    continue;
                }

                foreach (string name in namesOf(provider))
                {
                    if (names.Contains(name))
                    {
                        continue;
                    }

                    names.Add(name);
                }
            }

            names.Sort(StringComparer.Ordinal);

            return names;
        }

        private static VariableMapAsset FindVariableMap()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(VariableMapAsset)))
            {
                VariableMapAsset map = AssetDatabase.LoadAssetAtPath<VariableMapAsset>(AssetDatabase.GUIDToAssetPath(guid));

                if (map != null)
                {
                    return map;
                }
            }

            VariableMapAsset none = null;

            return none;
        }

        private void Set(FieldScriptBlock block, int argumentIndex, string value, bool rebuild = false)
        {
            while (block.Arguments.Count <= argumentIndex)
            {
                block.Arguments.Add(string.Empty);
            }

            block.Arguments[argumentIndex] = value;

            if (rebuild)
            {
                RebuildBlockList();
            }

            NotifyChanged();
        }

        private void Move(List<FieldScriptBlock> owner, int index, int direction)
        {
            int target = index + direction;

            if (target < 0 || target >= owner.Count)
            {
                return;
            }

            (owner[index], owner[target]) = (owner[target], owner[index]);

            RebuildBlockList();
            NotifyChanged();
        }

        private void Remove(List<FieldScriptBlock> owner, int index)
        {
            // Removing a block that opens one takes its body with it, which is what the nesting shows,
            // so it is worth being asked first.
            int inside = owner[index].OpensBlock ? owner[index].Children.Count + (owner[index].ElseChildren?.Count ?? 0) : 0;

            if (inside > 0)
            {
                bool confirmed = EditorUtility.DisplayDialog("Remove block",
                                                             $"This removes the {owner[index].OpCode.ScriptName} and the {inside} instruction(s) inside it.",
                                                             "Remove", "Cancel");

                if (!confirmed)
                {
                    return;
                }
            }

            owner.RemoveAt(index);

            RebuildBlockList();
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            m_OnChanged?.Invoke(FieldScriptBlocks.ToScriptText(m_Blocks));
        }

        private static int ParseInt(string text)
        {
            int.TryParse(text, out int value);

            return value;
        }

        private static float ParseFloat(string text)
        {
            float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value);

            return value;
        }
    }
}