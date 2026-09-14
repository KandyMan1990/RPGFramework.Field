using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.Field.Editor
{
    [CustomEditor(typeof(FieldScriptSource))]
    public sealed class FieldScriptSourceEditor : UnityEditor.Editor
    {
        private FieldScriptSource m_Source;
        private HelpBox           m_CompileResult;
        private TextField         m_RawTextView;

        public override VisualElement CreateInspectorGUI()
        {
            m_Source = (FieldScriptSource)target;

            VisualElement root = new VisualElement();

            root.Add(new PropertyField(serializedObject.FindProperty("ScriptId")));
            root.Add(Spacer(8));

            root.Add(Heading("Blocks"));
            root.Add(new FieldScriptBlockEditor(m_Source.ScriptText, OnScriptTextChanged));

            root.Add(Spacer(8));
            root.Add(BuildRawText());

            root.Add(Spacer(8));
            root.Add(new Button(Compile) { text = "Compile script" });

            m_CompileResult               = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            m_CompileResult.style.display = DisplayStyle.None;
            root.Add(m_CompileResult);

            root.Bind(serializedObject);

            return root;
        }

        private VisualElement BuildRawText()
        {
            Foldout foldout = new Foldout { text = "Script text", value = false };

            TextField text = new TextField { multiline = true, value = m_Source.ScriptText };
            text.style.minHeight = 120;
            text.SetEnabled(false);

            foldout.Add(new HelpBox("Generated from the blocks above. Edit the asset directly if you need to hand-write a script; unrecognised lines are preserved.", HelpBoxMessageType.None));
            foldout.Add(text);

            m_RawTextView = text;

            return foldout;
        }

        private void OnScriptTextChanged(string scriptText)
        {
            Undo.RecordObject(m_Source, "Edit field script");

            m_Source.ScriptText = scriptText;

            EditorUtility.SetDirty(m_Source);
            serializedObject.Update();

            m_RawTextView?.SetValueWithoutNotify(scriptText);
        }

        private void Compile()
        {
            try
            {
                byte[] bytecode = FieldScriptCompiler.Compile(m_Source.ScriptText);
                string path     = FieldCompiledScriptAssets.Write(m_Source, bytecode);

                ShowResult($"Compiled to {bytecode.Length} bytes at {path}", HelpBoxMessageType.Info);
            }
            catch (System.Exception e)
            {
                ShowResult(e.Message, HelpBoxMessageType.Error);
            }
        }

        private void ShowResult(string message, HelpBoxMessageType messageType)
        {
            m_CompileResult.text          = message;
            m_CompileResult.messageType   = messageType;
            m_CompileResult.style.display = DisplayStyle.Flex;
        }

        private static VisualElement Spacer(int height)
        {
            VisualElement spacer = new VisualElement();
            spacer.style.height = height;

            return spacer;
        }

        private static Label Heading(string text)
        {
            Label label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom            = 4;

            return label;
        }
    }
}

namespace RPGFramework.Field.Editor
{
    [CreateAssetMenu(menuName = "RPG Framework/Field/Script Source", fileName = "FieldScriptSource")]
    public sealed class FieldScriptSource : ScriptableObject
    {
        [Tooltip("Unique script ID used by the VM")]
        public int ScriptId;

        [TextArea(10, 30)]
        public string ScriptText;
    }
}