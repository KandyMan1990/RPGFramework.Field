using Unity.Cinemachine;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// Keeps the Cinemachine camera it is on following the field's camera focus: whoever is the player, unless a
    /// script has scrolled the view to a point or onto another entity.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineCamera))]
    internal sealed class FollowPlayer : MonoBehaviour
    {
        private CinemachineCamera m_Camera;

        private void Awake()
        {
            m_Camera = GetComponent<CinemachineCamera>();
        }

        internal void Follow(Transform focus)
        {
            m_Camera.Follow = focus;
        }
    }
}
