using System.Threading.Tasks;
using RPGFramework.Audio;

namespace RPGFramework.Field
{
    /// <summary>A sound a script started.</summary>
    internal interface IFieldSound
    {
        /// <summary>Whether it loops and has not ended, so that it would still be playing after a battle.</summary>
        bool IsLooping { get; }

        /// <summary>How far into the sound it has got, in seconds.</summary>
        float Position { get; }

        void Stop();

        void FadeVolume(float volume, float seconds);

        void FadePan(float pan, float seconds);
    }

    /// <summary>What a script's sounds are played through, as only Audio can make a playing sound.</summary>
    internal interface IFieldSoundPlayer
    {
        Task<IFieldSound> PlayAsync(ulong nameHash, float volume, float pan, float startSeconds);
    }

    /// <summary>Plays a script's sounds on Audio's player.</summary>
    internal sealed class FieldSoundPlayer : IFieldSoundPlayer
    {
        private readonly ISfxPlayer m_SfxPlayer;

        internal FieldSoundPlayer(ISfxPlayer sfxPlayer)
        {
            m_SfxPlayer = sfxPlayer;
        }

        async Task<IFieldSound> IFieldSoundPlayer.PlayAsync(ulong nameHash, float volume, float pan, float startSeconds)
        {
            ISfxReference reference = await m_SfxPlayer.PlayAsync(nameHash, volume, pan, startSeconds, ISfxPlayer.NEXT_IN_ORDER);

            return new FieldSound(m_SfxPlayer, reference);
        }

        private sealed class FieldSound : IFieldSound
        {
            private readonly ISfxPlayer    m_SfxPlayer;
            private readonly ISfxReference m_Reference;

            internal FieldSound(ISfxPlayer sfxPlayer, ISfxReference reference)
            {
                m_SfxPlayer = sfxPlayer;
                m_Reference = reference;
            }

            bool IFieldSound.IsLooping => m_Reference.Loops && !m_Reference.Completion.IsCompleted;

            float IFieldSound.Position => m_Reference.Position;

            void IFieldSound.Stop()
            {
                m_SfxPlayer.Stop(m_Reference);
            }

            void IFieldSound.FadeVolume(float volume, float seconds)
            {
                m_SfxPlayer.SetSoundVolume(m_Reference, volume, seconds);
            }

            void IFieldSound.FadePan(float pan, float seconds)
            {
                m_SfxPlayer.SetSoundPan(m_Reference, pan, seconds);
            }
        }
    }
}
