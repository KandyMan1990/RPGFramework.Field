using NUnit.Framework;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Field.Tests.Heads
{
    /// <summary>A body at the origin with a head bone one up from it, whichever way that bone happens to point.</summary>
    internal sealed class WhenABodyTurnsItsHead
    {
        private const float TOLERANCE = 0.01f;

        private static readonly Quaternion m_BonePose = Quaternion.Euler(10f, 20f, 30f);

        private GameObject  m_Body;
        private FieldEntity m_Entity;
        private Transform   m_Head;

        [SetUp]
        public void SetUp()
        {
            m_Body   = new GameObject("Body");
            m_Entity = m_Body.AddComponent<FieldEntity>();

            m_Head = new GameObject("Head").transform;
            m_Head.SetParent(m_Body.transform, false);
            m_Head.localPosition = Vector3.up;
            m_Head.localRotation = m_BonePose;

            SerializedObject authored = new SerializedObject(m_Entity);

            authored.FindProperty("m_Head").objectReferenceValue = m_Head;
            authored.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Body);
        }

        [Test]
        public void ThenTheBoneTurnsAboutTheBodysUp()
        {
            m_Entity.TurnHead(new float2(90f, 0f));

            AssertSame(Quaternion.Euler(0f, 90f, 0f) * m_BonePose, m_Head.rotation);
        }

        [Test]
        public void ThenAPitchUpTiltsItBack()
        {
            m_Entity.TurnHead(new float2(0f, 30f));

            AssertSame(Quaternion.Euler(-30f, 0f, 0f) * m_BonePose, m_Head.rotation);
        }

        [Test]
        public void ThenATurnedBodyTurnsItAboutItsOwnAxes()
        {
            Quaternion body = Quaternion.Euler(0f, 90f, 0f);

            m_Body.transform.rotation = body;

            m_Entity.TurnHead(new float2(0f, 30f));

            AssertSame(body * Quaternion.Euler(-30f, 0f, 0f) * m_BonePose, m_Head.rotation);
        }

        [Test]
        public void ThenABoneNothingAnimatedDoesNotTurnFurther()
        {
            m_Entity.TurnHead(new float2(30f, 0f));
            m_Entity.TurnHead(new float2(30f, 0f));

            AssertSame(Quaternion.Euler(0f, 30f, 0f) * m_BonePose, m_Head.rotation);
        }

        [Test]
        public void ThenAFreshPoseFromTheAnimationIsTurnedFrom()
        {
            Quaternion nextPose = Quaternion.Euler(5f, 5f, 5f);

            m_Entity.TurnHead(new float2(30f, 0f));

            m_Head.localRotation = nextPose;

            m_Entity.TurnHead(new float2(30f, 0f));

            AssertSame(Quaternion.Euler(0f, 30f, 0f) * nextPose, m_Head.rotation);
        }

        [Test]
        public void ThenOthersLookAtItsHead()
        {
            Assert.AreEqual(Vector3.up, m_Entity.LookPoint);
        }

        [Test]
        public void ThenABodyWithNoHeadIsLookedAtWhereItStands()
        {
            GameObject  other  = new GameObject("Other");
            FieldEntity entity = other.AddComponent<FieldEntity>();

            other.transform.position = new Vector3(4f, 0f, 0f);

            Assert.AreEqual(new Vector3(4f, 0f, 0f), entity.LookPoint);

            Object.DestroyImmediate(other);
        }

        private static void AssertSame(Quaternion expected, Quaternion actual)
        {
            Assert.AreEqual(0f, Quaternion.Angle(expected, actual), TOLERANCE);
        }
    }
}
