using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using UnityEngine;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>A script sending its entity's head somewhere, or saying how far it may go.</summary>
    internal sealed class WhenAScriptTurnsAHead
    {
        private const byte   TARGET  = 7;
        private const float  SECONDS = 1.5f;
        private const ushort MARK    = 1000;

        private TestField m_Field;
        private int       m_Entity;
        private int       m_Target;
        private float     m_Seconds;
        private bool      m_Follow;
        private Vector3   m_Point;
        private float     m_Yaw;
        private float     m_Pitch;
        private bool      m_Turning;

        [SetUp]
        public void SetUp()
        {
            m_Field   = new TestField();
            m_Entity  = -1;
            m_Target  = -1;
            m_Seconds = 0f;
            m_Follow  = false;
            m_Point   = default;
            m_Yaw     = 0f;
            m_Pitch   = 0f;
            m_Turning = true;

            m_Field.Vm.RequestHeadLookAtEntity   += OnLookAtEntity;
            m_Field.Vm.RequestHeadLookAtPosition += OnLookAtPosition;
            m_Field.Vm.RequestHeadLookForward    += OnLookForward;
            m_Field.Vm.RequestSetHeadLimits      += OnLimits;
            m_Field.Vm.IsEntityHeadTurning       =  IsTurning;
        }

        [Test]
        public void ThenLookingAtAnEntityPassesOnWhoAndHowLong()
        {
            FieldEntityRuntime entity = m_Field.Run(LookAtEntity(new Script(), FieldScriptOpCode.HeadLookAtEntity, true).Return());

            Assert.AreEqual(entity.EntityId, m_Entity);
            Assert.AreEqual(TARGET, m_Target);
            Assert.AreEqual(SECONDS, m_Seconds);
            Assert.IsTrue(m_Follow);
        }

        [Test]
        public void ThenLookingAtAnEntityNeedNotFollowIt()
        {
            m_Follow = true;

            m_Field.Run(LookAtEntity(new Script(), FieldScriptOpCode.HeadLookAtEntity, false).Return());

            Assert.IsFalse(m_Follow);
        }

        [Test]
        public void ThenLookingAtAnEntityCarriesOn()
        {
            m_Field.Run(LookAtEntity(new Script(), FieldScriptOpCode.HeadLookAtEntity, false).SetByte(MARK, 1).Return());

            Assert.AreEqual(1, Mark());
        }

        [Test]
        public void ThenLookingAtAPointPassesOnWhereAndHowLong()
        {
            FieldEntityRuntime entity = m_Field.Run(LookAtPoint(new Script(), FieldScriptOpCode.HeadLookAtPosition).Return());

            Assert.AreEqual(entity.EntityId, m_Entity);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), m_Point);
            Assert.AreEqual(SECONDS, m_Seconds);
        }

        [Test]
        public void ThenLookingForwardPassesOnHowLong()
        {
            FieldEntityRuntime entity = m_Field.Run(LookForward(new Script(), FieldScriptOpCode.HeadLookForward).Return());

            Assert.AreEqual(entity.EntityId, m_Entity);
            Assert.AreEqual(SECONDS, m_Seconds);
        }

        [Test]
        public void ThenLimitsArePassedOn()
        {
            FieldEntityRuntime entity = m_Field.Run(new Script().Op(FieldScriptOpCode.SetHeadLimits)
                                                                .Sources(Source.Immediate, Source.Immediate).Float(20f).Float(10f).Return());

            Assert.AreEqual(entity.EntityId, m_Entity);
            Assert.AreEqual(20f, m_Yaw);
            Assert.AreEqual(10f, m_Pitch);
        }

        [Test]
        public void ThenWaitingForTheHeadHoldsTheScriptWhileItTurns()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.WaitForHead).SetByte(MARK, 1).Return());

            Assert.AreEqual(0, Mark());
        }

        [Test]
        public void ThenWaitingForTheHeadEndsWhenItHasTurned()
        {
            FieldEntityRuntime entity = m_Field.Run(new Script().Op(FieldScriptOpCode.WaitForHead).SetByte(MARK, 1).Return());

            m_Turning = false;

            m_Field.Tick(entity, 3);

            Assert.AreEqual(1, Mark());
        }

        [Test]
        public void ThenTheWaitingFormForAnEntityHoldsTheScript()
        {
            m_Field.Run(LookAtEntity(new Script(), FieldScriptOpCode.HeadLookAtEntityAndWait, false).SetByte(MARK, 1).Return());

            Assert.AreEqual(TARGET, m_Target);
            Assert.AreEqual(0, Mark());
        }

        [Test]
        public void ThenTheWaitingFormForAPointHoldsTheScript()
        {
            m_Field.Run(LookAtPoint(new Script(), FieldScriptOpCode.HeadLookAtPositionAndWait).SetByte(MARK, 1).Return());

            Assert.AreEqual(new Vector3(1f, 2f, 3f), m_Point);
            Assert.AreEqual(0, Mark());
        }

        [Test]
        public void ThenTheWaitingFormForLookingForwardHoldsTheScript()
        {
            m_Field.Run(LookForward(new Script(), FieldScriptOpCode.HeadLookForwardAndWait).SetByte(MARK, 1).Return());

            Assert.AreEqual(SECONDS, m_Seconds);
            Assert.AreEqual(0, Mark());
        }

        private static Script LookAtEntity(Script script, FieldScriptOpCode opCode, bool follow)
        {
            return script.Op(opCode).Sources(Source.Immediate, Source.Immediate).Byte(TARGET).Float(SECONDS)
                         .Sources(Source.Immediate, Source.Immediate).Bool(follow);
        }

        private static Script LookAtPoint(Script script, FieldScriptOpCode opCode)
        {
            return script.Op(opCode).Sources(Source.Immediate, Source.Immediate).Float(1f).Float(2f)
                         .Sources(Source.Immediate, Source.Immediate).Float(3f).Float(SECONDS);
        }

        private static Script LookForward(Script script, FieldScriptOpCode opCode)
        {
            return script.Op(opCode).Sources(Source.Immediate, Source.Immediate).Float(SECONDS);
        }

        private int Mark()
        {
            return m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK);
        }

        private bool IsTurning(int entityId)
        {
            return m_Turning;
        }

        private void OnLookAtEntity(int entityId, int target, float seconds, bool follow)
        {
            m_Entity  = entityId;
            m_Target  = target;
            m_Seconds = seconds;
            m_Follow  = follow;
        }

        private void OnLookAtPosition(int entityId, Vector3 point, float seconds)
        {
            m_Entity  = entityId;
            m_Point   = point;
            m_Seconds = seconds;
        }

        private void OnLookForward(int entityId, float seconds)
        {
            m_Entity  = entityId;
            m_Seconds = seconds;
        }

        private void OnLimits(int entityId, float yaw, float pitch)
        {
            m_Entity = entityId;
            m_Yaw    = yaw;
            m_Pitch  = pitch;
        }
    }
}
