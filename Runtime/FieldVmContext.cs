using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace RPGFramework.Field
{
    internal struct FieldVmContext
    {
        internal readonly byte[] Script;
        internal          int    InstructionPointer;

        internal readonly FieldInstance Field;

        internal bool  IsActive;
        internal bool  IsSuspended;
        internal float WaitSeconds;

        private Task m_BlockTask;

        private readonly Stack<int> m_CallStack;

        internal bool HasReturnAddress => m_CallStack.Count > 0;

        internal FieldVmContext(byte[] script, int instructionPointer, FieldInstance field)
        {
            Script             = script;
            InstructionPointer = instructionPointer;

            Field = field;

            IsActive    = false;
            IsSuspended = false;
            WaitSeconds = 0f;

            m_BlockTask = null;

            m_CallStack = new Stack<int>(4);
        }

        internal void PushReturnAddress(int address)
        {
            m_CallStack.Push(address);
        }

        internal int PopReturnAddress()
        {
            return m_CallStack.Pop();
        }

        internal void BlockUntil(Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException($"{nameof(FieldVmContext)}::{nameof(BlockUntil)} task cannot be null");
            }

            if (m_BlockTask != null)
            {
                throw new InvalidOperationException($"{nameof(FieldVmContext)}::{nameof(BlockUntil)} BlockTask is already set");
            }

            m_BlockTask = task;
            IsSuspended = true;
        }

        internal bool TryResume()
        {
            if (m_BlockTask != null)
            {
                if (m_BlockTask.IsFaulted)
                {
                    Debug.LogException(m_BlockTask.Exception);
                }

                if (!m_BlockTask.IsCompleted)
                {
                    return false;
                }

                ClearBlockTask();
            }

            if (WaitSeconds > 0f)
            {
                return false;
            }

            IsSuspended = false;

            return true;
        }

        internal void ResetBlockingState()
        {
            IsSuspended = false;
            WaitSeconds = 0f;
            ClearBlockTask();
        }

        private void ClearBlockTask()
        {
            m_BlockTask = null;
        }
    }
}