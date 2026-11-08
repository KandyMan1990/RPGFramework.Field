using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using UnityEngine;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>An entity is told to face the point (1, 2, 3).</summary>
    internal sealed class WhenAnEntityFacesAPoint
    {
        private const ushort MARK = 1000;

        private TestField          m_Field;
        private FieldEntityRuntime m_Entity;
        private int                m_Facer;
        private Vector3            m_Point;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
            m_Facer = -1;
            m_Point = default;

            m_Field.Vm.RequestSetEntityToFacePosition += OnFace;

            m_Entity = m_Field.Run(new Script().Op(FieldScriptOpCode.SetDirectionToFacePosition)
                                               .Sources(Source.Immediate, Source.Immediate).Float(1f).Float(2f)
                                               .Sources(Source.Immediate, Source.Immediate).Float(3f)
                                               .SetByte(MARK, 1).Return());
        }

        [Test]
        public void ThenItIsThisEntityThatFaces()
        {
            Assert.AreEqual(m_Entity.EntityId, m_Facer);
        }

        [Test]
        public void ThenItFacesThePointGiven()
        {
            Assert.AreEqual(new Vector3(1f, 2f, 3f), m_Point);
        }

        [Test]
        public void ThenTheScriptCarriesOnAtOnce()
        {
            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        private void OnFace(int entityId, Vector3 point)
        {
            m_Facer = entityId;
            m_Point = point;
        }
    }
}
