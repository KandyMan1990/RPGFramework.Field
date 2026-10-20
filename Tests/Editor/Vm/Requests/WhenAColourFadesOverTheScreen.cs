using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using UnityEngine;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    internal sealed class WhenAColourFadesOverTheScreen
    {
        private const ushort MARK = 1000;

        private TestField m_Field;
        private Color     m_Colour;
        private bool      m_Added;
        private float     m_Seconds;

        [SetUp]
        public void SetUp()
        {
            m_Field   = new TestField();
            m_Colour  = default;
            m_Added   = false;
            m_Seconds = 0f;

            m_Field.Vm.RequestScreenColour += OnColour;
        }

        [TestCase(FieldScriptOpCode.SubtractiveScreenFade)]
        [TestCase(FieldScriptOpCode.AdditiveScreenFade)]
        public void ThenItAsksForTheColour(FieldScriptOpCode opCode)
        {
            m_Field.Run(Colour(opCode).Return());

            Assert.AreEqual(new Color(0.25f, 0.5f, 0.75f), m_Colour);
        }

        [TestCase(FieldScriptOpCode.SubtractiveScreenFade)]
        [TestCase(FieldScriptOpCode.AdditiveScreenFade)]
        public void ThenItAsksForItsSeconds(FieldScriptOpCode opCode)
        {
            m_Field.Run(Colour(opCode).Return());

            Assert.AreEqual(1.5f, m_Seconds);
        }

        [TestCase(FieldScriptOpCode.SubtractiveScreenFade, false)]
        [TestCase(FieldScriptOpCode.AdditiveScreenFade,    true)]
        public void ThenItAsksForItTakenOrAdded(FieldScriptOpCode opCode, bool added)
        {
            m_Field.Run(Colour(opCode).Return());

            Assert.AreEqual(added, m_Added);
        }

        [Test]
        public void ThenItsArgumentsAreReadExactly()
        {
            m_Field.Run(Colour(FieldScriptOpCode.SubtractiveScreenFade).SetByte(MARK, 1).Return());

            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        private static Script Colour(FieldScriptOpCode opCode)
        {
            return new Script().Op(opCode).Sources(Source.Immediate, Source.Immediate).Float(0.25f).Float(0.5f)
                               .Sources(Source.Immediate, Source.Immediate).Float(0.75f).Float(1.5f);
        }

        private void OnColour(Color colour, bool added, float seconds)
        {
            m_Colour  = colour;
            m_Added   = added;
            m_Seconds = seconds;
        }
    }
}
