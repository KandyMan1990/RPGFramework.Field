using System.Text.RegularExpressions;
using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using UnityEngine;
using UnityEngine.TestTools;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    /// <summary>
    /// A script compiled for another bytecode layout is refused rather than run as the wrong instructions, and so is
    /// one never exported, whose format reads as 0.
    /// </summary>
    internal sealed class WhenAScriptWasCompiledForAnotherFormat
    {
        private const int    SCRIPT = 500;
        private const ushort MARK   = 1000;

        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
        }

        [TestCase(0u)]
        [TestCase(FieldEntities.BYTECODE_FORMAT_VERSION + 1)]
        public void ThenItDoesNotRun(uint formatVersion)
        {
            Load(formatVersion);

            Assert.AreEqual(0, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        [TestCase(0u)]
        [TestCase(FieldEntities.BYTECODE_FORMAT_VERSION + 1)]
        public void ThenItsEntityGoesQuiet(uint formatVersion)
        {
            FieldEntityRuntime entity = Load(formatVersion);

            Assert.IsFalse(entity.IsSlotOccupied((byte)FieldScriptPriority.Main));
        }

        private FieldEntityRuntime Load(uint formatVersion)
        {
            LogAssert.Expect(LogType.Error, new Regex("was compiled for bytecode format"));

            m_Field.Vm.RegisterScript(SCRIPT, formatVersion, new Script().SetByte(MARK, 1).Return().ToArray());

            FieldEntityRuntime entity = m_Field.AddEntity(1, new byte[1], SCRIPT);

            m_Field.Tick(entity);

            return entity;
        }
    }
}
