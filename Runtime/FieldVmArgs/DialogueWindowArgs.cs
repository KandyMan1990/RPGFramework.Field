using UnityEngine;

namespace RPGFramework.Field.FieldVmArgs
{
    internal readonly struct DialogueWindowArgs
    {
        internal readonly ulong   DialogueId;
        internal readonly RectInt Rect;

        internal DialogueWindowArgs(ulong dialogueId, RectInt rect)
        {
            DialogueId = dialogueId;
            Rect       = rect;
        }
    }
}