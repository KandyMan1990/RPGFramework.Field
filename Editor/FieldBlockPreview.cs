using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// Tries a block from the block editor with no play mode: a shake moves the Scene view by the game's own shake until
    /// it is tried again, and a vibration runs the pad for its time.
    /// </summary>
    internal sealed class FieldBlockPreview
    {
        private ScreenShake      m_Shake;
        private FieldScriptBlock m_ShakingBlock;
        private Vector3          m_Moved;
        private float            m_VibrationLeft;
        private double           m_LastTime;
        private bool             m_Running;

        internal FieldBlockPreview()
        {
            m_Shake         = default;
            m_ShakingBlock  = null;
            m_Moved         = Vector3.zero;
            m_VibrationLeft = 0f;
            m_LastTime      = 0d;
            m_Running       = false;
        }

        internal static bool CanTry(FieldScriptBlock block)
        {
            return block.IsRecognised && (block.OpCode.OpCode == FieldScriptOpCode.ShakeScreen || block.OpCode.OpCode == FieldScriptOpCode.SetVibration);
        }

        internal static string Description(FieldScriptBlock block)
        {
            return block.OpCode.OpCode == FieldScriptOpCode.ShakeScreen ? "Try this shake on the Scene view. It runs until you press this again" : "Try this vibration on a connected gamepad";
        }

        internal void Try(FieldScriptBlock block)
        {
            float[] values = new float[block.Arguments.Count];

            for (int i = 0; i < values.Length; i++)
            {
                if (!float.TryParse(block.Arguments[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
                {
                    Debug.LogWarning($"{block.OpCode.ScriptName} can only be tried with numbers, and [{block.Arguments[i]}] is not one");

                    return;
                }
            }

            if (block.OpCode.OpCode == FieldScriptOpCode.ShakeScreen)
            {
                TryShake(block, values);
            }
            else
            {
                TryVibration(values);
            }
        }

        internal void Stop()
        {
            if (!m_Running)
            {
                return;
            }

            m_Shake         = default;
            m_ShakingBlock  = null;
            m_VibrationLeft = 0f;
            m_Running       = false;

            MoveView(Vector2.zero);

            InputSystem.ResetHaptics();

            EditorApplication.update                  -= Tick;
            AssemblyReloadEvents.beforeAssemblyReload -= Stop;
            EditorApplication.playModeStateChanged    -= OnPlayModeChanged;
        }

        private void TryShake(FieldScriptBlock block, float[] values)
        {
            if (m_ShakingBlock == block)
            {
                m_Shake.Stop();
                m_ShakingBlock = null;

                return;
            }

            m_Shake.Start(values[0], values[1], values[2], values[3]);
            m_ShakingBlock = block;

            Begin();
        }

        private void TryVibration(float[] values)
        {
            if (Gamepad.current == null)
            {
                Debug.LogWarning("There is no gamepad connected to try this on");

                return;
            }

            Gamepad.current.SetMotorSpeeds(values[0], values[1]);

            m_VibrationLeft = values[2];

            Begin();
        }

        private void Begin()
        {
            if (m_Running)
            {
                return;
            }

            m_Running  = true;
            m_LastTime = EditorApplication.timeSinceStartup;

            EditorApplication.update                  += Tick;
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            EditorApplication.playModeStateChanged    += OnPlayModeChanged;
        }

        private void Tick()
        {
            double now   = EditorApplication.timeSinceStartup;
            float  delta = (float)(now - m_LastTime);

            m_LastTime = now;

            if (!m_Shake.IsStill)
            {
                m_Shake.Tick(delta, Random.value, Random.value);

                MoveView(m_Shake.Offset);
            }

            if (m_VibrationLeft > 0f)
            {
                m_VibrationLeft -= delta;

                if (m_VibrationLeft <= 0f)
                {
                    InputSystem.ResetHaptics();
                }
            }

            if (m_Shake.IsStill && m_VibrationLeft <= 0f)
            {
                Stop();
            }
        }

        // The pivot is moved by what it has not been moved already, so looking around while it shakes is kept.
        private void MoveView(Vector2 offset)
        {
            SceneView view = SceneView.lastActiveSceneView;

            if (view == null)
            {
                return;
            }

            Camera  camera      = view.camera;
            float   halfHeight  = camera.orthographic ? camera.orthographicSize : Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * view.cameraDistance;
            float   worldByUnit = halfHeight * 2f * camera.aspect / ScreenShake.SCREEN_WIDTH;
            Vector3 moved       = (camera.transform.right * offset.x + camera.transform.up * offset.y) * worldByUnit;

            view.pivot += moved - m_Moved;
            m_Moved    =  moved;

            view.Repaint();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            Stop();
        }
    }
}
