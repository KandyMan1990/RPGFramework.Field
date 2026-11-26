using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.FieldVmArgs;
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
        private int       m_WalkedTo;
        private int       m_Faced;
        private int       m_ScrolledTo;
        private Vector3?  m_JumpedTo;

        [SetUp]
        public void SetUp()
        {
            m_Field    = new TestField();
            m_Turned     = FieldEntity.NO_ENTITY;
            m_LookedAt   = FieldEntity.NO_ENTITY;
            m_WalkedTo   = FieldEntity.NO_ENTITY;
            m_Faced      = FieldEntity.NO_ENTITY;
            m_ScrolledTo = FieldEntity.NO_ENTITY;
            m_JumpedTo   = null;

            m_Field.Vm.GetPartyMemberEntity         =  slot => slot == 1 ? MEMBER : FieldEntity.NO_ENTITY;
            m_Field.Vm.GetEntityPosition            =  entityId => entityId == MEMBER ? new Vector3(1f, 2f, 3f) : Vector3.zero;
            m_Field.Vm.GetEntityDirection           =  entityId => entityId == MEMBER ? 90f : 0f;
            m_Field.Vm.IsEntityRotating             =  entityId => false;
            m_Field.Vm.IsEntityHeadTurning          =  entityId => false;
            m_Field.Vm.RequestTurnEntityToEntity    += (entityId, target, seconds, easing) => m_Turned = target;
            m_Field.Vm.RequestHeadLookAtEntity      += (entityId, target, seconds, follow) => m_LookedAt = target;
            m_Field.Vm.IsEntityMoving               =  entityId => false;
            m_Field.Vm.IsEntityJumping              =  entityId => false;
            m_Field.Vm.RequestMoveEntity            += (entityId, args) => m_WalkedTo = args.TargetEntityId;
            m_Field.Vm.RequestJumpEntity            += (entityId, target, seconds, height) => m_JumpedTo = target;
            m_Field.Vm.RequestSetEntityToFaceEntity += (entityId, target) => m_Faced = target;
            m_Field.Vm.RequestScrollScreenToEntity  += (target, seconds, easing) => m_ScrolledTo = target;
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

        [Test]
        public void ThenAWalkGoesUpToTheMembersEntity()
        {
            Run(Walk(FieldScriptOpCode.MoveEntityToPartyMemberAndWait, 1));

            Assert.AreEqual(MEMBER, m_WalkedTo);
        }

        [Test]
        public void ThenAWalkToAnEmptySlotDoesNothing()
        {
            Run(Walk(FieldScriptOpCode.MoveEntityToPartyMember, 2));

            Assert.AreEqual(FieldEntity.NO_ENTITY, m_WalkedTo);
        }

        [Test]
        public void ThenAJumpLandsWhereTheMemberStands()
        {
            Run(new Script().Op(FieldScriptOpCode.JumpEntityToPartyMemberAndWait).Sources(Source.Immediate, Source.Immediate).Byte(1).Float(0.5f)
                            .Sources(Source.Immediate, Source.Immediate).Float(1f));

            Assert.AreEqual(new Vector3(1f, 2f, 3f), m_JumpedTo);
        }

        [Test]
        public void ThenAJumpToAnEmptySlotDoesNothing()
        {
            Run(new Script().Op(FieldScriptOpCode.JumpEntityToPartyMemberAndWait).Sources(Source.Immediate, Source.Immediate).Byte(2).Float(0.5f)
                            .Sources(Source.Immediate, Source.Immediate).Float(1f));

            Assert.IsNull(m_JumpedTo);
        }

        [Test]
        public void ThenItFacesTheMembersEntity()
        {
            Run(new Script().Op(FieldScriptOpCode.SetDirectionToFacePartyMember).Sources(Source.Immediate, Source.Immediate).Byte(1));

            Assert.AreEqual(MEMBER, m_Faced);
        }

        [Test]
        public void ThenTheViewScrollsOntoTheMembersEntity()
        {
            Run(new Script().Op(FieldScriptOpCode.ScrollScreenToPartyMember).Sources(Source.Immediate, Source.Immediate).Byte(1).Float(0.5f)
                            .Sources(Source.Immediate, Source.Immediate).Byte(0));

            Assert.AreEqual(MEMBER, m_ScrolledTo);
        }

        private static Script Walk(FieldScriptOpCode opCode, byte slot)
        {
            return new Script().Op(opCode).Sources(Source.Immediate, Source.Immediate).Byte(slot).Float(1.5f);
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
