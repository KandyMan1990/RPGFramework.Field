using NUnit.Framework;
using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field.Tests.Entities
{
    /// <summary>An offset of (2, 0, 0), moved to (4, 0, 0).</summary>
    internal sealed class WhenADrawOffsetMoves
    {
        private static readonly Vector3 m_Start  = new Vector3(2f, 0f, 0f);
        private static readonly Vector3 m_Target = new Vector3(4f, 0f, 0f);

        private DrawOffset m_Offset;

        [SetUp]
        public void SetUp()
        {
            m_Offset = default;
            m_Offset.MoveTo(m_Start, 0f, Easing.Linear);
        }

        [Test]
        public void ThenNoTimeGoesThereAtOnce()
        {
            Assert.AreEqual(m_Start, m_Offset.Current);
            Assert.IsFalse(m_Offset.IsMoving);
        }

        [Test]
        public void ThenItMovesFromTheOffsetItHas()
        {
            m_Offset.MoveTo(m_Target, 2f, Easing.Linear);
            m_Offset.Tick(1f);

            Assert.AreEqual(new Vector3(3f, 0f, 0f), m_Offset.Current);
        }

        [Test]
        public void ThenItIsMovingUntilItArrives()
        {
            m_Offset.MoveTo(m_Target, 2f, Easing.Linear);
            m_Offset.Tick(1f);

            Assert.IsTrue(m_Offset.IsMoving);
        }

        [Test]
        public void ThenItArrivesExactly()
        {
            m_Offset.MoveTo(m_Target, 2f, Easing.Smooth);
            m_Offset.Tick(5f);

            Assert.AreEqual(m_Target, m_Offset.Current);
            Assert.IsFalse(m_Offset.IsMoving);
        }

        [Test]
        public void ThenSmoothStartsMoreSlowly()
        {
            m_Offset.MoveTo(m_Target, 2f, Easing.Smooth);
            m_Offset.Tick(0.2f);

            Assert.Less(m_Offset.Current.x, 2.2f);
        }
    }
}
