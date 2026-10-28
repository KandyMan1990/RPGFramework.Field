using System;
using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.FieldVmArgs;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>A question of two answers on channel 1, its cursor starting on the second and cancelling to the first.</summary>
    internal sealed class WhenAQuestionIsAsked
    {
        private const ushort ANSWER   = 300;
        private const byte   CHANNEL  = 1;
        private const byte   STARTING = 1;
        private const byte   CANCEL   = 0;

        private TestField    m_Field;
        private QuestionArgs m_Asked;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
            m_Asked = default;

            m_Field.Vm.IsDialogueWindowOpen          =  IsOpen;
            m_Field.Vm.RequestAskPlayerToMakeAChoice += OnAsked;

            m_Field.Run(new Script().Op(FieldScriptOpCode.AskPlayerToMakeAChoice).Byte((byte)MemoryBank.Persistent).UShort(ANSWER)
                                    .Byte(CHANNEL).ULong(10).Byte(STARTING).Byte(CANCEL).Byte(2).ULong(20).ULong(30).Return());
        }

        [Test]
        public void ThenTheFieldIsGivenItsStartingAnswer()
        {
            Assert.AreEqual(STARTING, m_Asked.StartingAnswer);
        }

        [Test]
        public void ThenTheFieldIsGivenItsCancelAnswer()
        {
            Assert.AreEqual(CANCEL, m_Asked.CancelAnswer);
        }

        [Test]
        public void ThenTheFieldIsGivenItsAnswersAfterThem()
        {
            CollectionAssert.AreEqual(new ulong[] { 20, 30 }, m_Asked.AnswerIds);
        }

        private static bool IsOpen(byte channel)
        {
            return false;
        }

        private void OnAsked(QuestionArgs question, Action<byte> storeChoice)
        {
            m_Asked = question;
        }
    }
}
