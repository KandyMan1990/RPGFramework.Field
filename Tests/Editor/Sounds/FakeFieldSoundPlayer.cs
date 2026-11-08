using System.Collections.Generic;
using System.Threading.Tasks;

namespace RPGFramework.Field.Tests.Sounds
{
    /// <summary>A sound that records what is asked of it.</summary>
    internal sealed class FakeFieldSound : IFieldSound
    {
        internal FakeFieldSound(ulong nameHash, float volume, float pan, float startSeconds)
        {
            NameHash     = nameHash;
            Volume       = volume;
            Pan          = pan;
            StartSeconds = startSeconds;
        }

        internal ulong NameHash      { get; }
        internal float Volume        { get; private set; }
        internal float Pan           { get; private set; }
        internal float StartSeconds  { get; }
        internal float FadeSeconds   { get; private set; }
        internal int   StopCount     { get; private set; }
        internal bool  Loops         { get; set; }
        internal float PositionNow   { get; set; }

        bool IFieldSound.IsLooping => Loops && StopCount == 0;

        float IFieldSound.Position => PositionNow;

        void IFieldSound.Stop()
        {
            StopCount++;
        }

        void IFieldSound.FadeVolume(float volume, float seconds)
        {
            Volume      = volume;
            FadeSeconds = seconds;
        }

        void IFieldSound.FadePan(float pan, float seconds)
        {
            Pan         = pan;
            FadeSeconds = seconds;
        }
    }

    /// <summary>Hands back a sound for each play, at once or, while <see cref="Loading" />, when a test finishes them.</summary>
    internal sealed class FakeFieldSoundPlayer : IFieldSoundPlayer
    {
        private readonly List<TaskCompletionSource<IFieldSound>> m_Loads;

        internal FakeFieldSoundPlayer()
        {
            Played  = new List<FakeFieldSound>();
            m_Loads = new List<TaskCompletionSource<IFieldSound>>();
        }

        internal List<FakeFieldSound> Played  { get; }
        internal bool                 Loading { get; set; }

        Task<IFieldSound> IFieldSoundPlayer.PlayAsync(ulong nameHash, float volume, float pan, float startSeconds)
        {
            FakeFieldSound sound = new FakeFieldSound(nameHash, volume, pan, startSeconds);

            Played.Add(sound);

            if (!Loading)
            {
                return Task.FromResult<IFieldSound>(sound);
            }

            TaskCompletionSource<IFieldSound> load = new TaskCompletionSource<IFieldSound>();

            m_Loads.Add(load);

            return load.Task;
        }

        internal void FinishLoading()
        {
            int played = Played.Count - m_Loads.Count;

            for (int i = 0; i < m_Loads.Count; i++)
            {
                m_Loads[i].SetResult(Played[played + i]);
            }

            m_Loads.Clear();
        }
    }
}
