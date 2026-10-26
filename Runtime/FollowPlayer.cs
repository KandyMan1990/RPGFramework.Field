using Unity.Cinemachine;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// Keeps the Cinemachine camera it is on following whoever is the player: from when the field loads, and onto
    /// another entity when a script makes that one the player.
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

        internal void Follow(Transform player)
        {
            m_Camera.Follow = player;
        }
    }
}
