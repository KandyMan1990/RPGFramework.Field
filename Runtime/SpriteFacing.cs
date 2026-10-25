using RPGFramework.Field.FieldVmArgs;
using RPGFramework.Field.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// Which way a 2D entity faces. A sprite is never rotated — it shows its facing by which frames it draws — so a 2D
    /// driver keeps the facing here, on the screen's plane, and leaves its body's rotation alone. A rotation a script
    /// gives is read as Unity's 2D rotations are: none faces up the screen, and a positive turn is anticlockwise.
    /// </summary>
    internal sealed class SpriteFacing
    {
        private const float MIN_DIRECTION_SQR = 0.0001f;

        private Vector3       m_Facing;
        private RotationState m_Turn;

        internal SpriteFacing()
        {
            m_Facing = Default;
            m_Turn   = default;
        }

        /// <summary>Towards the camera, as a 2D character stands until something turns it.</summary>
        internal static Vector3 Default => Vector3.down;

        internal Vector3 Facing => m_Facing;

        /// <summary>The rotation that faces this way, for a field to keep and give back.</summary>
        internal Quaternion Rotation => RotationFacing(m_Facing);

        internal RotationState Turn => m_Turn;

        /// <summary>Faces along a direction at once; none leaves the facing as it was.</summary>
        internal void Face(Vector3 direction)
        {
            Vector3 flat = new Vector3(direction.x, direction.y, 0f);

            if (flat.sqrMagnitude > MIN_DIRECTION_SQR)
            {
                m_Facing = flat.normalized;
            }
        }

        internal void SetRotation(Quaternion rotation)
        {
            Face(rotation * Vector3.up);
        }

        internal void StartTurn(SetEntityRotationAsyncArgs args)
        {
            float start  = AngleOf(m_Facing);
            float target = Wind(start, AngleOf(args.Rotation * Vector3.up), args.RotationDirection);

            m_Turn = new RotationState
                     {
                         Active        = true,
                         StartAngle    = start,
                         TargetAngle   = target,
                         Elapsed       = 0f,
                         Duration      = args.Duration,
                         Interpolation = args.RotationType,
                         Direction     = args.RotationDirection
                     };
        }

        internal void ResumeTurn(RotationState turn)
        {
            m_Turn = turn;
        }

        internal void Tick(float deltaTime)
        {
            if (!m_Turn.Active)
            {
                return;
            }

            m_Turn.Elapsed += deltaTime;

            float t = math.clamp(m_Turn.Elapsed / m_Turn.Duration, 0f, 1f);
            t = RotationUtility.ApplyInterpolation(t, m_Turn.Interpolation);

            m_Facing = DirectionOf(math.lerp(m_Turn.StartAngle, m_Turn.TargetAngle, t));

            if (m_Turn.Elapsed >= m_Turn.Duration)
            {
                m_Facing      = DirectionOf(m_Turn.TargetAngle);
                m_Turn.Active = false;
            }
        }

        /// <summary>The rotation a script would give to face along a direction.</summary>
        internal static Quaternion RotationFacing(Vector3 direction)
        {
            return Quaternion.Euler(0f, 0f, AngleOf(direction));
        }

        /// <summary>Degrees anticlockwise from up the screen.</summary>
        private static float AngleOf(Vector3 direction)
        {
            return math.degrees(math.atan2(-direction.x, direction.y));
        }

        private static Vector3 DirectionOf(float angle)
        {
            float radians = math.radians(angle);

            return new Vector3(-math.sin(radians), math.cos(radians), 0f);
        }

        // The angle counts anticlockwise, so a clockwise turn is the way it falls.
        private static float Wind(float start, float target, RotationDirection direction)
        {
            float delta = (target - start) % 360f;

            if (delta > 180f)
            {
                delta -= 360f;
            }
            else if (delta < -180f)
            {
                delta += 360f;
            }

            if (direction == RotationDirection.Clockwise && delta > 0f)
            {
                delta -= 360f;
            }
            else if (direction == RotationDirection.CounterClockwise && delta < 0f)
            {
                delta += 360f;
            }

            return start + delta;
        }
    }
}
