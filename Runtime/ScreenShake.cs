using RPGFramework.Field.FieldVmArgs;
using RPGFramework.Field.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// The whole picture shaking until a script stops it: one axis across and one down, each swinging to the far side of
    /// where it was by a random part of its amplitude. Amplitudes are on a screen 1920 wide.
    /// </summary>
    internal struct ScreenShake
    {
        internal const float SCREEN_WIDTH = 1920f;

        private ShakeAxis m_Across;
        private ShakeAxis m_Down;

        internal Vector2 Offset => new Vector2(m_Across.Offset, m_Down.Offset);

        /// <summary>Whether it has stopped and come back to rest, or never started.</summary>
        internal bool IsStill => m_Across.IsStill && m_Down.IsStill;

        /// <summary>Starts it, or changes one under way from its next swing.</summary>
        internal void Start(float amplitudeX, float swingSecondsX, float amplitudeY, float swingSecondsY)
        {
            m_Across.Start(amplitudeX, swingSecondsX);
            m_Down.Start(amplitudeY, swingSecondsY);
        }

        /// <summary>Finishes the swing under way and comes back to rest over one more.</summary>
        internal void Stop()
        {
            m_Across.Stop();
            m_Down.Stop();
        }

        /// <param name="deltaTime">Seconds since the last tick.</param>
        /// <param name="rollX">A random number from 0 to 1, how much of its amplitude a new swing across takes.</param>
        /// <param name="rollY">A random number from 0 to 1, how much of its amplitude a new swing down takes.</param>
        internal void Tick(float deltaTime, float rollX, float rollY)
        {
            m_Across.Tick(deltaTime, rollX);
            m_Down.Tick(deltaTime, rollY);
        }

        /// <summary>A projection moving the picture by an offset on the screen, perspective or orthographic alike.</summary>
        internal static Matrix4x4 Shifted(Matrix4x4 projection, Vector2 offset, float aspect)
        {
            float halfWidth  = SCREEN_WIDTH * 0.5f;
            float halfHeight = halfWidth / aspect;

            return Matrix4x4.Translate(new Vector3(offset.x / halfWidth, offset.y / halfHeight, 0f)) * projection;
        }

        private struct ShakeAxis
        {
            private float m_Amplitude;
            private float m_SwingSeconds;
            private float m_From;
            private float m_To;
            private float m_Elapsed;
            private bool  m_Shaking;
            private bool  m_Swinging;

            internal float Offset { get; private set; }

            internal bool IsStill => !m_Shaking && !m_Swinging && m_Elapsed >= m_SwingSeconds;

            internal void Start(float amplitude, float swingSeconds)
            {
                m_Amplitude    = amplitude;
                m_SwingSeconds = swingSeconds;
                m_Shaking      = true;
            }

            internal void Stop()
            {
                m_Shaking = false;
            }

            internal void Tick(float deltaTime, float roll)
            {
                m_Elapsed += deltaTime;

                if (m_Shaking)
                {
                    if (!m_Swinging)
                    {
                        Swing(Offset, roll * m_Amplitude);

                        m_Swinging = true;
                    }
                    else if (m_Elapsed >= m_SwingSeconds)
                    {
                        Swing(m_To, (m_To < 0f ? 1f : -1f) * roll * m_Amplitude);
                    }
                }
                else if (m_Swinging && m_Elapsed >= m_SwingSeconds)
                {
                    Swing(m_To, 0f);

                    m_Swinging = false;
                }

                Offset = m_Elapsed >= m_SwingSeconds ? m_To : math.lerp(m_From, m_To, EasingCurve.Apply(m_Elapsed / m_SwingSeconds, Easing.Smooth));
            }

            private void Swing(float from, float to)
            {
                m_From    = from;
                m_To      = to;
                m_Elapsed = 0f;
            }
        }
    }
}
