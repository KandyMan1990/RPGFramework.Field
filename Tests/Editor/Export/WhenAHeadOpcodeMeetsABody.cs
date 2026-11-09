using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Field.Editor;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Field.Tests.Export
{
    /// <summary>Export's check that a body can turn the head its scripts turn.</summary>
    internal sealed class WhenAHeadOpcodeMeetsABody
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
        public void ThenABodyWithNoHeadIsRefused()
        {
            Assert.AreEqual(1, Problems("HEAD_LOOK_FORWARD 1.0").Count);
        }

        [Test]
        public void ThenEachOpcodeIsReported()
        {
            Assert.AreEqual(2, Problems("HEAD_LOOK_FORWARD 1.0", "WAIT_FOR_HEAD").Count);
        }

        [Test]
        public void ThenABodyNamingItsHeadBoneIsAccepted()
        {
            Transform head = new GameObject("Head").transform;

            head.SetParent(m_Body.transform, false);

            SerializedObject authored = new SerializedObject(m_Entity);

            authored.FindProperty("m_Head").objectReferenceValue = head;
            authored.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(0, Problems("HEAD_LOOK_FORWARD 1.0").Count);
        }

        [Test]
        public void ThenAnOpcodeThatTurnsNoHeadIsLeftAlone()
        {
            Assert.AreEqual(0, Problems("RETURN").Count);
        }

        private List<string> Problems(params string[] lines)
        {
            List<string> problems = new List<string>();

            FieldDatabase.ValidateHeadOpcodes(m_Entity, "script", lines, problems);

            return problems;
        }
    }
}
