using RPGFramework.Field.FieldVmArgs;
using Unity.Mathematics;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>Which of a ladder's three animations an entity shows. Going down plays them backwards.</summary>
    internal enum LadderAnimation : byte
    {
        GetOn,
        Climb,
        GetOff
    }

    /// <summary>A ladder animation, and how far through it to show.</summary>
    internal readonly struct LadderPose
    {
        internal readonly LadderAnimation Animation;
        internal readonly float           NormalisedTime;

        internal LadderPose(LadderAnimation animation, float normalisedTime)
        {
            Animation      = animation;
            NormalisedTime = normalisedTime;
        }
    }

    /// <summary>
    /// An entity on a ladder: getting on, climbing, and getting off, or going back off where it began. Getting on and
    /// off play through by themselves, each as long as its animation; the climb is steered, and goes at the entity's
    /// speed. A part with no length is skipped with its animation.
    /// </summary>
    internal struct LadderClimb
    {
        private const float STEER_THRESHOLD = 0.5f;

        private enum Part : byte
        {
            Entering,
            Climbing,
            Leaving,
            BackingOff,
            Finished
        }

        private readonly Vector3 m_Start;
        private readonly Vector3 m_OnLadder;
        private readonly Vector3 m_OffLadder;
        private readonly Vector3 m_Finish;
        private readonly bool    m_GoingUp;
        private readonly float   m_EnterSeconds;
        private readonly float   m_ClimbSeconds;
        private readonly float   m_LeaveSeconds;
        private readonly float   m_CycleSeconds;

        private Part  m_Part;
        private float m_Elapsed;
        private float m_Cycle;
        private bool  m_BackedOff;

        /// <param name="start">Where the entity stands as it begins.</param>
        /// <param name="path">The ladder's points and which way it is taken.</param>
        /// <param name="speed">How fast it climbs, in units a second.</param>
        /// <param name="getOnSeconds">How long the get on animation runs.</param>
        /// <param name="climbSeconds">How long one cycle of the climb animation runs.</param>
        /// <param name="getOffSeconds">How long the get off animation runs.</param>
        internal LadderClimb(Vector3 start, LadderClimbArgs path, float speed, float getOnSeconds, float climbSeconds, float getOffSeconds)
        {
            m_Start     = start;
            m_OnLadder  = path.OnLadder;
            m_OffLadder = path.OffLadder;
            m_Finish    = path.Finish;
            m_GoingUp   = path.GoingUp;

            float distance = Vector3.Distance(path.OnLadder, path.OffLadder);

            m_EnterSeconds = start == path.OnLadder ? 0f : path.GoingUp ? getOnSeconds : getOffSeconds;
            m_ClimbSeconds = speed > 0f ? distance / speed : 0f;
            m_LeaveSeconds = path.OffLadder == path.Finish ? 0f : path.GoingUp ? getOffSeconds : getOnSeconds;
            m_CycleSeconds = climbSeconds;

            m_Part      = Part.Entering;
            m_Elapsed   = 0f;
            m_Cycle     = 0f;
            m_BackedOff = false;

            SkipPartsWithNoLength();
        }

        internal readonly bool IsFinished => m_Part == Part.Finished;

        internal readonly Vector3 Position
        {
            get
            {
                switch (m_Part)
                {
                    case Part.Entering:
                        return math.lerp(m_Start, m_OnLadder, m_Elapsed / m_EnterSeconds);

                    case Part.Climbing:
                        return math.lerp(m_OnLadder, m_OffLadder, m_Elapsed / m_ClimbSeconds);

                    case Part.Leaving:
                        return math.lerp(m_OffLadder, m_Finish, m_Elapsed / m_LeaveSeconds);

                    case Part.BackingOff:
                        return math.lerp(m_OnLadder, m_Start, m_Elapsed / m_EnterSeconds);

                    default:
                        return m_BackedOff ? m_Start : m_Finish;
                }
            }
        }

        /// <summary>The animation to show. Which way each runs is the way the entity is going on the ladder.</summary>
        internal readonly LadderPose Pose
        {
            get
            {
                switch (m_Part)
                {
                    case Part.Entering:
                    {
                        float through = m_Elapsed / m_EnterSeconds;

                        return m_GoingUp ? new LadderPose(LadderAnimation.GetOn, through) : new LadderPose(LadderAnimation.GetOff, 1f - through);
                    }

                    case Part.Leaving:
                    {
                        float through = m_Elapsed / m_LeaveSeconds;

                        return m_GoingUp ? new LadderPose(LadderAnimation.GetOff, through) : new LadderPose(LadderAnimation.GetOn, 1f - through);
                    }

                    case Part.BackingOff:
                    {
                        float through = m_Elapsed / m_EnterSeconds;

                        return m_GoingUp ? new LadderPose(LadderAnimation.GetOn, 1f - through) : new LadderPose(LadderAnimation.GetOff, through);
                    }

                    default:
                        return new LadderPose(LadderAnimation.Climb, m_Cycle);
                }
            }
        }

        /// <summary>
        /// Which way along the ladder a push goes, up the screen being up the ladder: 1 on, -1 back and 0 for none.
        /// </summary>
        internal readonly float SteerFrom(float push)
        {
            if (math.abs(push) < STEER_THRESHOLD)
            {
                return 0f;
            }

            return push > 0f == m_GoingUp ? 1f : -1f;
        }

        /// <param name="deltaTime">The frame's time.</param>
        /// <param name="steer">During the climb: 1 goes on, -1 goes back and 0 stays put.</param>
        internal void Tick(float deltaTime, float steer)
        {
            if (m_Part == Part.Climbing)
            {
                Climb(deltaTime, steer);

                return;
            }

            m_Elapsed += deltaTime;

            if (m_Part == Part.Entering && m_Elapsed >= m_EnterSeconds)
            {
                Begin(Part.Climbing);
            }
            else if (m_Part == Part.Leaving && m_Elapsed >= m_LeaveSeconds)
            {
                Begin(Part.Finished);
            }
            else if (m_Part == Part.BackingOff && m_Elapsed >= m_EnterSeconds)
            {
                Begin(Part.Finished);
            }
        }

        private void Climb(float deltaTime, float steer)
        {
            m_Elapsed += deltaTime * steer;

            if (m_CycleSeconds > 0f)
            {
                float direction = m_GoingUp ? steer : -steer;

                m_Cycle = math.frac(m_Cycle + deltaTime * direction / m_CycleSeconds);
            }

            if (m_Elapsed >= m_ClimbSeconds)
            {
                Begin(Part.Leaving);
            }
            else if (m_Elapsed < 0f)
            {
                m_BackedOff = true;

                Begin(Part.BackingOff);
            }
        }

        private void Begin(Part part)
        {
            m_Part    = part;
            m_Elapsed = 0f;

            SkipPartsWithNoLength();
        }

        private void SkipPartsWithNoLength()
        {
            if (m_Part == Part.Entering && m_EnterSeconds <= 0f)
            {
                m_Part = Part.Climbing;
            }

            if (m_Part == Part.Climbing && m_ClimbSeconds <= 0f)
            {
                m_Part = Part.Leaving;
            }

            if (m_Part == Part.Leaving && m_LeaveSeconds <= 0f || m_Part == Part.BackingOff && m_EnterSeconds <= 0f)
            {
                m_Part = Part.Finished;
            }
        }
    }
}
