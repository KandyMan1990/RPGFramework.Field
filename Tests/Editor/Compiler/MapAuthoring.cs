using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Field.Tests.Compiler
{
    /// <summary>
    /// A variable map edited as an author edits it, through the asset's serialized properties: three variables in
    /// the persistent bank — a uint, a bool and a record of two with one field.
    /// </summary>
    internal static class MapAuthoring
    {
        internal const int GIL   = 0;
        internal const int FLAG  = 1;
        internal const int PARTY = 2;

        internal static VariableMapAsset ThreeVariables()
        {
            VariableMapAsset   map       = ScriptableObject.CreateInstance<VariableMapAsset>();
            SerializedObject   asset     = new SerializedObject(map);
            SerializedProperty variables = asset.FindProperty("m_Variables");

            variables.arraySize = 3;

            Variable(variables.GetArrayElementAtIndex(GIL),   1, "gil",   VariableWidth.UInt, 1, 0);
            Variable(variables.GetArrayElementAtIndex(FLAG),  2, "flag",  VariableWidth.Bool, 1, 4);
            Variable(variables.GetArrayElementAtIndex(PARTY), 3, "party", VariableWidth.Byte, 2, 6);

            SerializedProperty fields = variables.GetArrayElementAtIndex(PARTY).FindPropertyRelative("m_Fields");

            fields.arraySize = 1;

            SerializedProperty hp = fields.GetArrayElementAtIndex(0);

            hp.FindPropertyRelative("m_Id").intValue    = 1;
            hp.FindPropertyRelative("m_Name").stringValue = "hp";
            hp.FindPropertyRelative("m_Width").intValue = (int)VariableWidth.UShort;
            hp.FindPropertyRelative("m_Count").intValue = 1;

            asset.ApplyModifiedPropertiesWithoutUndo();

            return map;
        }

        internal static SerializedProperty Variable(SerializedObject asset, int index)
        {
            return asset.FindProperty("m_Variables").GetArrayElementAtIndex(index);
        }

        private static void Variable(SerializedProperty variable, uint id, string name, VariableWidth width, int count, int offset)
        {
            variable.FindPropertyRelative("m_Id").uintValue     = id;
            variable.FindPropertyRelative("m_Name").stringValue = name;
            variable.FindPropertyRelative("m_Bank").intValue    = (int)MemoryBank.Persistent;
            variable.FindPropertyRelative("m_Width").intValue   = (int)width;
            variable.FindPropertyRelative("m_Count").intValue   = count;
            variable.FindPropertyRelative("m_Offset").intValue  = offset;
            variable.FindPropertyRelative("m_Fields").arraySize = 0;
        }
    }
}
