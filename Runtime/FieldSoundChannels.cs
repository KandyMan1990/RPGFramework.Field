using System.Threading.Tasks;
using RPGFramework.Core;

namespace RPGFramework.Field
{
    /// <summary>A looping sound a battle stopped, to be started again where it had got to as the field returns.</summary>
    internal readonly struct KeptSound
    {
        internal readonly byte  Channel;
        internal readonly ulong NameHash;
        internal readonly float Volume;
        internal readonly float Pan;
        internal readonly float Position;

        internal KeptSound(byte channel, ulong nameHash, float volume, float pan, float position)
        {
            Channel  = channel;
            NameHash = nameHash;
            Volume   = volume;
            Pan      = pan;
            Position = position;
        }
    }

    /// <summary>
    /// The sounds scripts can reach again, each kept under a channel the script picked; channel 0 keeps nothing. A
    /// channel is only that key. A sound takes a few frames to load, so what a script asks of one still loading is done
    /// the moment it has, in the order it was asked.
    /// </summary>
    internal sealed class FieldSoundChannels
    {
        private const byte NO_CHANNEL = 0;

        private struct Channel
        {
            internal Task<IFieldSound> Sound;
            internal ulong             NameHash;
            internal float             Volume;
            internal float             Pan;
        }

        private readonly IFieldSoundPlayer m_Player;
        private readonly Channel[]         m_Channels;

        internal FieldSoundChannels(IFieldSoundPlayer player)
        {
            m_Player   = player;
            m_Channels = new Channel[ArgumentTypes.SOUND_CHANNEL_COUNT];
        }

        /// <summary>Plays a sound, in place of whatever the channel holds.</summary>
        internal void Play(ulong nameHash, float volume, float pan, byte channel)
        {
            Start(nameHash, volume, pan, channel, 0f);
        }

        internal void Stop(byte channel)
        {
            Task<IFieldSound> sound = m_Channels[channel].Sound;

            if (sound == null)
            {
                return;
            }

            m_Channels[channel] = default;

            StopOnceLoadedAsync(sound).FireAndForget();
        }

        internal void StopAll()
        {
            for (int i = 0; i < m_Channels.Length; i++)
            {
                Stop((byte)i);
            }
        }

        internal void FadeVolume(byte channel, float volume, float seconds)
        {
            Task<IFieldSound> sound = m_Channels[channel].Sound;

            if (sound == null)
            {
                return;
            }

            m_Channels[channel].Volume = volume;

            FadeVolumeOnceLoadedAsync(sound, volume, seconds).FireAndForget();
        }

        internal void FadePan(byte channel, float pan, float seconds)
        {
            Task<IFieldSound> sound = m_Channels[channel].Sound;

            if (sound == null)
            {
                return;
            }

            m_Channels[channel].Pan = pan;

            FadePanOnceLoadedAsync(sound, pan, seconds).FireAndForget();
        }

        /// <summary>
        /// Every channel's looping sound, at the volume and pan a fade under way is going to. A return runs no init
        /// script to start them again.
        /// </summary>
        internal KeptSound[] KeepLooping()
        {
            int count = 0;

            for (int i = 0; i < m_Channels.Length; i++)
            {
                if (IsLooping(i))
                {
                    count++;
                }
            }

            KeptSound[] kept = new KeptSound[count];

            int next = 0;

            for (int i = 0; i < m_Channels.Length; i++)
            {
                if (IsLooping(i))
                {
                    Channel channel = m_Channels[i];

                    kept[next++] = new KeptSound((byte)i, channel.NameHash, channel.Volume, channel.Pan, channel.Sound.Result.Position);
                }
            }

            return kept;
        }

        /// <summary>Starts sounds a battle stopped, each from where it had got to.</summary>
        internal void StartAgain(KeptSound[] sounds)
        {
            for (int i = 0; i < sounds.Length; i++)
            {
                KeptSound sound = sounds[i];

                Start(sound.NameHash, sound.Volume, sound.Pan, sound.Channel, sound.Position);
            }
        }

        private void Start(ulong nameHash, float volume, float pan, byte channel, float startSeconds)
        {
            Task<IFieldSound> sound = m_Player.PlayAsync(nameHash, volume, pan, startSeconds);

            sound.FireAndForget();

            if (channel == NO_CHANNEL)
            {
                return;
            }

            Stop(channel);

            m_Channels[channel].Sound    = sound;
            m_Channels[channel].NameHash = nameHash;
            m_Channels[channel].Volume   = volume;
            m_Channels[channel].Pan      = pan;
        }

        private bool IsLooping(int channel)
        {
            Task<IFieldSound> sound = m_Channels[channel].Sound;

            return sound != null && sound.IsCompletedSuccessfully && sound.Result.IsLooping;
        }

        private static async Task StopOnceLoadedAsync(Task<IFieldSound> sound)
        {
            IFieldSound loaded = await sound;

            loaded.Stop();
        }

        private static async Task FadeVolumeOnceLoadedAsync(Task<IFieldSound> sound, float volume, float seconds)
        {
            IFieldSound loaded = await sound;

            loaded.FadeVolume(volume, seconds);
        }

        private static async Task FadePanOnceLoadedAsync(Task<IFieldSound> sound, float pan, float seconds)
        {
            IFieldSound loaded = await sound;

            loaded.FadePan(pan, seconds);
        }
    }
}
