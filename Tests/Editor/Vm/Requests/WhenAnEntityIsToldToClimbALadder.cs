using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>An entity goes up a ladder through three points, and is on it unless a test says otherwise.</summary>
    internal sealed class WhenAnEntityIsToldToClimbALadder
    {
        private const ushort MARK = 1000;

        private TestField          m_Field;
        private FieldEntityRuntime m_Entity;
        private bool               m_Climbing;
        private int                m_Climber;
        private LadderClimbArgs    m_Args;

        [SetUp]
        public void SetUp()
        {
            m_Field    = new TestField();
            m_Climbing = true;
            m_Climber  = -1;
            m_Args     = default;

            m_Field.Vm.IsEntityClimbing   =  IsClimbing;
            m_Field.Vm.RequestClimbLadder += OnClimb;

            m_Entity = m_Field.Run(new Script().Op(FieldScriptOpCode.ClimbLadderAndWait)
                                               .Sources(Source.Immediate, Source.Immediate).Bool(true).Float(1f)
                                               .Sources(Source.Immediate, Source.Immediate).Float(2f).Float(3f)
                                               .Sources(Source.Immediate, Source.Immediate).Float(4f).Float(5f)
                                               .Sources(Source.Immediate, Source.Immediate).Float(6f).Float(7f)
                                               .Sources(Source.Immediate, Source.Immediate).Float(8f).Float(9f)
                                               .SetByte(MARK, 1).Return());
        }

        [Test]
        public void ThenItIsThisEntityThatClimbs()
        {
            Assert.AreEqual(m_Entity.EntityId, m_Climber);
        }

        [Test]
        public void ThenItGoesTheWayGiven()
        {
            Assert.IsTrue(m_Args.GoingUp);
        }

        [Test]
        public void ThenGettingOnEndsAtTheFirstPoint()
        {
            Assert.AreEqual(new Vector3(1f, 2f, 3f), m_Args.OnLadder);
        }

        [Test]
        public void ThenTheClimbEndsAtTheSecondPoint()
        {
            Assert.AreEqual(new Vector3(4f, 5f, 6f), m_Args.OffLadder);
        }

        [Test]
        public void ThenGettingOffEndsAtTheThirdPoint()
        {
            Assert.AreEqual(new Vector3(7f, 8f, 9f), m_Args.Finish);
        }

        [Test]
        public void ThenTheScriptWaitsWhileItIsOnTheLadder()
        {
            m_Field.Tick(m_Entity, 3);

            Assert.AreEqual(0, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        [Test]
        public void ThenTheScriptCarriesOnOnceItIsOff()
        {
            m_Climbing = false;
            m_Field.Tick(m_Entity, 2);

            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        private bool IsClimbing(int entityId)
        {
            return m_Climbing;
        }

        private void OnClimb(int entityId, LadderClimbArgs args)
        {
            m_Climber = entityId;
            m_Args    = args;
        }
    }
}
