using System;
using System.Collections.Generic;
using System.Linq;
using RPGFramework.Localisation.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace RPGFramework.Field.Editor
{
    public class FieldDesignerWindow : EditorWindow
    {
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
        private VisualElement m_ScriptBlockContainer;

        private FieldEntity       m_SelectedEntity;
        private FieldScriptSource m_SelectedScriptSource;

        private GameObject        m_CurrentlyOpenPrefab;
        private List<FieldEntity> m_CurrentlyOpenPrefabFieldEntities;

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
            if (m_CurrentlyOpenPrefab != null)
            {
                PrefabUtility.UnloadPrefabContents(m_CurrentlyOpenPrefab);
                m_CurrentlyOpenPrefab = null;
            }
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

            string path = AssetDatabase.GetAssetPath(m_CurrentFieldAsset.Prefab);
            m_CurrentlyOpenPrefab = PrefabUtility.LoadPrefabContents(path);

            m_CurrentlyOpenPrefabFieldEntities = m_CurrentlyOpenPrefab.GetComponentsInChildren<FieldEntity>().ToList();

            m_EntityListView.ClearSelection();
            m_EntityScriptListView.ClearSelection();
            m_EntityScriptListView.itemsSource = new List<ScriptEntry>();
            m_EntityScriptListView.Rebuild();

            m_ScriptBlockContainer.Clear();
            m_SelectedEntity       = null;
            m_SelectedScriptSource = null;

            m_EntityListView.itemsSource = m_CurrentlyOpenPrefabFieldEntities;
            m_EntityListView.Rebuild();

            m_ScriptBlockContainer.Add(new HelpBox("Select an entity, then one of its scripts.", HelpBoxMessageType.None));

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

            List<string> problems = FieldCompiledScriptAssets.CompileAll();

            if (problems.Count == 0)
            {
                problems = m_FieldDesignerData.FieldDatabase.ValidateFields();
            }

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
            if (m_CurrentlyOpenPrefab != null)
            {
                PrefabUtility.UnloadPrefabContents(m_CurrentlyOpenPrefab);
                m_CurrentlyOpenPrefab = null;
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

            m_EntityListView.makeItem = () => new Label();
            m_EntityListView.bindItem = (element, index) =>
                                        {
                                            GameObject entity = m_CurrentlyOpenPrefabFieldEntities[index].gameObject;

                                            Label label = (Label)element;
                                            label.text = entity.name;
                                        };
            m_EntityListView.selectedIndicesChanged += OnEntitySelected;

            m_ScriptBlockContainer = rootVisualElement.Q<VisualElement>("ScriptBlockContainer");

            m_EntityScriptListView.makeItem = () => new Label();
            m_EntityScriptListView.bindItem = (element, index) =>
                                              {
                                                  ScriptEntry scriptEntry = m_SelectedEntity.ScriptDefinition.Scripts[index];

                                                  Label label = (Label)element;
                                                  label.text = $"{index}  {scriptEntry.ScriptType}";
                                              };
            m_EntityScriptListView.selectedIndicesChanged += OnEntityScriptSelected;
        }

        private void InitEncountersTab()
        {
            m_EncountersViewer = rootVisualElement.Q<VisualElement>("EncountersViewer");
            SetElementVisible(m_EncountersViewer, false);
        }

        private void OnEntitySelected(IEnumerable<int> obj)
        {
            m_SelectedEntity = m_CurrentlyOpenPrefabFieldEntities[m_EntityListView.selectedIndex];

            m_ScriptBlockContainer.Clear();
            m_SelectedScriptSource = null;

            m_EntityScriptListView.ClearSelection();

            if (m_SelectedEntity.ScriptDefinition == null)
            {
                m_EntityScriptListView.itemsSource = new List<ScriptEntry>();
                m_EntityScriptListView.Rebuild();

                m_ScriptBlockContainer.Add(new HelpBox($"'{m_SelectedEntity.name}' has no {nameof(FieldScriptDefinition)}.", HelpBoxMessageType.Warning));
                return;
            }

            m_EntityScriptListView.itemsSource = m_SelectedEntity.ScriptDefinition.Scripts;
            m_EntityScriptListView.Rebuild();

            m_ScriptBlockContainer.Add(new HelpBox("Select one of this entity's scripts to edit it.", HelpBoxMessageType.None));
        }

        private void OnEntityScriptSelected(IEnumerable<int> obj)
        {
            m_ScriptBlockContainer.Clear();
            m_SelectedScriptSource = null;

            int index = m_EntityScriptListView.selectedIndex;

            if (index < 0 || m_SelectedEntity?.ScriptDefinition?.Scripts == null)
            {
                return;
            }

            if (index >= m_SelectedEntity.ScriptDefinition.Scripts.Count)
            {
                return;
            }

            ScriptEntry scriptEntry = m_SelectedEntity.ScriptDefinition.Scripts[index];

            if (scriptEntry.CompiledScript == null)
            {
                m_ScriptBlockContainer.Add(new HelpBox($"Script [{index}] ({scriptEntry.ScriptType}) has no compiled script assigned.", HelpBoxMessageType.Warning));
                return;
            }

            if (!FieldCompiledScriptAssets.TryFindSource(scriptEntry.CompiledScript, out m_SelectedScriptSource))
            {
                m_ScriptBlockContainer.Add(new HelpBox($"No {nameof(FieldScriptSource)} found beside '{scriptEntry.CompiledScript.name}'. A compiled script is written next to the source it came from, so the two must stay together.", HelpBoxMessageType.Warning));
                return;
            }

            m_ScriptBlockContainer.Add(BuildScriptHeader(index, scriptEntry));
            m_ScriptBlockContainer.Add(new FieldScriptBlockEditor(m_SelectedScriptSource.ScriptText, OnScriptTextChanged));
        }

        private VisualElement BuildScriptHeader(int eventId, ScriptEntry scriptEntry)
        {
            VisualElement header = new VisualElement();
            header.style.marginBottom = 6;

            Label scriptTitle = new Label($"{m_SelectedEntity.ScriptDefinition.EntityName} — {scriptEntry.ScriptType} (event {eventId})");
            scriptTitle.style.unityFontStyleAndWeight = FontStyle.Bold;

            header.Add(scriptTitle);
            header.Add(new Button(CompileSelectedScript) { text = "Compile script" });

            return header;
        }

        private void OnScriptTextChanged(string scriptText)
        {
            if (m_SelectedScriptSource == null)
            {
                return;
            }

            Undo.RecordObject(m_SelectedScriptSource, "Edit field script");

            m_SelectedScriptSource.ScriptText = scriptText;

            EditorUtility.SetDirty(m_SelectedScriptSource);
        }

        private void CompileSelectedScript()
        {
            if (m_SelectedScriptSource == null)
            {
                return;
            }

            try
            {
                byte[] bytecode = FieldScriptCompiler.Compile(m_SelectedScriptSource.ScriptText);

                FieldCompiledScriptAssets.Write(m_SelectedScriptSource, bytecode);

                Debug.Log($"{nameof(FieldDesignerWindow)} compiled '{m_SelectedScriptSource.name}' to {bytecode.Length} bytes");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Compile failed", e.Message, "OK");
            }
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