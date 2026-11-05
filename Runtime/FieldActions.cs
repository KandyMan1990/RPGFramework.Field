using RPGFramework.Core.Input;
using RPGFramework.Hashing;
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
#endif

namespace RPGFramework.Field
{
    /// <summary>
    /// The actions the field reads beside Core's, each the hash of the name a game gives it, and every action the field
    /// reads, declared for the editor's check of the field scene's map.
    /// </summary>
    internal sealed class FieldActions
#if UNITY_EDITOR
        : IRequiredActions
#endif
    {
        /// <summary>The name of the action that opens the party menu.</summary>
        internal const string MENU = "Menu";

        internal static readonly ulong Menu = Fnv1a64.Hash(MENU);

#if UNITY_EDITOR
        private static readonly RequiredAction[] m_Required =
        {
            new RequiredAction(CoreActions.CONFIRM, RequiredActionKind.Button, "talks to whoever is ahead, and turns a dialogue window's pages"),
            new RequiredAction(CoreActions.CANCEL, RequiredActionKind.Button, "moves a question's cursor to its cancel answer"),
            new RequiredAction(MENU, RequiredActionKind.Button, "opens the party menu"),
#if ENABLE_RPG_FRAMEWORK_DEBUG
            new RequiredAction(DebugRoomShortcut.SHOULDER_LEFT, RequiredActionKind.Button, DebugRoomShortcut.DESCRIPTION),
            new RequiredAction(DebugRoomShortcut.TRIGGER_LEFT, RequiredActionKind.Button, DebugRoomShortcut.DESCRIPTION),
            new RequiredAction(DebugRoomShortcut.SHOULDER_RIGHT, RequiredActionKind.Button, DebugRoomShortcut.DESCRIPTION),
            new RequiredAction(DebugRoomShortcut.TRIGGER_RIGHT, RequiredActionKind.Button, DebugRoomShortcut.DESCRIPTION),
            new RequiredAction(DebugRoomShortcut.SELECT, RequiredActionKind.Button, DebugRoomShortcut.DESCRIPTION),
#endif
        };

        Type IRequiredActions.Module => typeof(FieldModuleMonoBehaviour);

        string IRequiredActions.Map => null;

        bool IRequiredActions.Moves => true;

        IReadOnlyList<RequiredAction> IRequiredActions.Actions => m_Required;
#endif
    }
}
