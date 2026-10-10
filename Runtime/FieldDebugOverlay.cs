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
        private const float LINE_WIDTH = 2f;
        private const float ARC_STEP   = 10f;

        private readonly List<(Vector3 From, Vector3 To, Color Colour)> m_Lines;

        private Camera m_Camera;

        internal FieldDebugOverlay()
        {
            m_Lines = new List<(Vector3, Vector3, Color)>();

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

        /// <summary>
        /// The edges of a box, placed by <paramref name="localToWorld" /> — a collider's object's, so it turns and scales
        /// with it, or the identity for a box already in world space.
        /// </summary>
        internal void Box(Matrix4x4 localToWorld, Vector3 centre, Vector3 size, Color colour)
        {
            Vector3   half    = size / 2f;
            Vector3[] corners = new Vector3[8];

            for (int i = 0; i < 8; i++)
            {
                Vector3 sign = new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f);

                corners[i] = localToWorld.MultiplyPoint3x4(centre + Vector3.Scale(half, sign));
            }

            // Each corner joins the three that differ from it by one axis.
            for (int i = 0; i < 8; i++)
            {
                for (int axis = 1; axis < 8; axis <<= 1)
                {
                    if ((i & axis) == 0)
                    {
                        Line(corners[i], corners[i | axis], colour);
                    }
                }
            }
        }

        /// <summary>
        /// The edges of a 2D box collider, in the plane its object lies in.
        /// </summary>
        internal void Rectangle(Transform transform, Vector2 offset, Vector2 size, Color colour)
        {
            Vector2 half = size / 2f;

            Vector3 a = transform.TransformPoint(offset + new Vector2(-half.x, -half.y));
            Vector3 b = transform.TransformPoint(offset + new Vector2(half.x,  -half.y));
            Vector3 c = transform.TransformPoint(offset + new Vector2(half.x,  half.y));
            Vector3 d = transform.TransformPoint(offset + new Vector2(-half.x, half.y));

            Line(a, b, colour);
            Line(b, c, colour);
            Line(c, d, colour);
            Line(d, a, colour);
        }

        /// <summary>
        /// A world-aligned box around any other collider's bounds, which is looser than its shape but always drawable.
        /// </summary>
        internal void Bounds(Bounds bounds, Color colour)
        {
            Box(Matrix4x4.identity, bounds.center, bounds.size, colour);
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

            for (int i = 0; i < m_Lines.Count; i++)
            {
                (Vector3 from, Vector3 to, Color colour) = m_Lines[i];

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