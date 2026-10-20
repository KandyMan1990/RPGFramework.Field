using System;
using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Compiler
{
    /// <summary>A request is its opcode, a sources byte, the entity and its event; the slot is the script type's own.</summary>
    internal sealed class WhenAScriptIsRequestedByAnother
    {
        private byte[] m_Request;

        [SetUp]
        public void SetUp()
        {
            m_Request = FieldScriptCompiler.Compile("REQUEST_SCRIPT 3 12");
        }

        [Test]
        public void ThenItIsSixBytes()
        {
            Assert.AreEqual(6, m_Request.Length);
        }

        [Test]
        public void ThenItNamesTheEntity()
        {
            Assert.AreEqual(3, m_Request[3]);
        }

        [Test]
        public void ThenItNamesTheEvent()
        {
            Assert.AreEqual(12, BitConverter.ToUInt16(m_Request, 4));
        }

        [Test]
        public void ThenItsValuesAreMarkedAsWritten()
        {
            Assert.AreEqual(0, m_Request[2]);
        }

        [Test]
        public void ThenAnEventPast255IsKept()
        {
            Assert.AreEqual(5000, BitConverter.ToUInt16(FieldScriptCompiler.Compile("REQUEST_SCRIPT_WAIT_END 1 5000"), 4));
        }
    }
}
