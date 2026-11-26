using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using UnityEngine;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>
    /// Party slot 1 played by entity 7 and slot 2 by no one; each opcode naming a party slot, followed by an instruction
    /// marking that the script went on.
    /// </summary>
    internal sealed class WhenAScriptActsOnAPartyMember
    {
        private const int    MEMBER = 7;
        private const ushort X      = 40;
        private const ushort Y      = 44;
        private const ushort Z      = 48;
        private const ushort MARK   = 60;

        private TestField m_Field;
        private int       m_Turned;
        private int       m_LookedAt;

        [SetUp]
        public void SetUp()
        {
            m_Field    = new TestField();
            m_Turned   = FieldEntity.NO_ENTITY;
            m_LookedAt = FieldEntity.NO_ENTITY;

            m_Field.Vm.GetPartyMemberEntity      =  slot => slot == 1 ? MEMBER : FieldEntity.NO_ENTITY;
            m_Field.Vm.GetEntityPosition         =  entityId => entityId == MEMBER ? new Vector3(1f, 2f, 3f) : Vector3.zero;
            m_Field.Vm.GetEntityDirection        =  entityId => entityId == MEMBER ? 90f : 0f;
            m_Field.Vm.IsEntityRotating          =  entityId => false;
            m_Field.Vm.IsEntityHeadTurning       =  entityId => false;
            m_Field.Vm.RequestTurnEntityToEntity += (entityId, target, seconds, easing) => m_Turned = target;
            m_Field.Vm.RequestHeadLookAtEntity   += (entityId, target, seconds, follow) => m_LookedAt = target;
        }

        [Test]
        public void ThenATurnFacesTheMembersEntity()
        {
            Run(Turn(1));

            Assert.AreEqual(MEMBER, m_Turned);
        }

        [Test]
        public void ThenATurnToAnEmptySlotDoesNothing()
        {
            Run(Turn(2));

            Assert.AreEqual(FieldEntity.NO_ENTITY, m_Turned);
        }

        [Test]
        public void ThenAHeadLooksAtTheMembersEntity()
        {
            Run(Head(FieldScriptOpCode.HeadLookAtPartyMember, 1));

            Assert.AreEqual(MEMBER, m_LookedAt);
        }

        [Test]
        public void ThenAHeadWaitingToLookAtAnEmptySlotDoesNothing()
        {
            Run(Head(FieldScriptOpCode.HeadLookAtPartyMemberAndWait, 2));

            Assert.AreEqual(FieldEntity.NO_ENTITY, m_LookedAt);
        }

        [Test]
        public void ThenTheMembersPositionIsStored()
        {
            Run(Position(1));

            Assert.AreEqual(new Vector3(1f, 2f, 3f), Stored());
        }

        [Test]
        public void ThenAnEmptySlotsPositionLeavesTheVariables()
        {
            m_Field.Memory.WriteFloat(MemoryBank.Persistent, X, 5f);

            Run(Position(2));

            Assert.AreEqual(new Vector3(5f, 0f, 0f), Stored());
        }

        [Test]
        public void ThenTheMembersDirectionIsStored()
        {
            Run(new Script().Op(FieldScriptOpCode.GetPartyMemberDirection).Sources(Source.Immediate, Source.Persistent).Byte(1).UShort(X));

            Assert.AreEqual(90f, m_Field.Memory.ReadFloat(MemoryBank.Persistent, X));
        }

        private static Script Turn(byte slot)
        {
            return new Script().Op(FieldScriptOpCode.TurnToPartyMemberAndWait).Sources(Source.Immediate, Source.Immediate).Byte(slot).Float(0.5f)
                               .Sources(Source.Immediate, Source.Immediate).Byte(0);
        }

        private static Script Head(FieldScriptOpCode opCode, byte slot)
        {
            return new Script().Op(opCode).Sources(Source.Immediate, Source.Immediate).Byte(slot).Float(0.5f)
                               .Sources(Source.Immediate, Source.Immediate).Bool(false);
        }

        private static Script Position(byte slot)
        {
            return new Script().Op(FieldScriptOpCode.GetPartyMemberPosition).Sources(Source.Immediate, Source.Persistent).Byte(slot).UShort(X)
                               .Sources(Source.Persistent, Source.Persistent).UShort(Y).UShort(Z);
        }

        private Vector3 Stored()
        {
            return new Vector3(m_Field.Memory.ReadFloat(MemoryBank.Persistent, X), m_Field.Memory.ReadFloat(MemoryBank.Persistent, Y), m_Field.Memory.ReadFloat(MemoryBank.Persistent, Z));
        }

        private void Run(Script script)
        {
            FieldEntityRuntime entity = m_Field.Start(script.SetByte(MARK, 1).Return());

            for (int i = 0; i < 4 && m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK) == 0; i++)
            {
                m_Field.Tick(entity);
            }

            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK), "the instruction after it ran");
        }
    }
}
