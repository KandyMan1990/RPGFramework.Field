using System;
using System.Collections.Generic;
using RPGFramework.Core.Dialogue;
using RPGFramework.Core.Memory;
using RPGFramework.Audio.Music;
using RPGFramework.Audio.Sfx;
using RPGFramework.Localisation.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.UIElements;
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
    internal sealed class FieldScriptBlockEditor : VisualElement
    {
        private readonly List<FieldScriptBlock>                m_Blocks;
        private readonly VisualElement                         m_BlockList;
        private readonly Action<string>                        m_OnChanged;
        private readonly FieldEntities                         m_Field;
        private readonly FieldEntityRecord                     m_Entity;
        private readonly IReadOnlyList<LocalisationSheetAsset> m_Sheets;
        private readonly Dictionary<string, List<SpawnChoice>> m_SpawnChoices      = new Dictionary<string, List<SpawnChoice>>();
        private readonly Dictionary<string, List<string>>      m_StateNamesByTrack = new Dictionary<string, List<string>>();

        private List<string>     m_DialogueKeys;
        private List<string>     m_AnimationNames;
        private VariableMapAsset m_VariableMap;
        private List<string>     m_FieldNames;
        private List<string>     m_MusicNames;
        private List<string>     m_SoundNames;
        private List<string>     m_MusicStateNames;

        public FieldScriptBlockEditor(string scriptText, FieldEntities field, FieldEntityRecord entity, IReadOnlyList<LocalisationSheetAsset> sheets, Action<string> onChanged)
        {
            m_OnChanged = onChanged;
            m_Field     = field;
            m_Entity    = entity;
            m_Sheets    = sheets;
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

            MenuButton addButton = new MenuButton(menu => FillOpCodeMenu(menu, m_Blocks)) { text = "Add block" };
            addButton.style.flexGrow = 1;

            toolbar.Add(addButton);

            return toolbar;
        }

        /// <summary>
        /// Every opcode that can actually run, grouped the way the enum groups them.
        /// </summary>
        private void FillOpCodeMenu(DropdownMenu menu, List<FieldScriptBlock> target)
        {
            menu.AppendAction($"Flow/{FieldScriptBlocks.LABEL_BLOCK}  —  Mark a place a jump can go to", _ =>
                                                                                                         {
                                                                                                             target.Add(FieldScriptBlock.CreateLabel(NextLabelName()));
                                                                                                             RebuildBlockList();
                                                                                                             NotifyChanged();
                                                                                                         });

            foreach (FieldOpCodeInfo opCode in FieldOpCodeCatalogue.All)
            {
                string category = GetCategory(opCode.OpCode);
                string label = string.IsNullOrEmpty(opCode.Summary)
                                   ? $"{category}/{opCode.ScriptName}"
                                   : $"{category}/{opCode.ScriptName}  —  {opCode.Summary}";

                FieldOpCodeInfo captured = opCode;

                menu.AppendAction(label, _ =>
                                         {
                                             target.Add(WithFirstLabel(FieldScriptBlock.CreateEmpty(captured)));
                                             RebuildBlockList();
                                             NotifyChanged();
                                         });
            }
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
            root.style.borderLeftColor = block.IsLabel      ? new Color(0.55f, 0.75f, 0.45f) :
                                         block.IsRecognised ? new Color(0.35f, 0.55f, 0.85f) : new Color(0.85f, 0.55f, 0.2f);

            root.Add(BuildBlockHeader(block, index, owner));

            if (block.IsLabel)
            {
                root.Add(BuildLabelName(block));
                return root;
            }

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

            MenuButton addInside = new MenuButton(menu => FillOpCodeMenu(menu, children)) { text = addLabel };
            addInside.style.marginTop = 4;

            body.Add(addInside);

            return body;
        }

        private VisualElement BuildBlockHeader(FieldScriptBlock block, int index, List<FieldScriptBlock> owner)
        {
            VisualElement header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems    = Align.Center;

            Label name = new Label(block.IsLabel ? FieldScriptBlocks.LABEL_BLOCK : block.IsRecognised ? block.OpCode.ScriptName : "Unrecognised");
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.flexGrow                = 1;
            name.tooltip                       = block.IsLabel ? "A place a jump can go to. Emits nothing" : block.IsRecognised ? block.OpCode.Summary : block.RawLine;

            header.Add(name);
            header.Add(MakeSmallButton("▲", "Move up",   () => Move(owner, index, -1)));
            header.Add(MakeSmallButton("▼", "Move down", () => Move(owner, index, 1)));
            header.Add(MakeSmallButton("✕", "Remove",    () => Remove(owner, index)));

            return header;
        }

        private static Button MakeSmallButton(string text, string tooltip, Action onClick)
        {
            Button button = MakeSmall(new Button(onClick), text, tooltip);

            return button;
        }

        private static Button MakeSmall(Button button, string text, string tooltip)
        {
            button.text               = text;
            button.tooltip            = tooltip;
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
                row.Add(MakeSmall(new MenuButton(menu => FillVariableMenu(menu, block, argumentIndex, argument)), "▼", "Choose a variable"));
            }

            if (HasLiteralControl(argument) && IsVariableToken(block, argumentIndex))
            {
                row.Add(MakeSmallButton("#", "Use a value instead", () => Set(block, argumentIndex, FieldScriptBlock.DefaultFor(argument), true)));
            }

            return row;
        }

        /// <summary>
        /// The label's name, changed on Enter or when the field loses focus rather than on every key, since a rename
        /// rebuilds the blocks. Every jump that went to the old name goes to the new one, so renaming cannot break them.
        /// </summary>
        private VisualElement BuildLabelName(FieldScriptBlock block)
        {
            TextField name = new TextField("Name") { value = block.LabelName, isDelayed = true };
            name.tooltip = "Starts with a letter; letters, digits and underscores";

            name.RegisterValueChangedCallback(e =>
                                              {
                                                  if (!FieldScriptCompiler.IsLabelName(e.newValue))
                                                  {
                                                      name.SetValueWithoutNotify(e.previousValue);
                                                      return;
                                                  }

                                                  RenameJumps(m_Blocks, block.LabelName, e.newValue);
                                                  block.RenameLabel(e.newValue);

                                                  RebuildBlockList();
                                                  NotifyChanged();
                                              });

            return name;
        }

        private static void RenameJumps(List<FieldScriptBlock> blocks, string from, string to)
        {
            foreach (FieldScriptBlock block in blocks)
            {
                if (block.IsRecognised)
                {
                    for (int i = 0; i < block.OpCode.Arguments.Count && i < block.Arguments.Count; i++)
                    {
                        if (block.OpCode.Arguments[i].Type == ArgumentType.Label && block.Arguments[i] == from)
                        {
                            block.Arguments[i] = to;
                        }
                    }
                }

                if (block.Children != null)
                {
                    RenameJumps(block.Children, from, to);
                }

                if (block.ElseChildren != null)
                {
                    RenameJumps(block.ElseChildren, from, to);
                }
            }
        }

        /// <summary>
        /// The labels in this script, including those inside IF blocks: a jump can go anywhere in its own script.
        /// </summary>
        private List<string> LabelNames()
        {
            List<string> names = new List<string>();

            CollectLabelNames(m_Blocks, names);

            return names;
        }

        private static void CollectLabelNames(List<FieldScriptBlock> blocks, List<string> names)
        {
            foreach (FieldScriptBlock block in blocks)
            {
                if (block.IsLabel)
                {
                    names.Add(block.LabelName);
                }

                if (block.Children != null)
                {
                    CollectLabelNames(block.Children, names);
                }

                if (block.ElseChildren != null)
                {
                    CollectLabelNames(block.ElseChildren, names);
                }
            }
        }

        private string NextLabelName()
        {
            List<string> taken = LabelNames();

            int    number = taken.Count + 1;
            string next   = $"Label{number}";

            while (taken.Contains(next))
            {
                number++;
                next = $"Label{number}";
            }

            return next;
        }

        private FieldScriptBlock WithFirstLabel(FieldScriptBlock block)
        {
            List<string> labels = LabelNames();

            for (int i = 0; labels.Count > 0 && i < block.OpCode.Arguments.Count; i++)
            {
                if (block.OpCode.Arguments[i].Type == ArgumentType.Label)
                {
                    block.Arguments[i] = labels[0];
                }
            }

            return block;
        }

        /// <summary>
        /// The labels in this script. With none there is nowhere to go yet, which is said rather than offering an
        /// empty list.
        /// </summary>
        private VisualElement LabelChoice(FieldScriptBlock block, int argumentIndex, string label, string current)
        {
            List<string> labels = LabelNames();

            if (labels.Count == 0)
            {
                HelpBox none = new HelpBox("No LABEL in this script yet. Add one where this should go, then choose it here.", HelpBoxMessageType.Info);

                return none;
            }

            DropdownField field = new DropdownField(label, labels, 0);

            // Whatever is written stays shown, even a label that no longer exists, rather than quietly becoming the first.
            field.SetValueWithoutNotify(current);
            field.RegisterValueChangedCallback(e => Set(block, argumentIndex, e.newValue));

            return field;
        }

        /// <summary>
        /// A number an enum names, offered by those names and written as the number. A <see cref="FlagsAttribute" />
        /// enum is offered as a mask.
        /// </summary>
        private VisualElement EnumChoice(FieldScriptBlock block, int argumentIndex, Type enumType, string label, string current)
        {
            long.TryParse(current, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out long number);

            Enum value = (Enum)Enum.ToObject(enumType, number);

            BaseField<Enum> field = enumType.IsDefined(typeof(FlagsAttribute), false) ? new EnumFlagsField(label, value) : new EnumField(label, value);

            field.RegisterValueChangedCallback(e => Set(block, argumentIndex, Convert.ToInt64(e.newValue).ToString(System.Globalization.CultureInfo.InvariantCulture)));

            return field;
        }

        /// <summary>
        /// The field's entities by name, writing the id. Choosing one rebuilds the block, since which scripts it offers
        /// for an event beside it depends on which entity is named.
        /// </summary>
        private DropdownField EntityDropdown(FieldScriptBlock block, int argumentIndex, string label, string current)
        {
            List<string> labels = new List<string>(m_Field.Entities.Count);
            string       shown  = current;

            foreach (FieldEntityRecord entity in m_Field.Entities)
            {
                string entityLabel = $"{entity.EntityId}: {entity.Name}";

                labels.Add(entityLabel);

                if (entity.EntityId.ToString(System.Globalization.CultureInfo.InvariantCulture) == current)
                {
                    shown = entityLabel;
                }
            }

            DropdownField field = new DropdownField(label, labels, 0);
            field.SetValueWithoutNotify(shown);

            field.RegisterValueChangedCallback(e =>
                                               {
                                                   FieldEntityRecord chosen = m_Field.Entities[labels.IndexOf(e.newValue)];
                                                   Set(block, argumentIndex, chosen.EntityId.ToString(System.Globalization.CultureInfo.InvariantCulture), true);
                                               });

            return field;
        }

        /// <summary>
        /// Whose scripts an event id counts: the entity the block names, or with none named this entity's own, as
        /// <c>RETURN_TO_SCRIPT</c>'s. A named entity written as a variable, or one the field no longer has, cannot be
        /// known here.
        /// </summary>
        private bool TryGetEventOwner(FieldScriptBlock block, out FieldEntityRecord owner)
        {
            owner = m_Entity;

            for (int i = 0; i < block.OpCode.Arguments.Count; i++)
            {
                if (block.OpCode.Arguments[i].Type != ArgumentType.EntityId)
                {
                    continue;
                }

                string named = i < block.Arguments.Count ? block.Arguments[i] : string.Empty;
                owner = m_Field.Entities.Find(entity => entity.EntityId.ToString(System.Globalization.CultureInfo.InvariantCulture) == named);
                break;
            }

            bool hasOwner = owner != null;

            return hasOwner;
        }

        private static string DescribeScript(FieldScriptRecord script, int eventId)
        {
            string description = string.IsNullOrEmpty(script.Name) ? $"{eventId}: {script.Type}" : $"{eventId}: {script.Name} ({script.Type})";

            return description;
        }

        /// <summary>
        /// A choice of 0 to <paramref name="count" /> - 1, labelled, writing the number. A number outside the range is
        /// shown as written rather than quietly changed.
        /// </summary>
        private DropdownField IndexDropdown(FieldScriptBlock block, int argumentIndex, string label, string current, int count, Func<int, string> describe)
        {
            List<string> labels = new List<string>(count);

            for (int i = 0; i < count; i++)
            {
                labels.Add(describe(i));
            }

            DropdownField field = new DropdownField(label, labels, 0);

            bool known = int.TryParse(current, out int index) && index >= 0 && index < count;
            field.SetValueWithoutNotify(known ? labels[index] : current);

            field.RegisterValueChangedCallback(e => Set(block, argumentIndex, labels.IndexOf(e.newValue).ToString(System.Globalization.CultureInfo.InvariantCulture)));

            return field;
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

            if (argument.EnumType != null)
            {
                return EnumChoice(block, argumentIndex, argument.EnumType, label, current);
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

                case ArgumentType.EntityId:
                    return EntityDropdown(block, argumentIndex, label, current);

                case ArgumentType.EventId when TryGetEventOwner(block, out FieldEntityRecord owner):
                    return IndexDropdown(block, argumentIndex, label, current, owner.Scripts.Count, i => DescribeScript(owner.Scripts[i], i));

                case ArgumentType.Byte:
                case ArgumentType.UShort:
                case ArgumentType.Int:
                case ArgumentType.EventId:
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
                case ArgumentType.AnimationName:
                case ArgumentType.LocalisationKey:
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

                case ArgumentType.LocalisationKeyList:
                    return KeyList(block, argumentIndex, label, current);

                case ArgumentType.Label:
                    return LabelChoice(block, argumentIndex, label, current);

                case ArgumentType.Priority:
                    return IndexDropdown(block, argumentIndex, label, current, Enum.GetValues(typeof(FieldScriptPriority)).Length, i => $"{i}: {(FieldScriptPriority)i}");

                case ArgumentType.DialogueChannel:
                    return IndexDropdown(block, argumentIndex, label, current, ArgumentTypes.DIALOGUE_CHANNEL_COUNT, i => $"Channel {i}");

                case ArgumentType.MessageVariableSlot:
                    return IndexDropdown(block, argumentIndex, label, current, DialogueMarkup.MESSAGE_VARIABLE_COUNT, i => $"{{Var {i}}}");

                case ArgumentType.DialogueWindowStyle:
                {
                    List<string> styles = new List<string>(Enum.GetNames(typeof(DialogueWindowStyle)));

                    DropdownField field = new DropdownField(label, styles, Mathf.Max(0, styles.IndexOf(current)));
                    field.RegisterValueChangedCallback(e => Set(block, argumentIndex, e.newValue));
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
        private void FillVariableMenu(DropdownMenu menu, FieldScriptBlock block, int argumentIndex, FieldArgumentInfo argument)
        {
            if (m_VariableMap == null)
            {
                m_VariableMap = FindVariableMap();
            }

            if (m_VariableMap == null)
            {
                menu.AppendAction("No VariableMapAsset in the project", null, DropdownMenuAction.Status.Disabled);
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

                menu.AppendAction(label, _ => Set(block, argumentIndex, token, true));
                offered++;
            }

            if (offered == 0)
            {
                menu.AppendAction($"No {width} variables declared", null, DropdownMenuAction.Status.Disabled);
            }
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

                case ArgumentType.AnimationName:
                    return m_AnimationNames ??= GatherAnimationNames();

                case ArgumentType.LocalisationKey:
                    return m_DialogueKeys ??= GatherDialogueKeys();

                default:
                    return new List<string>();
            }
        }

        /// <summary>
        /// The keys of the sheets this field loads, which are the only ones its scripts can show. Read from the sheet
        /// assets, where generation records them; a sheet generated before that was recorded offers nothing until it
        /// is generated again.
        /// </summary>
        private List<string> GatherDialogueKeys()
        {
            List<string> keys = new List<string>();

            foreach (LocalisationSheetAsset sheet in m_Sheets)
            {
                if (sheet != null)
                {
                    keys.AddRange(sheet.Keys);
                }
            }

            return keys;
        }

        /// <summary>
        /// A key per row, for a list that runs to the end of the line — the answers to a choice. The script text keeps
        /// them space separated in one argument, which is how the compiler reads them.
        /// </summary>
        private VisualElement KeyList(FieldScriptBlock block, int argumentIndex, string label, string current)
        {
            List<string> keys    = new List<string>(current.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            List<string> choices = m_DialogueKeys ??= GatherDialogueKeys();

            VisualElement list = new VisualElement();
            list.Add(new Label(label));

            void Write() => Set(block, argumentIndex, string.Join(" ", keys), true);

            for (int i = 0; i < keys.Count; i++)
            {
                int index = i;

                VisualElement row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

                VisualElement key;

                if (choices.Count == 0)
                {
                    TextField typed = new TextField { value = keys[index] };
                    typed.RegisterValueChangedCallback(e =>
                                                       {
                                                           keys[index] = e.newValue;
                                                           Set(block, argumentIndex, string.Join(" ", keys));
                                                       });
                    key = typed;
                }
                else
                {
                    DropdownField chosen = new DropdownField(choices, 0);
                    chosen.SetValueWithoutNotify(keys[index]);
                    chosen.RegisterValueChangedCallback(e =>
                                                        {
                                                            keys[index] = e.newValue;
                                                            Set(block, argumentIndex, string.Join(" ", keys));
                                                        });
                    key = chosen;
                }

                key.style.flexGrow = 1;
                row.Add(key);
                row.Add(MakeSmallButton("−", "Remove this answer", () =>
                                                                   {
                                                                       keys.RemoveAt(index);
                                                                       Write();
                                                                   }));
                list.Add(row);
            }

            list.Add(new Button(() =>
                                {
                                    keys.Add(choices.Count > 0 ? choices[0] : "Sheet/Key");
                                    Write();
                                })
                     {
                         text = "Add answer"
                     });

            return list;
        }

        /// <summary>
        /// Every state in the entity's Animator controller, sub-state machines included. Offered rather than typed so
        /// a name cannot be mistyped — it is hashed into the bytecode, where a typo is indistinguishable from a state
        /// that does not exist. A name with a space in it is left out: arguments are read one word at a time, so it
        /// could not be written down.
        /// </summary>
        private List<string> GatherAnimationNames()
        {
            List<string> animationNames = new List<string>();

            Animator animator = m_Entity.Body == null ? null : m_Entity.Body.GetComponentInChildren<Animator>(true);

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return animationNames;
            }

            RuntimeAnimatorController runtime = animator.runtimeAnimatorController;

            if (runtime is AnimatorOverrideController overrideController)
            {
                runtime = overrideController.runtimeAnimatorController;
            }

            if (runtime is not AnimatorController controller)
            {
                return animationNames;
            }

            foreach (AnimatorControllerLayer layer in controller.layers)
            {
                CollectAnimationNames(layer.stateMachine, animationNames);
            }

            animationNames.Sort(StringComparer.Ordinal);

            return animationNames;
        }

        private static void CollectAnimationNames(AnimatorStateMachine stateMachine, List<string> animationNames)
        {
            foreach (ChildAnimatorState state in stateMachine.states)
            {
                if (!state.state.name.Contains(' '))
                {
                    animationNames.Add(state.state.name);
                }
            }

            foreach (ChildAnimatorStateMachine child in stateMachine.stateMachines)
            {
                CollectAnimationNames(child.stateMachine, animationNames);
            }
        }

        /// <summary>
        /// A button that opens a menu beneath itself, filled on each click so it lists what exists at that moment.
        /// </summary>
        private sealed class MenuButton : Button, IToolbarMenuElement
        {
            private readonly DropdownMenu         m_Menu = new DropdownMenu();
            private readonly Action<DropdownMenu> m_Fill;

            DropdownMenu IToolbarMenuElement.menu => m_Menu;

            internal MenuButton(Action<DropdownMenu> fill)
            {
                m_Fill  =  fill;
                clicked += Open;
            }

            private void Open()
            {
                m_Menu.ClearItems();
                m_Fill(m_Menu);
                this.ShowMenu();
            }
        }

        /// <summary>One spawn point a field offers: what the author sees, and the id that gets written.</summary>
        internal readonly struct SpawnChoice
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

        internal static List<SpawnChoice> GatherSpawnChoices(string fieldName)
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

        internal static List<string> GatherFieldNames()
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