using System;
using System.Collections.Generic;
using System.Linq;
using RPGFramework.Localisation.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace RPGFramework.Field.Editor
{
    internal class FieldDesignerWindow : EditorWindow
    {
        /// <summary>A new script returns at once, so it compiles before anything has been written in it.</summary>
        private const string NEW_SCRIPT_TEXT = "RETURN";

        private const string NO_BODY = "(none)";

        [SerializeField]
        private VisualTreeAsset m_Uxml;

        private FieldDesignerData            m_FieldDesignerData;
        private FieldDatabaseAssetAuthoring  m_CurrentFieldAsset;
        private List<LocalisationSheetAsset> m_LocalisationSheetAssets;

        private ModalWindow   m_Window;
        private ListView      m_FieldsContainerListView;
        private VisualElement m_PrefabViewer;
        private VisualElement m_TextViewer;
        private VisualElement m_ScriptsViewer;
        private VisualElement m_EncountersViewer;
        private ObjectField   m_PrefabObjectField;
        private Label         m_CurrentFieldLabel;
        private ListView      m_TextViewerListView;
        private ListView      m_EntityListView;
        private ListView      m_EntityScriptListView;
        private Button        m_AddEntityButton;
        private Button        m_AddScriptButton;
        private Button        m_DeleteEntityButton;
        private Button        m_DeleteScriptButton;
        private VisualElement m_ScriptBlockContainer;

        private FieldEntityRecord m_SelectedEntity;
        private FieldScriptRecord m_SelectedScript;
        private HelpBox           m_ScriptStatus;

        private GameObject    m_CurrentlyOpenPrefab;
        private string        m_CurrentlyOpenPrefabPath;
        private FieldEntities m_OpenFieldEntities;
        private bool          m_HasUnsavedChanges;

        [MenuItem("RPG Framework/Field Designer Window")]
        public static void ShowWindow()
        {
            GetWindow<FieldDesignerWindow>();
        }

        private void OnEnable()
        {
            m_FieldDesignerData = FieldDesignerDataUtility.GetOrCreate();
        }

        private void OnDestroy()
        {
            CloseOpenPrefab();
        }

        /// <summary>
        /// The field's prefab is edited as a loaded copy, which is only written back by saving — so closing it with
        /// changes asks first rather than dropping them.
        /// </summary>
        private void CloseOpenPrefab()
        {
            if (m_CurrentlyOpenPrefab == null)
            {
                return;
            }

            if (m_HasUnsavedChanges && EditorUtility.DisplayDialog("Unsaved script changes", $"Save the changes to {m_CurrentlyOpenPrefab.name}'s scripts?", "Save", "Discard"))
            {
                SaveOpenPrefab();
            }

            PrefabUtility.UnloadPrefabContents(m_CurrentlyOpenPrefab);

            m_CurrentlyOpenPrefab = null;
            m_OpenFieldEntities   = null;
            m_HasUnsavedChanges   = false;
        }

        private void SaveOpenPrefab()
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();

            // Both would write the same asset, and whichever saved last would silently undo the other.
            if (stage != null && stage.assetPath == m_CurrentlyOpenPrefabPath)
            {
                EditorUtility.DisplayDialog("Field is open in the prefab view", $"{m_CurrentlyOpenPrefab.name} is also open in the prefab view. Save or close it there first, then save here.", "OK");
                return;
            }

            PrefabUtility.SaveAsPrefabAsset(m_CurrentlyOpenPrefab, m_CurrentlyOpenPrefabPath);

            m_HasUnsavedChanges = false;
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();

            m_Uxml.CloneTree(rootVisualElement);

            InitWindow();
        }

        private void InitWindow()
        {
            Button saveButton = rootVisualElement.Q<Button>("SaveButton");
            saveButton.RegisterCallback<ClickEvent>(_ => m_FieldDesignerData.Save());

            Button addFieldButton = rootVisualElement.Q<Button>("AddFieldButton");
            addFieldButton.RegisterCallback<ClickEvent>(AddFieldButtonCallback);

            Button deleteFieldButton = rootVisualElement.Q<Button>("DeleteFieldButton");
            deleteFieldButton.RegisterCallback<ClickEvent>(DeleteFieldButtonCallback);

            Button prefabButton = rootVisualElement.Q<Button>("PrefabButton");
            prefabButton.RegisterCallback<ClickEvent>(OnPrefabButtonPressed);

            Button textButton = rootVisualElement.Q<Button>("TextButton");
            textButton.RegisterCallback<ClickEvent>(OnTextButtonPressed);

            Button scriptsButton = rootVisualElement.Q<Button>("ScriptsButton");
            scriptsButton.RegisterCallback<ClickEvent>(OnScriptsButtonPressed);

            Button encountersButton = rootVisualElement.Q<Button>("EncountersButton");
            encountersButton.RegisterCallback<ClickEvent>(OnEncountersButtonPressed);

            Button exportButton = rootVisualElement.Q<Button>("ExportButton");
            exportButton.RegisterCallback<ClickEvent>(OnExportButtonPressed);

            m_CurrentFieldLabel      = rootVisualElement.Q<Label>("CurrentFieldLabel");
            m_CurrentFieldLabel.text = string.Empty;

            m_FieldsContainerListView = rootVisualElement.Q<ListView>("FieldsContainer");

            m_FieldsContainerListView.itemsSource = m_FieldDesignerData.Fields;
            m_FieldsContainerListView.makeItem    = () => new Label();
            m_FieldsContainerListView.bindItem = (element, index) =>
                                                 {
                                                     GameObject prefab = m_FieldDesignerData.Fields[index].Prefab;
                                                     string     text   = prefab != null ? prefab.name : "Unknown";

                                                     Label label = (Label)element;
                                                     label.text = text;
                                                 };

            m_FieldsContainerListView.selectedIndicesChanged += OnFieldSelected;
            m_FieldsContainerListView.Rebuild();

            InitPrefabTab();
            InitTextTab();
            InitScriptsTab();
            InitEncountersTab();
        }

        private void AddFieldButtonCallback(ClickEvent e)
        {
            m_FieldDesignerData.Fields.Add(new FieldDatabaseAssetAuthoring());
            m_FieldsContainerListView.RefreshItems();
        }

        private void DeleteFieldButtonCallback(ClickEvent e)
        {
            if (m_FieldsContainerListView.selectedIndex == -1)
            {
                return;
            }

            if (EditorUtility.DisplayDialog("Delete Field", "Are you sure you want to delete this field?", "Ok", "Cancel"))
            {
                m_FieldDesignerData.Fields.RemoveAt(m_FieldsContainerListView.selectedIndex);
                m_FieldsContainerListView.ClearSelection();
                m_FieldsContainerListView.RefreshItems();
            }
        }

        private void OnPrefabButtonPressed(ClickEvent e)
        {
            if (m_FieldsContainerListView.selectedIndex == -1)
            {
                return;
            }

            SetElementVisible(m_PrefabViewer,     true);
            SetElementVisible(m_TextViewer,       false);
            SetElementVisible(m_ScriptsViewer,    false);
            SetElementVisible(m_EncountersViewer, false);
        }

        private void OnTextButtonPressed(ClickEvent e)
        {
            if (m_FieldsContainerListView.selectedIndex == -1)
            {
                return;
            }

            m_TextViewerListView.itemsSource = m_LocalisationSheetAssets;
            m_TextViewerListView.Rebuild();

            SetElementVisible(m_PrefabViewer,     false);
            SetElementVisible(m_TextViewer,       true);
            SetElementVisible(m_ScriptsViewer,    false);
            SetElementVisible(m_EncountersViewer, false);
        }

        private void OnScriptsButtonPressed(ClickEvent e)
        {
            if (m_FieldsContainerListView.selectedIndex == -1)
            {
                return;
            }

            CloseOpenPrefab();

            m_CurrentlyOpenPrefabPath = AssetDatabase.GetAssetPath(m_CurrentFieldAsset.Prefab);
            m_CurrentlyOpenPrefab     = PrefabUtility.LoadPrefabContents(m_CurrentlyOpenPrefabPath);
            m_OpenFieldEntities       = m_CurrentlyOpenPrefab.GetComponent<FieldEntities>();

            m_EntityListView.ClearSelection();
            m_EntityScriptListView.ClearSelection();
            m_EntityScriptListView.itemsSource = new List<FieldScriptRecord>();
            m_EntityScriptListView.Rebuild();

            m_ScriptBlockContainer.Clear();
            m_SelectedEntity = null;
            m_SelectedScript = null;

            m_EntityListView.itemsSource = m_OpenFieldEntities != null ? m_OpenFieldEntities.Entities : new List<FieldEntityRecord>();
            m_EntityListView.Rebuild();

            string hint = m_OpenFieldEntities != null
                              ? "Select an entity, then one of its scripts."
                              : $"This field has no {nameof(FieldEntities)} on its root. Run RPG Framework / Field / Migrate Entities To Records.";

            m_ScriptBlockContainer.Add(new HelpBox(hint, m_OpenFieldEntities != null ? HelpBoxMessageType.None : HelpBoxMessageType.Warning));

            SetEditingEnabled(m_OpenFieldEntities != null, false, false);

            SetElementVisible(m_PrefabViewer,     false);
            SetElementVisible(m_TextViewer,       false);
            SetElementVisible(m_ScriptsViewer,    true);
            SetElementVisible(m_EncountersViewer, false);
        }

        private void OnEncountersButtonPressed(ClickEvent e)
        {
            if (m_FieldsContainerListView.selectedIndex == -1)
            {
                return;
            }

            SetElementVisible(m_PrefabViewer,     false);
            SetElementVisible(m_TextViewer,       false);
            SetElementVisible(m_ScriptsViewer,    false);
            SetElementVisible(m_EncountersViewer, true);
        }

        private void OnExportButtonPressed(ClickEvent e)
        {
            m_Window           =  CreateInstance<ModalWindow>();
            m_Window.OnConfirm += OnGenerateFieldDatabaseScriptButtonClickedCallback;

            m_Window.Init(nameof(FieldDesignerWindow), "Generate Asset Bundles and FieldDatabase script", "FieldDatabase.cs");
        }

        private void OnGenerateFieldDatabaseScriptButtonClickedCallback(string path, string filename, string namespaceForScript)
        {
            m_Window.OnConfirm -= OnGenerateFieldDatabaseScriptButtonClickedCallback;
            m_Window           =  null;

            // Export reads the saved prefabs, so unsaved edits here would not be in it.
            if (m_HasUnsavedChanges && EditorUtility.DisplayDialog("Unsaved script changes", $"Save the changes to {m_CurrentlyOpenPrefab.name}'s scripts before exporting?", "Save", "Export without them"))
            {
                SaveOpenPrefab();
            }

            List<string> problems = m_FieldDesignerData.FieldDatabase.ValidateFields();

            if (problems.Count > 0)
            {
                string message = $"{problems.Count} problem(s) found. Nothing was exported.\n\n{string.Join("\n\n", problems)}";

                Debug.LogError($"{nameof(FieldDesignerWindow)}::{nameof(OnGenerateFieldDatabaseScriptButtonClickedCallback)} {message}");
                EditorUtility.DisplayDialog("Export failed", message, "OK");
                return;
            }

            m_FieldDesignerData.FieldDatabase.BuildScriptFile(path, filename, namespaceForScript);
            m_FieldDesignerData.FieldDatabase.BuildAssetBundles();
        }

        private void OnFieldSelected(IEnumerable<int> obj)
        {
            CloseOpenPrefab();

            // The lists still hold the field being left, and its prefab has just been unloaded.
            if (m_AddEntityButton != null)
            {
                m_SelectedEntity = null;
                m_SelectedScript = null;

                m_EntityListView.ClearSelection();
                m_EntityScriptListView.ClearSelection();

                m_EntityListView.itemsSource       = new List<FieldEntityRecord>();
                m_EntityScriptListView.itemsSource = new List<FieldScriptRecord>();

                m_EntityListView.Rebuild();
                m_EntityScriptListView.Rebuild();

                m_ScriptBlockContainer?.Clear();

                SetEditingEnabled(false, false, false);
            }

            int index = m_FieldsContainerListView.selectedIndex;
            if (index < 0)
            {
                return;
            }

            m_CurrentFieldAsset = m_FieldDesignerData.Fields[index];

            GameObject prefab = m_CurrentFieldAsset.Prefab;
            string     text   = prefab != null ? prefab.name : "Unknown";

            m_PrefabObjectField.value = m_CurrentFieldAsset.Prefab;

            m_CurrentFieldLabel.text = text;

            m_LocalisationSheetAssets = new List<LocalisationSheetAsset>();
            if (m_CurrentFieldAsset.LocalisationSheets != null)
            {
                m_LocalisationSheetAssets.AddRange(m_CurrentFieldAsset.LocalisationSheets);
            }

            SetElementVisible(m_PrefabViewer,     false);
            SetElementVisible(m_TextViewer,       false);
            SetElementVisible(m_ScriptsViewer,    false);
            SetElementVisible(m_EncountersViewer, false);
        }

        private void InitPrefabTab()
        {
            m_PrefabViewer = rootVisualElement.Q<VisualElement>("PrefabViewer");
            SetElementVisible(m_PrefabViewer, false);

            m_PrefabObjectField = rootVisualElement.Q<ObjectField>("PrefabObjectField");
            m_PrefabObjectField.RegisterValueChangedCallback(OnPrefabObjectFieldChanged);
        }

        private void InitTextTab()
        {
            m_TextViewer = rootVisualElement.Q<VisualElement>("TextViewer");
            SetElementVisible(m_TextViewer, false);

            m_TextViewerListView = rootVisualElement.Q<ListView>("TextViewerListView");
            m_TextViewerListView.makeItem = () =>
                                            {
                                                ObjectField objectField = new ObjectField
                                                                          {
                                                                              objectType = typeof(LocalisationSheetAsset)
                                                                          };

                                                objectField.RegisterValueChangedCallback(OnTextViewerObjectFieldChanged);

                                                return objectField;
                                            };
            m_TextViewerListView.bindItem = (element, index) =>
                                            {
                                                ObjectField objectField = (ObjectField)element;
                                                objectField.userData = index;
                                                objectField.SetValueWithoutNotify(m_LocalisationSheetAssets[index]);
                                            };
        }

        private void InitScriptsTab()
        {
            m_ScriptsViewer = rootVisualElement.Q<VisualElement>("ScriptsViewer");
            SetElementVisible(m_ScriptsViewer, false);

            m_EntityListView       = rootVisualElement.Q<ListView>("EntityListView");
            m_EntityScriptListView = rootVisualElement.Q<ListView>("EntityScriptListView");
            m_AddEntityButton      = rootVisualElement.Q<Button>("AddEntityButton");
            m_AddScriptButton      = rootVisualElement.Q<Button>("AddScriptButton");
            m_DeleteEntityButton   = rootVisualElement.Q<Button>("DeleteEntityButton");
            m_DeleteScriptButton   = rootVisualElement.Q<Button>("DeleteScriptButton");

            m_AddEntityButton.RegisterCallback<ClickEvent>(OnAddEntityPressed);
            m_AddScriptButton.RegisterCallback<ClickEvent>(OnAddScriptPressed);
            m_DeleteEntityButton.RegisterCallback<ClickEvent>(OnDeleteEntityPressed);
            m_DeleteScriptButton.RegisterCallback<ClickEvent>(OnDeleteScriptPressed);

            SetEditingEnabled(false, false, false);

            m_EntityListView.makeItem = () => new Label();
            m_EntityListView.bindItem = (element, index) =>
                                        {
                                            FieldEntityRecord entity = m_OpenFieldEntities.Entities[index];

                                            Label label = (Label)element;
                                            label.text = entity.Body != null ? $"{entity.EntityId}  {entity.Name}" : $"{entity.EntityId}  {entity.Name}  (no body)";
                                        };
            m_EntityListView.selectedIndicesChanged += OnEntitySelected;

            m_ScriptBlockContainer = rootVisualElement.Q<VisualElement>("ScriptBlockContainer");

            m_EntityScriptListView.makeItem = () => new Label();
            m_EntityScriptListView.bindItem = (element, index) =>
                                              {
                                                  FieldScriptRecord script = m_SelectedEntity.Scripts[index];

                                                  Label label = (Label)element;
                                                  label.text = string.IsNullOrEmpty(script.Name) ? $"{index}  {script.Type}" : $"{index}  {script.Type}  {script.Name}";
                                              };
            m_EntityScriptListView.selectedIndicesChanged += OnEntityScriptSelected;
        }

        private void InitEncountersTab()
        {
            m_EncountersViewer = rootVisualElement.Q<VisualElement>("EncountersViewer");
            SetElementVisible(m_EncountersViewer, false);
        }

        /// <summary>
        /// Also called with nothing selected — clearing the list reports that as a change — and while the list still
        /// holds the field being left, so the index is checked against what is in the list now.
        /// </summary>
        private void OnEntitySelected(IEnumerable<int> obj)
        {
            int index = m_EntityListView.selectedIndex;

            m_ScriptBlockContainer.Clear();
            m_SelectedScript = null;

            m_EntityScriptListView.ClearSelection();

            if (m_OpenFieldEntities == null || index < 0 || index >= m_OpenFieldEntities.Entities.Count)
            {
                m_SelectedEntity                   = null;
                m_EntityScriptListView.itemsSource = new List<FieldScriptRecord>();
                m_EntityScriptListView.Rebuild();

                SetEditingEnabled(m_OpenFieldEntities != null, false, false);
                return;
            }

            m_SelectedEntity = m_OpenFieldEntities.Entities[index];

            m_EntityScriptListView.itemsSource = m_SelectedEntity.Scripts;
            m_EntityScriptListView.Rebuild();

            SetEditingEnabled(true, true, false);

            m_ScriptBlockContainer.Add(BuildEntityHeader());
            m_ScriptBlockContainer.Add(new HelpBox("Select one of this entity's scripts to edit it.", HelpBoxMessageType.None));
        }

        private void OnEntityScriptSelected(IEnumerable<int> obj)
        {
            m_ScriptBlockContainer.Clear();
            m_SelectedScript = null;

            int index = m_EntityScriptListView.selectedIndex;

            if (index < 0 || m_SelectedEntity == null || index >= m_SelectedEntity.Scripts.Count)
            {
                return;
            }

            m_SelectedScript = m_SelectedEntity.Scripts[index];

            SetEditingEnabled(true, true, true);

            m_ScriptBlockContainer.Add(BuildEntityHeader());
            m_ScriptBlockContainer.Add(BuildScriptHeader(index, m_SelectedScript));
            m_ScriptBlockContainer.Add(new FieldScriptBlockEditor(m_SelectedScript.Text, OnScriptTextChanged));
        }

        /// <summary>
        /// The entity the selected script belongs to: what it is called, and which object in the field is its body —
        /// an entity that only runs scripts needs none.
        /// </summary>
        private VisualElement BuildEntityHeader()
        {
            VisualElement header = new VisualElement();
            header.style.marginBottom = 8;

            Label entityTitle = new Label($"Entity {m_SelectedEntity.EntityId}");
            entityTitle.style.unityFontStyleAndWeight = FontStyle.Bold;

            TextField entityName = new TextField("Name") { value = m_SelectedEntity.Name };
            entityName.RegisterValueChangedCallback(e =>
                                                    {
                                                        m_SelectedEntity.SetName(e.newValue);
                                                        m_HasUnsavedChanges = true;
                                                        m_EntityListView.RefreshItems();
                                                    });

            header.Add(entityTitle);
            header.Add(entityName);
            header.Add(BuildBodyField());

            if (m_SelectedEntity.Body == null)
            {
                header.Add(BuildAddBodySection());
            }

            return header;
        }

        /// <summary>
        /// A body is chosen as what the entity is for — a gateway, a character — rather than as a list of components,
        /// and its visuals stay a prefab of the game's own. An entity that only runs scripts needs none of this.
        /// </summary>
        private VisualElement BuildAddBodySection()
        {
            Foldout foldout = new Foldout { text = "Add a body", value = false };

            EnumField   preset  = new EnumField("Is a", FieldBodyPreset.Character);
            TextField   name    = new TextField("Object name") { value = m_SelectedEntity.Name };
            ObjectField visuals = new ObjectField("Visuals prefab") { objectType = typeof(GameObject), allowSceneObjects = false };
            visuals.tooltip = "Instantiated as the body's visible object, which VISIBILITY shows and hides. Leave empty for something unseen, such as a gateway";

            foldout.Add(preset);
            foldout.Add(name);
            foldout.Add(visuals);
            foldout.Add(new Button(() => AddBody((FieldBodyPreset)preset.value, name.value, (GameObject)visuals.value)) { text = "Build body" });

            ObjectField existing = new ObjectField("Or a body prefab") { objectType = typeof(GameObject), allowSceneObjects = false };
            existing.tooltip = "A prefab that is already a body — the same kind of character used in several fields. It is added as it is";

            foldout.Add(existing);
            foldout.Add(new Button(() => UseBodyPrefab((GameObject)existing.value)) { text = "Use prefab as body" });

            return foldout;
        }

        private void AddBody(FieldBodyPreset preset, string name, GameObject visuals)
        {
            FieldEntity body = FieldEntityBodyBuilder.Build(m_CurrentlyOpenPrefab, m_OpenFieldEntities.Dimension, preset, name, visuals);

            m_SelectedEntity.SetBody(body);

            // A gateway's script is what takes the player out of the field, so it starts with one to write the jump in.
            if (preset == FieldBodyPreset.Gateway && !m_SelectedEntity.Scripts.Exists(script => script.Type == FieldScriptType.Gateway))
            {
                m_SelectedEntity.Scripts.Add(new FieldScriptRecord(FieldScriptType.Gateway, string.Empty, NEW_SCRIPT_TEXT));
            }

            WriteInitScript(preset, visuals != null);

            OnBodyChanged();
        }

        /// <summary>
        /// What a body of this kind cannot do without: the player's entity is the one that claims the player, and an
        /// entity starts hidden, so anything with visuals has to show itself. Only written into an init script still
        /// left as the one <c>Add Entity</c> made, so nothing an author wrote is touched.
        /// </summary>
        private void WriteInitScript(FieldBodyPreset preset, bool hasVisuals)
        {
            FieldScriptRecord init = m_SelectedEntity.Scripts.Find(script => script.Type == FieldScriptType.Init);

            if (init == null || init.Text != NEW_SCRIPT_TEXT)
            {
                return;
            }

            List<string> lines = new List<string>();

            if (preset == FieldBodyPreset.PlayerCharacter)
            {
                lines.Add("SET_PLAYER_ENTITY");
            }

            if (hasVisuals)
            {
                lines.Add("VISIBILITY true");
            }

            if (lines.Count == 0)
            {
                return;
            }

            lines.Add(NEW_SCRIPT_TEXT);

            init.SetText(string.Join("\n", lines));
        }

        private void UseBodyPrefab(GameObject bodyPrefab)
        {
            if (bodyPrefab == null)
            {
                return;
            }

            GameObject  instance = (GameObject)PrefabUtility.InstantiatePrefab(bodyPrefab, m_CurrentlyOpenPrefab.transform);
            FieldEntity body     = instance.GetComponent<FieldEntity>();

            if (body == null)
            {
                body = instance.AddComponent<FieldEntity>();
            }

            m_SelectedEntity.SetBody(body);

            OnBodyChanged();
        }

        private void OnBodyChanged()
        {
            m_HasUnsavedChanges = true;

            m_EntityListView.RefreshItems();
            m_EntityScriptListView.Rebuild();

            m_ScriptBlockContainer.Clear();
            m_ScriptBlockContainer.Add(BuildEntityHeader());
            m_ScriptBlockContainer.Add(new HelpBox("Select one of this entity's scripts to edit it.", HelpBoxMessageType.None));
        }

        /// <summary>
        /// Offered as a list of the field's bodies rather than an object field, because the field is open as a loaded
        /// copy and an object picker cannot see into it.
        /// </summary>
        private VisualElement BuildBodyField()
        {
            List<FieldEntity> bodies  = new List<FieldEntity>(m_CurrentlyOpenPrefab.GetComponentsInChildren<FieldEntity>(true));
            List<string>      choices = new List<string> { NO_BODY };

            foreach (FieldEntity body in bodies)
            {
                choices.Add(body.gameObject.name);
            }

            int chosen = m_SelectedEntity.Body == null ? 0 : bodies.IndexOf(m_SelectedEntity.Body) + 1;

            DropdownField field = new DropdownField("Body", choices, Mathf.Max(0, chosen));
            field.RegisterValueChangedCallback(e =>
                                               {
                                                   int index = choices.IndexOf(e.newValue);

                                                   m_SelectedEntity.SetBody(index <= 0 ? null : bodies[index - 1]);
                                                   m_HasUnsavedChanges = true;
                                                   m_EntityListView.RefreshItems();
                                               });

            return field;
        }

        private VisualElement BuildScriptHeader(int eventId, FieldScriptRecord script)
        {
            VisualElement header = new VisualElement();
            header.style.marginBottom = 6;

            Label scriptTitle = new Label($"Event {eventId}");
            scriptTitle.style.unityFontStyleAndWeight = FontStyle.Bold;

            EnumField scriptType = new EnumField("Type", script.Type);
            scriptType.RegisterValueChangedCallback(e =>
                                                    {
                                                        script.SetType((FieldScriptType)e.newValue);
                                                        m_HasUnsavedChanges = true;
                                                        m_EntityScriptListView.RefreshItems();
                                                    });

            TextField scriptName = new TextField("Name") { value = script.Name };
            scriptName.tooltip = "What this script is for. Authoring only — it is not exported";
            scriptName.RegisterValueChangedCallback(e =>
                                                    {
                                                        script.SetName(e.newValue);
                                                        m_HasUnsavedChanges = true;
                                                        m_EntityScriptListView.RefreshItems();
                                                    });

            VisualElement buttons = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            buttons.Add(new Button(CheckSelectedScript) { text = "Check script" });
            buttons.Add(new Button(SaveOpenPrefab) { text = "Save field" });

            m_ScriptStatus               = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            m_ScriptStatus.style.display = DisplayStyle.None;

            header.Add(scriptTitle);
            header.Add(scriptType);
            header.Add(scriptName);
            header.Add(buttons);
            header.Add(m_ScriptStatus);

            return header;
        }

        /// <summary>
        /// A new entity has no body: one that only runs scripts needs none, and one that does gets its body chosen in
        /// the header. Its id is the next free one, since ids are how scripts name each other.
        /// </summary>
        private void OnAddEntityPressed(ClickEvent e)
        {
            if (m_OpenFieldEntities == null)
            {
                return;
            }

            int entityId = 0;

            foreach (FieldEntityRecord record in m_OpenFieldEntities.Entities)
            {
                if (record.EntityId >= entityId)
                {
                    entityId = record.EntityId + 1;
                }
            }

            FieldEntityRecord entity = new FieldEntityRecord(entityId, $"Entity{entityId}", null);
            entity.Scripts.Add(new FieldScriptRecord(FieldScriptType.Init, string.Empty, NEW_SCRIPT_TEXT));

            m_OpenFieldEntities.Entities.Add(entity);
            m_HasUnsavedChanges = true;

            m_EntityListView.Rebuild();
            m_EntityListView.SetSelection(m_OpenFieldEntities.Entities.Count - 1);
        }

        private void SetEditingEnabled(bool hasField, bool hasEntity, bool hasScript)
        {
            m_AddEntityButton.SetEnabled(hasField);
            m_DeleteEntityButton.SetEnabled(hasEntity);
            m_AddScriptButton.SetEnabled(hasEntity);
            m_DeleteScriptButton.SetEnabled(hasScript);
        }

        /// <summary>
        /// An entity's body is its own, so it goes with it unless the author wants to keep the object. Scripts on
        /// other entities that request this one by id are left naming an entity that is no longer there, which export
        /// refuses.
        /// </summary>
        private void OnDeleteEntityPressed(ClickEvent e)
        {
            if (m_SelectedEntity == null)
            {
                return;
            }

            string question = $"Delete entity {m_SelectedEntity.EntityId} '{m_SelectedEntity.Name}' and its {m_SelectedEntity.Scripts.Count} script(s)?";

            if (m_SelectedEntity.Body != null)
            {
                int choice = EditorUtility.DisplayDialogComplex("Delete entity", question, "Delete it and its body", "Cancel", "Delete it, keep the body");

                if (choice == 1)
                {
                    return;
                }

                if (choice == 0)
                {
                    Object.DestroyImmediate(m_SelectedEntity.Body.gameObject);
                }
            }
            else if (!EditorUtility.DisplayDialog("Delete entity", question, "Delete", "Cancel"))
            {
                return;
            }

            m_OpenFieldEntities.Entities.Remove(m_SelectedEntity);

            m_SelectedEntity = null;
            m_SelectedScript = null;

            ClearSelectionAfterDelete();
        }

        /// <summary>
        /// A script's place in the list is its event id, so deleting one moves every script after it up: a request
        /// that named one of those by number now names its neighbour.
        /// </summary>
        private void OnDeleteScriptPressed(ClickEvent e)
        {
            if (m_SelectedEntity == null || m_SelectedScript == null)
            {
                return;
            }

            int index = m_SelectedEntity.Scripts.IndexOf(m_SelectedScript);

            string question = $"Delete script [{index}] ({m_SelectedScript.Type}) from '{m_SelectedEntity.Name}'?";

            if (index < m_SelectedEntity.Scripts.Count - 1)
            {
                question += " The scripts after it move up one place, so anything that requests them by event id will name a different script.";
            }

            if (!EditorUtility.DisplayDialog("Delete script", question, "Delete", "Cancel"))
            {
                return;
            }

            m_SelectedEntity.Scripts.Remove(m_SelectedScript);
            m_SelectedScript = null;

            ClearSelectionAfterDelete();
        }

        private void ClearSelectionAfterDelete()
        {
            m_HasUnsavedChanges = true;

            m_EntityListView.ClearSelection();
            m_EntityScriptListView.ClearSelection();
            m_EntityScriptListView.itemsSource = m_SelectedEntity != null ? m_SelectedEntity.Scripts : new List<FieldScriptRecord>();

            m_EntityListView.Rebuild();
            m_EntityScriptListView.Rebuild();

            m_ScriptBlockContainer.Clear();
            m_ScriptBlockContainer.Add(new HelpBox("Select an entity, then one of its scripts.", HelpBoxMessageType.None));

            SetEditingEnabled(true, m_SelectedEntity != null, false);
        }

        private void OnAddScriptPressed(ClickEvent e)
        {
            if (m_SelectedEntity == null)
            {
                return;
            }

            // An entity's first script is its init script; anything later is only run when something asks for it.
            FieldScriptType scriptType = m_SelectedEntity.Scripts.Count == 0 ? FieldScriptType.Init : FieldScriptType.Requested;

            m_SelectedEntity.Scripts.Add(new FieldScriptRecord(scriptType, string.Empty, NEW_SCRIPT_TEXT));
            m_HasUnsavedChanges = true;

            m_EntityScriptListView.Rebuild();
            m_EntityScriptListView.SetSelection(m_SelectedEntity.Scripts.Count - 1);
        }

        private void OnScriptTextChanged(string scriptText)
        {
            if (m_SelectedScript == null)
            {
                return;
            }

            m_SelectedScript.SetText(scriptText);

            m_HasUnsavedChanges = true;
        }

        /// <summary>
        /// Compile only to find mistakes: bytecode is made at export, from whatever the text is then.
        /// </summary>
        private void CheckSelectedScript()
        {
            if (m_SelectedScript == null)
            {
                return;
            }

            try
            {
                byte[] bytecode = FieldScriptCompiler.Compile(m_SelectedScript.Text);

                m_ScriptStatus.text        = $"Compiles to {bytecode.Length} bytes.";
                m_ScriptStatus.messageType = HelpBoxMessageType.Info;
            }
            catch (Exception e)
            {
                m_ScriptStatus.text        = e.Message;
                m_ScriptStatus.messageType = HelpBoxMessageType.Error;
            }

            m_ScriptStatus.style.display = DisplayStyle.Flex;
        }

        private void OnPrefabObjectFieldChanged(ChangeEvent<Object> evt)
        {
            GameObject prefab = (GameObject)evt.newValue;
            m_CurrentFieldAsset.Prefab = prefab;

            m_FieldsContainerListView.RefreshItems();
        }

        private void OnTextViewerObjectFieldChanged(ChangeEvent<Object> evt)
        {
            ObjectField objectField = (ObjectField)evt.target;
            int         index       = (int)objectField.userData;

            m_LocalisationSheetAssets[index]       = (LocalisationSheetAsset)evt.newValue;
            m_CurrentFieldAsset.LocalisationSheets = m_LocalisationSheetAssets.ToArray();
        }

        private static void SetElementVisible(VisualElement element, bool visible)
        {
            element.SetEnabled(visible);
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}