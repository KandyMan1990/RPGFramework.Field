using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.Tests.Vm;
using UnityEngine;
using UnityEngine.TestTools;

namespace RPGFramework.Field.Tests.GameOpCodes
{
    /// <summary>
    /// Nothing but the handler knows how many bytes its arguments take, so in the editor each read is checked against
    /// the opcode's attributes, and a handler that reads otherwise is logged and its script stopped rather than run on
    /// from the wrong byte.
    /// </summary>
    internal sealed class WhenAGameHandlerReadsOtherThanItsArguments
    {
        private const ushort MARK = 1000;

        private TestField                   m_Field;
        private Action<FieldScriptContext> m_Read;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField(new TestOpCodeDatabase().With(TestGameOpCode.TakeTwo, TakeTwoHandler));
            m_Read  = null;
        }

        [Test]
        public void ThenAReadOfTheWrongTypeIsLogged()
        {
            m_Read = ReadsAnIntFirst;

            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape("read argument 0 as Int, which its attributes describe as Byte")));

            m_Field.Run(TakeTwo());
        }

        [Test]
        public void ThenAnArgumentLeftUnreadIsLogged()
        {
            m_Read = ReadsOne;

            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape("left argument 1 of its 2 unread")));

            m_Field.Run(TakeTwo());
        }

        [Test]
        public void ThenAReadPastItsArgumentsIsLogged()
        {
            m_Read = ReadsThree;

            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape("read argument 2 as Int, but its attributes describe 2")));

            m_Field.Run(TakeTwo());
        }

        [Test]
        public void ThenTheScriptStopsAtIt()
        {
            m_Read = ReadsOne;

            LogAssert.Expect(LogType.Error, new Regex("unread"));

            m_Field.Run(TakeTwo());

            Assert.AreEqual(0, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        private static Script TakeTwo()
        {
            return new Script().UShort((ushort)TestGameOpCode.TakeTwo).Sources(Source.Immediate, Source.Immediate).Byte(7).Int(1234)
                               .SetByte(MARK, 1)
                               .Return();
        }

        private void TakeTwoHandler(FieldScriptContext context)
        {
            m_Read(context);
        }

        private static void ReadsAnIntFirst(FieldScriptContext context)
        {
            context.ReadInt();
            context.ReadInt();
        }

        private static void ReadsOne(FieldScriptContext context)
        {
            context.ReadByte();
        }

        private static void ReadsThree(FieldScriptContext context)
        {
            context.ReadByte();
            context.ReadInt();
            context.ReadInt();
        }
    }
}
