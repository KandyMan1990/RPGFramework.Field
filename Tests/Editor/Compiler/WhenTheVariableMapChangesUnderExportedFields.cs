using NUnit.Framework;
using RPGFramework.Core.Memory;
using RPGFramework.Field.Editor;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Field.Tests.Compiler
{
    /// <summary>
    /// A player build refuses fields exported against another layout: what scripts address changes the layout, and
    /// what they do not, the list's order, leaves it alone.
    /// </summary>
    internal sealed class WhenTheVariableMapChangesUnderExportedFields
    {
        private VariableMapAsset m_Map;
        private SerializedObject m_Asset;
        private ulong            m_Exported;

        [SetUp]
        public void SetUp()
        {
            m_Map      = MapAuthoring.ThreeVariables();
            m_Asset    = new SerializedObject(m_Map);
            m_Exported = FieldExport.LayoutHash(m_Map);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Map);
        }

        [Test]
        public void ThenReorderingTheListKeepsTheLayout()
        {
            m_Asset.FindProperty("m_Variables").MoveArrayElement(MapAuthoring.GIL, MapAuthoring.PARTY);
            m_Asset.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(m_Exported, FieldExport.LayoutHash(m_Map));
        }

        [Test]
        public void ThenMovingAVariableChangesTheLayout()
        {
            MapAuthoring.Variable(m_Asset, MapAuthoring.FLAG).FindPropertyRelative("m_Offset").intValue = 5;
            m_Asset.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreNotEqual(m_Exported, FieldExport.LayoutHash(m_Map));
        }

        [Test]
        public void ThenRenamingAVariableChangesTheLayout()
        {
            MapAuthoring.Variable(m_Asset, MapAuthoring.GIL).FindPropertyRelative("m_Name").stringValue = "money";
            m_Asset.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreNotEqual(m_Exported, FieldExport.LayoutHash(m_Map));
        }

        [Test]
        public void ThenResizingARecordsFieldChangesTheLayout()
        {
            MapAuthoring.Variable(m_Asset, MapAuthoring.PARTY).FindPropertyRelative("m_Fields").GetArrayElementAtIndex(0)
                        .FindPropertyRelative("m_Count").intValue = 2;
            m_Asset.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreNotEqual(m_Exported, FieldExport.LayoutHash(m_Map));
        }

        [Test]
        public void ThenPuttingAChangeBackRestoresTheLayout()
        {
            SerializedProperty offset = MapAuthoring.Variable(m_Asset, MapAuthoring.FLAG).FindPropertyRelative("m_Offset");

            offset.intValue = 5;
            m_Asset.ApplyModifiedPropertiesWithoutUndo();
            offset.intValue = 4;
            m_Asset.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(m_Exported, FieldExport.LayoutHash(m_Map));
        }
    }
}
