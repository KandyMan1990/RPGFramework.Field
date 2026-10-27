using System.Collections.Generic;
using RPGFramework.Audio.Music;
using RPGFramework.Audio.Sfx;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// The music, sounds and stem states the game's providers list: what a script's audio names can resolve to. The
    /// providers' lists are what is authored, whether the game builds its audio in or loads it from bundles.
    /// </summary>
    internal sealed class AudioNames
    {
        private readonly List<string>                     m_Music;
        private readonly List<string>                     m_Sounds;
        private readonly Dictionary<string, List<string>> m_StatesByTrack;
        private readonly List<string>                     m_AllStates;

        internal AudioNames(List<string> music, List<string> sounds, Dictionary<string, List<string>> statesByTrack)
        {
            m_Music         = music;
            m_Sounds        = sounds;
            m_StatesByTrack = statesByTrack;
            m_AllStates     = new List<string>();

            for (int i = 0; i < music.Count; i++)
            {
                if (!statesByTrack.TryGetValue(music[i], out List<string> states))
                {
                    continue;
                }

                for (int j = 0; j < states.Count; j++)
                {
                    if (!m_AllStates.Contains(states[j]))
                    {
                        m_AllStates.Add(states[j]);
                    }
                }
            }
        }

        internal List<string> Music  => m_Music;
        internal List<string> Sounds => m_Sounds;

        /// <summary>The track's stem states, or every track's when no provider lists the track.</summary>
        internal List<string> StatesOf(string track)
        {
            return track != null && m_StatesByTrack.TryGetValue(track, out List<string> states) ? states : m_AllStates;
        }

        internal static AudioNames FromProject()
        {
            List<string>                     music         = new List<string>();
            List<string>                     sounds        = new List<string>();
            Dictionary<string, List<string>> statesByTrack = new Dictionary<string, List<string>>();

            MusicAssetProvider[] musicProviders = LoadAll<MusicAssetProvider>();

            for (int i = 0; i < musicProviders.Length; i++)
            {
                MusicAssetProvider provider = musicProviders[i];

                if (provider == null)
                {
                    continue;
                }

                foreach (string track in provider.AssetNames)
                {
                    if (statesByTrack.ContainsKey(track))
                    {
                        continue;
                    }

                    music.Add(track);
                    statesByTrack.Add(track, new List<string>(provider.StemStateNamesOf(track)));
                }
            }

            SfxAssetProvider[] sfxProviders = LoadAll<SfxAssetProvider>();

            for (int i = 0; i < sfxProviders.Length; i++)
            {
                SfxAssetProvider provider = sfxProviders[i];

                if (provider == null)
                {
                    continue;
                }

                foreach (string sound in provider.AssetNames)
                {
                    if (!sounds.Contains(sound))
                    {
                        sounds.Add(sound);
                    }
                }
            }

            return new AudioNames(music, sounds, statesByTrack);
        }

        private static T[] LoadAll<T>() where T : ScriptableObject
        {
            string[] guids  = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            T[]      assets = new T[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                assets[i] = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            return assets;
        }
    }
}
