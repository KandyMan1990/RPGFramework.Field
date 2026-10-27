using System;
using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Battle.SharedTypes;
using RPGFramework.Core.Input;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.Editor;
using RPGFramework.Field.Tests.Vm;

namespace RPGFramework.Field.Tests.Catalogue
{
    /// <summary>
    /// An opcode's width and order live in two places: the attributes the compiler writes from, and the reads its
    /// handler makes. Each sequential opcode's new block is compiled and run, with the field answering every wait at
    /// once, and the instruction after it must run. Opcodes naming a variable or a field resolve them against the
    /// project's assets, and jumps need a label, so neither is run here. RETURN and RETURN_TO_SCRIPT end the script
    /// on purpose; what they do is tested where scripts are scheduled.
    /// </summary>
    internal sealed class WhenAnOpcodesNewBlockRuns
    {
        private const int    FRAMES = 8;
        private const ushort MARK   = 1000;

        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();

            m_Field.Vm.IsMusicPlaying        = Never;
            m_Field.Vm.IsMusicVolumeFading   = Never;
            m_Field.Vm.IsScreenFading        = Never;
            m_Field.Vm.IsScreenColourFading  = Never;
            m_Field.Vm.IsEntityRotating      = Never;
            m_Field.Vm.IsEntityMoving        = Never;
            m_Field.Vm.IsEntityAnimating     = Never;
            m_Field.Vm.IsDialogueWindowOpen  = Never;
            m_Field.Vm.GetLastBattleResult   = Won;
            m_Field.Vm.StartWaitingForButton = AlreadyPressed;

            // Entity 0, which a new block's entity arguments name.
            m_Field.AddEntity(0, new byte[1], m_Field.Add(new Script().Return()));
        }

        [TestCaseSource(nameof(Runnable))]
        public void ThenItCompiles(string scriptName)
        {
            Assert.DoesNotThrow(() => FieldScriptCompiler.Compile(NewLine(scriptName)));
        }

        [TestCaseSource(nameof(Runnable))]
        public void ThenTheVmReadsExactlyTheBytesWritten(string scriptName)
        {
            byte[]             block  = FieldScriptCompiler.Compile(NewLine(scriptName));
            FieldEntityRuntime entity = m_Field.Start(new Script().Bytes(block).SetByte(MARK, 1).Return());

            for (int i = 0; i < FRAMES && m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK) == 0; i++)
            {
                m_Field.Tick(entity);
            }

            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        private static IEnumerable<string> Runnable()
        {
            IReadOnlyList<FieldOpCodeInfo> all      = FieldOpCodeCatalogue.All;
            List<string>                   runnable = new List<string>();

            for (int i = 0; i < all.Count; i++)
            {
                if (IsRunnable(all[i]))
                {
                    runnable.Add(all[i].ScriptName);
                }
            }

            return runnable;
        }

        private static bool IsRunnable(FieldOpCodeInfo info)
        {
            if (info.Layout != ArgumentLayout.Sequential)
            {
                return false;
            }

            if (info.OpCode == FieldScriptOpCode.Return || info.OpCode == FieldScriptOpCode.ReturnToAnotherScript)
            {
                return false;
            }

            for (int i = 0; i < info.Arguments.Count; i++)
            {
                ArgumentType type = info.Arguments[i].Type;

                if (type == ArgumentType.Variable || type == ArgumentType.Value || type == ArgumentType.FieldName || type == ArgumentType.Label)
                {
                    return false;
                }
            }

            return true;
        }

        private static string NewLine(string scriptName)
        {
            FieldOpCodeCatalogue.TryGet(scriptName, out FieldOpCodeInfo info);

            FieldScriptBlock block = FieldScriptBlock.CreateEmpty(info);
            block.PickFirstChoices(new OneChoiceEach());

            return block.ToLine();
        }

        private static bool Never()
        {
            return false;
        }

        private static bool Never(int entityId)
        {
            return false;
        }

        private static bool Never(byte channel)
        {
            return false;
        }

        private static BattleCompleteState Won()
        {
            return BattleCompleteState.VICTORY;
        }

        private static Func<bool> AlreadyPressed(ControlSlot button)
        {
            return Pressed;
        }

        private static bool Pressed()
        {
            return true;
        }
    }
}
