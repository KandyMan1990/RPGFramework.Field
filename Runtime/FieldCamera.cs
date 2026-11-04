using Unity.Cinemachine;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// One of a field's Cinemachine cameras, named by an id for <c>SET_CAMERA</c>. The field looks through one at a
    /// time, the one with the lowest id as it is entered afresh: the others are switched off, so nothing set on them in
    /// the inspector can take the view.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineCamera))]
    internal sealed class FieldCamera : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("How scripts name this camera. Unique within the field; the lowest is the one a field starts on")]
        private byte m_Id;

        private CinemachineCamera m_Camera;

        internal byte Id => m_Id;

        private void Awake()
        {
            m_Camera = GetComponent<CinemachineCamera>();
        }

        internal void SetLive(bool live)
        {
            m_Camera.enabled = live;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Authoring only. One above the highest id among the other cameras in the field under
        /// <paramref name="fieldRoot" />, so two added one after another never share one.
        /// </summary>
        internal static byte NextId(Transform fieldRoot, FieldCamera except)
        {
            int highest = -1;

            FieldCamera[] others = fieldRoot.GetComponentsInChildren<FieldCamera>(true);

            for (int i = 0; i < others.Length; i++)
            {
                FieldCamera other = others[i];

                if (other != except && other.m_Id > highest)
                {
                    highest = other.m_Id;
                }
            }

            return (byte)(highest + 1);
        }

        internal void SetId(byte id)
        {
            m_Id = id;
        }

        private void Reset()
        {
            m_Id = NextId(transform.root, this);
        }
#endif
    }
}
