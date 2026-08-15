using System.Collections.Generic;
using RPGFramework.Localisation.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

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

        [MenuItem("RPG Framework/Field Designer Window")]
        public static void ShowWindow()
        {
            GetWindow<FieldDesignerWindow>();
        }

        private void OnEnable()
        {
            m_FieldDesignerData = FieldDesignerDataUtility.GetOrCreate();
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
            // this is to list entities in prefab and their view/modify their scripts  

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
            // this is to list which encounters can happen and their frequency including off

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

            m_FieldDesignerData.FieldDatabase.BuildScriptFile(path, filename, namespaceForScript);
            m_FieldDesignerData.FieldDatabase.BuildAssetBundles();
        }

        private void OnFieldSelected(IEnumerable<int> obj)
        {
            m_CurrentFieldAsset = m_FieldDesignerData.Fields[m_FieldsContainerListView.selectedIndex];

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
        }

        private void InitEncountersTab()
        {
            m_EncountersViewer = rootVisualElement.Q<VisualElement>("EncountersViewer");
            SetElementVisible(m_EncountersViewer, false);
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