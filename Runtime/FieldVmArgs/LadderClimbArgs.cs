using UnityEngine;

namespace RPGFramework.Field.FieldVmArgs
{
    /// <summary>
    /// A ladder as a script gives it, its points in the order they are reached: where getting on ends, where the climb
    /// ends, and where getting off ends.
    /// </summary>
    internal readonly struct LadderClimbArgs
    {
        internal readonly bool    GoingUp;
        internal readonly Vector3 OnLadder;
        internal readonly Vector3 OffLadder;
        internal readonly Vector3 Finish;

        internal LadderClimbArgs(bool goingUp, Vector3 onLadder, Vector3 offLadder, Vector3 finish)
        {
            GoingUp   = goingUp;
            OnLadder  = onLadder;
            OffLadder = offLadder;
            Finish    = finish;
        }
    }
}
