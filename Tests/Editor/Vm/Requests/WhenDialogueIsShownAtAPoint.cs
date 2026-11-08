using System;
using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>A window on channel 1 at (60, 80), sized to its text, the channel free unless a test says otherwise.</summary>
    internal sealed class WhenDialogueIsShownAtAPoint
    {
        private const byte   CHANNEL = 1;
        private const ushort ANSWER  = 300;

        private TestField    m_Field;
        private List<string> m_Asked;
        private bool         m_Busy;
        private byte         m_FitChannel;
        private Vector2Int   m_FitPosition;
        private bool         m_Flag;
        private QuestionArgs m_Question;

        [SetUp]
        public void SetUp()
        {
            m_Field       = new TestField();
            m_Asked       = new List<string>();
            m_Busy        = false;
            m_FitChannel  = 0;
            m_FitPosition = default;
            m_Flag        = false;
            m_Question    = default;

            m_Field.Vm.IsDialogueWindowOpen            =  IsOpen;
            m_Field.Vm.RequestFitDialogueWindow        += OnFit;
            m_Field.Vm.RequestShowDialogueWindow       += OnShown;
            m_Field.Vm.RequestShowDialogueWindowNoWait += OnShown;
            m_Field.Vm.RequestAskPlayerToMakeAChoice   += OnAsked;
        }

        [Test]
        public void ThenTheWindowIsFittedAtThePoint()
        {
            m_Field.Run(ShowAndWait(true));

            Assert.AreEqual(CHANNEL, m_FitChannel);
            Assert.AreEqual(new Vector2Int(60, 80), m_FitPosition);
        }

        [Test]
        public void ThenItIsFittedBeforeItIsShown()
        {
            m_Field.Run(ShowAndWait(true));

            CollectionAssert.AreEqual(new[] { "fit", "shown" }, m_Asked);
        }

        [Test]
        public void ThenItsBlockMovementIsPassedOn()
        {
            m_Field.Run(ShowAndWait(true));

            Assert.IsTrue(m_Flag);
        }

        [Test]
        public void ThenWithoutWaitingIsNotDismissible()
        {
            m_Field.Run(ShowWithoutWaiting(FieldScriptOpCode.ShowDialogueAtNoWait));

            Assert.AreEqual(new Vector2Int(60, 80), m_FitPosition);
            Assert.IsFalse(m_Flag);
        }

        [Test]
        public void ThenTheDismissibleOneIs()
        {
            m_Field.Run(ShowWithoutWaiting(FieldScriptOpCode.ShowDialogueAtNoWaitDismissible));

            Assert.AreEqual(new Vector2Int(60, 80), m_FitPosition);
            Assert.IsTrue(m_Flag);
        }

        [Test]
        public void ThenABusyChannelIsWaitedForBeforeFitting()
        {
            m_Busy = true;

            m_Field.Run(ShowAndWait(true));

            CollectionAssert.IsEmpty(m_Asked);
        }

        [Test]
        public void ThenAQuestionIsFittedBeforeItIsAsked()
        {
            m_Field.Run(Ask());

            CollectionAssert.AreEqual(new[] { "fit", "asked" }, m_Asked);
            Assert.AreEqual(new Vector2Int(60, 80), m_FitPosition);
        }

        [Test]
        public void ThenAQuestionKeepsItsAnswers()
        {
            m_Field.Run(Ask());

            Assert.AreEqual(2, m_Question.AnswerIds.Length);
            Assert.AreEqual(1, m_Question.StartingAnswer);
        }

        private static Script ShowAndWait(bool blockMovement)
        {
            return new Script().Op(FieldScriptOpCode.ShowDialogueAt).Byte(CHANNEL).ULong(10)
                               .Sources(Source.Immediate, Source.Immediate).Int(60).Int(80)
                               .Sources(Source.Immediate, Source.Immediate).Bool(blockMovement).Return();
        }

        private static Script ShowWithoutWaiting(FieldScriptOpCode opCode)
        {
            return new Script().Op(opCode).Byte(CHANNEL).ULong(10)
                               .Sources(Source.Immediate, Source.Immediate).Int(60).Int(80).Return();
        }

        private static Script Ask()
        {
            return new Script().Op(FieldScriptOpCode.AskPlayerAt).Byte((byte)MemoryBank.Persistent).UShort(ANSWER).Byte(CHANNEL)
                               .Sources(Source.Immediate, Source.Immediate).Int(60).Int(80)
                               .ULong(10).Byte(1).Byte(0).Byte(2).ULong(20).ULong(30).Return();
        }

        private bool IsOpen(byte channel)
        {
            return m_Busy;
        }

        private void OnFit(byte channel, Vector2Int position)
        {
            m_Asked.Add("fit");
            m_FitChannel  = channel;
            m_FitPosition = position;
        }

        private void OnShown(byte channel, ulong dialogueId, bool flag)
        {
            m_Asked.Add("shown");
            m_Flag = flag;
        }

        private void OnAsked(QuestionArgs question, Action<byte> storeChoice)
        {
            m_Asked.Add("asked");
            m_Question = question;
        }
    }
}
