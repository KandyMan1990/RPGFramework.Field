#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.Field
{
    /// <summary>
    /// World-space debug lines drawn through the field's UI, so they show in the Game view without relying on
    /// the Gizmos toggle. Each line is also sent to <see cref="Debug.DrawLine(Vector3, Vector3, Color)" /> for the
    /// Scene view.
    /// </summary>
    internal sealed class FieldDebugOverlay : VisualElement
    {
        private const float LINE_WIDTH  = 2f;
        private const float ARC_STEP    = 10f;

        private readonly List<(Vector3 From, Vector3 To, Color Colour)> m_Lines = new List<(Vector3, Vector3, Color)>();

        private Camera m_Camera;

        internal FieldDebugOverlay()
        {
            pickingMode    = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left     = 0;
            style.top      = 0;
            style.right    = 0;
            style.bottom   = 0;

            generateVisualContent += Draw;
        }

        internal void Begin(Camera camera)
        {
            m_Camera = camera;
            m_Lines.Clear();
        }

        internal void Line(Vector3 from, Vector3 to, Color colour)
        {
            m_Lines.Add((from, to, colour));
            Debug.DrawLine(from, to, colour);
        }

        internal void Arc(Vector3 centre, Vector3 up, Vector3 startDirection, float angle, float radius, Color colour)
        {
            int     steps    = Mathf.Max(1, Mathf.CeilToInt(angle / ARC_STEP));
            Vector3 previous = centre + startDirection * radius;

            for (int i = 1; i <= steps; i++)
            {
                Vector3 next = centre + Quaternion.AngleAxis(angle * i / steps, up) * startDirection * radius;

                Line(previous, next, colour);

                previous = next;
            }
        }

        internal void End()
        {
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            if (m_Camera == null || panel == null)
            {
                return;
            }

            Painter2D painter = context.painter2D;
            painter.lineWidth = LINE_WIDTH;

            foreach ((Vector3 from, Vector3 to, Color colour) in m_Lines)
            {
                if (m_Camera.WorldToViewportPoint(from).z <= 0f || m_Camera.WorldToViewportPoint(to).z <= 0f)
                {
                    continue;
                }

                painter.strokeColor = colour;
                painter.BeginPath();
                painter.MoveTo(this.WorldToLocal(RuntimePanelUtils.CameraTransformWorldToPanel(panel, from, m_Camera)));
                painter.LineTo(this.WorldToLocal(RuntimePanelUtils.CameraTransformWorldToPanel(panel, to,   m_Camera)));
                painter.Stroke();
            }
        }
    }
}
#endif
