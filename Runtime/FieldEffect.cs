using UnityEngine;
using UnityEngine.VFX;

namespace RPGFramework.Field
{
    /// <summary>
    /// Something an entity has that a script switches on and off: a torch, steam, sparks. It sits under the entity's
    /// body and moves with it. What it shows lives on this GameObject or under it — Particle Systems, VFX Graphs, lights
    /// — and it starts the way the prefab has it. Switched off, its particles stop being made and those already out run
    /// their course, then all of it goes off.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FieldEffect : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("How scripts name this effect. Unique within the field")]
        private byte m_Id;

        private ParticleSystem[] m_ParticleSystems;
        private VisualEffect[]   m_VisualEffects;
        private bool             m_Finishing;

        internal byte Id => m_Id;

        internal void Bind()
        {
            m_ParticleSystems = GetComponentsInChildren<ParticleSystem>(true);
            m_VisualEffects   = GetComponentsInChildren<VisualEffect>(true);
        }

        internal void SwitchOn()
        {
            bool wasOn = gameObject.activeSelf;

            m_Finishing = false;

            gameObject.SetActive(true);

            for (int i = 0; i < m_ParticleSystems.Length; i++)
            {
                m_ParticleSystems[i].Play(false);
            }

            // One just switched on has already been told to play, by its initial event.
            for (int i = 0; wasOn && i < m_VisualEffects.Length; i++)
            {
                m_VisualEffects[i].Play();
            }
        }

        internal void SwitchOff()
        {
            for (int i = 0; i < m_ParticleSystems.Length; i++)
            {
                m_ParticleSystems[i].Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }

            for (int i = 0; i < m_VisualEffects.Length; i++)
            {
                m_VisualEffects[i].Stop();
            }

            m_Finishing = true;

            Tick();
        }

        /// <summary>Off with nothing left to finish, for an effect put back as a field is returned to.</summary>
        internal void SwitchOffAtOnce()
        {
            m_Finishing = false;

            gameObject.SetActive(false);
        }

        internal void Tick()
        {
            if (m_Finishing && !HasParticlesOut())
            {
                SwitchOffAtOnce();
            }
        }

        private bool HasParticlesOut()
        {
            for (int i = 0; i < m_ParticleSystems.Length; i++)
            {
                if (m_ParticleSystems[i].IsAlive(false))
                {
                    return true;
                }
            }

            for (int i = 0; i < m_VisualEffects.Length; i++)
            {
                if (m_VisualEffects[i].HasAnySystemAwake())
                {
                    return true;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Authoring only. One above the highest id among the other effects in the field under
        /// <paramref name="fieldRoot" />, so two added one after another never share one.
        /// </summary>
        internal static byte NextId(Transform fieldRoot, FieldEffect except)
        {
            int highest = -1;

            FieldEffect[] others = fieldRoot.GetComponentsInChildren<FieldEffect>(true);

            for (int i = 0; i < others.Length; i++)
            {
                if (others[i] != except && others[i].m_Id > highest)
                {
                    highest = others[i].m_Id;
                }
            }

            return (byte)(highest + 1);
        }

        internal void SetId(byte id)
        {
            m_Id = id;
        }
#endif
    }
}
