using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Field.Editor
{
    public class FieldDesignerData : ScriptableObject
    {
        private FieldDatabase m_FieldDatabase;

        public FieldDatabase                     FieldDatabase => m_FieldDatabase;
        public List<FieldDatabaseAssetAuthoring> Fields        => m_FieldDatabase.Fields;

        public void Initialise()
        {
            m_FieldDatabase = new FieldDatabase();
        }

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    internal static class FieldDesignerDataUtility
    {
        private const string ASSETS            = "Assets";
        private const string EDITOR            = "Editor";
        private const string FIELD             = "Field";
        private const string EDITOR_FOLDER     = "Assets/Editor";
        private const string FIELD_FOLDER      = "Assets/Editor/Field";
        private const string ASSET_PATH        = "Assets/Editor/Field/FieldDesignerData.asset";
        private const string SEARCH_PARAMETERS = "t:FieldDesignerData";

        public static FieldDesignerData GetOrCreate()
        {
            FieldDesignerData data = FindData();

            if (data == null)
            {
                data = CreateData();
            }

            return data;
        }

        private static FieldDesignerData FindData()
        {
            string[] guids = AssetDatabase.FindAssets(SEARCH_PARAMETERS);

            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);

                FieldDesignerData existing = AssetDatabase.LoadAssetAtPath<FieldDesignerData>(path);

                if (existing != null)
                {
                    return existing;
                }
            }

            return null;
        }

        private static FieldDesignerData CreateData()
        {
            if (!AssetDatabase.IsValidFolder(EDITOR_FOLDER))
            {
                AssetDatabase.CreateFolder(ASSETS, EDITOR);
            }

            if (!AssetDatabase.IsValidFolder(FIELD_FOLDER))
            {
                AssetDatabase.CreateFolder(EDITOR_FOLDER, FIELD);
            }

            FieldDesignerData data = ScriptableObject.CreateInstance<FieldDesignerData>();
            data.Initialise();

            AssetDatabase.CreateAsset(data, ASSET_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = data;

            return data;
        }
    }
}