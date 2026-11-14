using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Field.Editor;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Field.Tests.Export
{
    /// <summary>Export's check that the head opcodes, 3D only, stay out of a 2D field, a sprite showing where it looks by its facing.</summary>
    internal sealed class WhenAHeadOpcodeIsInA2DField
    {
        private GameObject  m_Body;
        private FieldEntity m_Entity;

        [SetUp]
        public void SetUp()
        {
            m_Body   = new GameObject("Body");
            m_Entity = m_Body.AddComponent<FieldEntity>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Body);
        }

        [Test]
        public void ThenItIsRefusedEvenOnABodyNamingAHead()
        {
            Transform head = new GameObject("Head").transform;

            head.SetParent(m_Body.transform, false);

            SerializedObject authored = new SerializedObject(m_Entity);

            authored.FindProperty("m_Head").objectReferenceValue = head;
            authored.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(1, Problems(m_Entity, "HEAD_LOOK_FORWARD 1.0").Count);
        }

        [Test]
        public void ThenItIsRefusedOnAnEntityWithNoBody()
        {
            Assert.AreEqual(1, Problems(null, "WAIT_FOR_HEAD").Count);
        }

        [Test]
        public void ThenTheProblemNamesTheFacingOpcodes()
        {
            StringAssert.Contains("SET_DIRECTION_TO_FACE_ENTITY", Problems(m_Entity, "WAIT_FOR_HEAD")[0]);
        }

        [Test]
        public void ThenAnOpcodeThatTurnsNoHeadIsLeftAlone()
        {
            Assert.AreEqual(0, Problems(m_Entity, "SET_DIRECTION_TO_FACE_ENTITY 0").Count);
        }

        private static List<string> Problems(FieldEntity body, params string[] lines)
        {
            List<string> problems = new List<string>();

            FieldDatabase.ValidateHeadOpcodes(FieldDimension.TwoD, body, "script", lines, problems);

            return problems;
        }
    }
}
