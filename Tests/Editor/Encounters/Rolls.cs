using System;
using System.Collections.Generic;

namespace RPGFramework.Field.Tests.Encounters
{
    internal static class Rolls
    {
        /// <summary>The rolls given, in order; asking for one more throws, failing the test.</summary>
        internal static Func<byte> Of(params byte[] rolls)
        {
            Queue<byte> queue = new Queue<byte>(rolls);

            return queue.Dequeue;
        }
    }
}
