using UnityEngine;

namespace RPGFramework.Field.FieldVmArgs
{
    internal readonly struct DialogueWindowArgs
    {
        internal readonly byte    Channel;
        internal readonly RectInt Rect;

        internal DialogueWindowArgs(byte channel, RectInt rect)
        {
            Channel = channel;
            Rect    = rect;
        }
    }
}