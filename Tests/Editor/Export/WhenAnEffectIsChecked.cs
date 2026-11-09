using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Field.Editor;
using UnityEngine;

namespace RPGFramework.Field.Tests.Export
{
    /// <summary>Export's check of where an effect sits and the id it has.</summary>
    internal sealed class WhenAnEffectIsChecked
    {
        private GameObject   m_Body;
        private GameObject   m_Loose;
        private HashSet<int> m_Ids;
        private List<string> m_Problems;

        [SetUp]
        public void SetUp()
        {
            m_Body     = new GameObject("Body");
            m_Loose    = new GameObject("Loose");
            m_Ids      = new HashSet<int>();
            m_Problems = new List<string>();

            m_Body.AddComponent<FieldEntity>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Body);
            Object.DestroyImmediate(m_Loose);
        }

        [Test]
        public void ThenOneUnderAnEntityIsAccepted()
        {
            FieldDatabase.ValidateEffect(EffectUnder(m_Body), "Field", m_Ids, m_Problems);

            Assert.AreEqual(0, m_Problems.Count);
        }

        [Test]
        public void ThenOneNotUnderAnEntityIsRefused()
        {
            FieldDatabase.ValidateEffect(EffectUnder(m_Loose), "Field", m_Ids, m_Problems);

            Assert.AreEqual(1, m_Problems.Count);
        }

        [Test]
        public void ThenAnIdAlreadyTakenIsRefused()
        {
            FieldDatabase.ValidateEffect(EffectUnder(m_Body), "Field", m_Ids, m_Problems);
            FieldDatabase.ValidateEffect(EffectUnder(m_Body), "Field", m_Ids, m_Problems);

            Assert.AreEqual(1, m_Problems.Count);
        }

        private static FieldEffect EffectUnder(GameObject parent)
        {
            GameObject effect = new GameObject("Effect");

            effect.transform.SetParent(parent.transform, false);

            return effect.AddComponent<FieldEffect>();
        }
    }
}
