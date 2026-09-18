using System.Threading;
using RPGFramework.Core.Dialogue;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// One of a field's dialogue windows. A channel keeps its rectangle and style for every message shown on it,
    /// so a script sets them once and then only names the channel. It shows one thing at a time.
    /// </summary>
    internal sealed class FieldDialogueChannel
    {
        internal bool                HasRect;
        internal RectInt             Rect;
        internal DialogueWindowStyle Style;

        /// <summary>
        /// The window showing on this channel, or null when it is free.
        /// </summary>
        internal IDialogueWindow Window;

        /// <summary>
        /// Cancelled to close <see cref="Window" /> early, by <c>CLOSE_DIALOGUE_WINDOW</c>.
        /// </summary>
        internal CancellationTokenSource Close;

        internal void Reset()
        {
            HasRect = false;
            Rect    = default;
            Style   = DialogueWindowStyle.Spoken;
            Window  = null;
            Close   = null;
        }
    }
}