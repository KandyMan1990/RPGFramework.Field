using System.Collections.Generic;
using RPGFramework.Core.Dialogue;
using RPGFramework.Core.Editor.Dialogue;
using RPGFramework.Localisation.Editor;
using UnityEditor;

namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// Checks the dialogue markup in the sheets fields use, when the sheets are generated — the moment a writer's
    /// text arrives, and while they can still fix it. A typo such as <c>{Locaton}</c> or an unclosed style stops
    /// generation with the sheet, key and language, instead of turning up on screen.<br /><br />
    /// Only sheets a field names are checked. Other text — a menu's, say — may use braces for its own purposes.
    /// </summary>
    internal sealed class FieldDialogueTextValidator : ILocalisationTextValidator
    {
        private readonly HashSet<string>     m_DialogueSheets;
        private readonly IDialogueTextStyles m_Styles;

        public FieldDialogueTextValidator()
        {
            m_DialogueSheets = new HashSet<string>();
            m_Styles         = DialogueEditorUtility.FindProjectTextStyles();

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(FieldDesignerData)}"))
            {
                FieldDesignerData data = AssetDatabase.LoadAssetAtPath<FieldDesignerData>(AssetDatabase.GUIDToAssetPath(guid));

                foreach (FieldDatabaseAssetAuthoring field in data.Fields)
                {
                    if (field.LocalisationSheets == null)
                    {
                        continue;
                    }

                    foreach (LocalisationSheetAsset sheet in field.LocalisationSheets)
                    {
                        if (sheet != null)
                        {
                            m_DialogueSheets.Add(sheet.SheetName);
                        }
                    }
                }
            }
        }

        public void Validate(string sheetName, string language, string key, string text, List<string> problems)
        {
            if (!m_DialogueSheets.Contains(sheetName))
            {
                return;
            }

            foreach (string problem in DialogueMarkup.Validate(text, m_Styles))
            {
                problems.Add($"[{sheetName}] {key} ({language}): {problem}");
            }
        }
    }
}
