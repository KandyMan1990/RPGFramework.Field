using System.Threading.Tasks;
using RPGFramework.Audio;
using RPGFramework.Core;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Core.Transitions;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.Field
{
    /// <summary>Where a script has left the field's screen, as the save keeps it.</summary>
    internal enum ScreenFade : byte
    {
        Clear,
        FadedOut,
        ColourSubtracted,
        ColourAdded
    }

    /// <summary>
    /// The field's own screen, beneath its message windows: a script's fades, transitions played into a layer at the
    /// back of the field's UI, and its colour fades, a colour taken from the screen or added to it. The two are one
    /// state, so either replaces the other, and where a change will settle is saved as it starts, so a field left faded
    /// or tinted is still so after a battle or a load. The layer stays through a field jump, the module's scene staying
    /// loaded. On unscaled time, as transitions are.
    /// </summary>
    internal sealed class FieldScreen
    {
        private readonly IMemoryService  m_MemoryService;
        private readonly Shader          m_ScreenColourShader;
        private readonly VisualElement   m_Host;
        private readonly TransitionLayer m_Layer;
        private readonly ISfxPlayer      m_SfxPlayer;
        private readonly ushort          m_FadeAddress;
        private readonly ushort          m_TransitionAddress;
        private readonly ushort          m_ColourAddress;

        private ScreenColour m_Colour;
        private Color        m_Shown;
        private bool         m_Adds;
        private Task         m_Fade;
        private Task         m_ColourFade;
        private int          m_ColourRun;

        internal FieldScreen(ITransitionConfig config, ITransitionDatabase database, ISfxPlayer sfxPlayer, IMemoryService memoryService, IVariableMap variableMap)
        {
            m_MemoryService      = memoryService;
            m_ScreenColourShader = config.ScreenColourShader;
            m_Host               = new VisualElement { name = "ScreenFade" };
            m_Layer              = new TransitionLayer(m_Host, config, database);
            m_SfxPlayer          = sfxPlayer;
            m_FadeAddress        = AddressOf(variableMap, FieldVariables.SCREEN_FADE);
            m_TransitionAddress  = AddressOf(variableMap, FieldVariables.SCREEN_FADE_TRANSITION);
            m_ColourAddress      = AddressOf(variableMap, FieldVariables.SCREEN_COLOUR);
            m_Colour             = null;
            m_Shown              = Color.clear;
            m_Adds               = false;
            m_Fade               = Task.CompletedTask;
            m_ColourFade         = Task.CompletedTask;
            m_ColourRun          = 0;

            m_Host.pickingMode    = PickingMode.Ignore;
            m_Host.style.position = Position.Absolute;
            m_Host.style.left     = 0f;
            m_Host.style.top      = 0f;
            m_Host.style.right    = 0f;
            m_Host.style.bottom   = 0f;
        }

        internal bool IsFading => !m_Fade.IsCompleted;

        internal bool IsColourFading => !m_ColourFade.IsCompleted;

        /// <summary>Puts the layer at the back of the field's UI, showing where the save says the screen was left.</summary>
        internal void Enter(VisualElement root)
        {
            root.Insert(0, m_Host);

            ScreenFade fade = (ScreenFade)m_MemoryService.ReadByte(MemoryBank.Persistent, m_FadeAddress);

            switch (fade)
            {
                case ScreenFade.FadedOut:
                    m_Layer.Cut(m_MemoryService.ReadUlong(MemoryBank.Persistent, m_TransitionAddress), true);
                    break;

                case ScreenFade.ColourSubtracted:
                case ScreenFade.ColourAdded:
                    ShowColour(Unpack(m_MemoryService.ReadInt(MemoryBank.Persistent, m_ColourAddress)), fade == ScreenFade.ColourAdded);
                    break;
            }
        }

        /// <summary>Stops drawing as the field module exits, leaving what the save holds.</summary>
        internal void Exit()
        {
            m_Layer.Cut(0, false);
            RemoveColour();

            m_Host.RemoveFromHierarchy();
        }

        /// <summary>
        /// Fades the screen out, covering it, or back in, through <paramref name="transition" />, from wherever it is,
        /// or <paramref name="atOnce" />, ending any colour fade.
        /// </summary>
        internal void Fade(bool fadeOut, ulong transition, bool atOnce)
        {
            RemoveColour();
            Save(fadeOut ? ScreenFade.FadedOut : ScreenFade.Clear, transition, 0);

            if (atOnce)
            {
                m_Layer.Cut(transition, fadeOut);
                m_Fade = Task.CompletedTask;

                return;
            }

            TransitionAsset played = fadeOut ? m_Layer.FindCover(transition) : m_Layer.FindReveal(transition);

            if (played.Sound != 0)
            {
                m_SfxPlayer.PlayAsync(played.Sound).FireAndForget();
            }

            m_Fade = fadeOut ? m_Layer.CoverAsync(transition) : m_Layer.RevealAsync(transition);
            m_Fade.FireAndForget();
        }

        /// <summary>
        /// Takes <paramref name="colour" /> from each channel of the screen, or adds it, reaching it over
        /// <paramref name="seconds" /> from the colour shown — black after a fade out, when it is taken — or at once
        /// for 0, ending any fade. A colour of black leaves the screen clear.
        /// </summary>
        internal void FadeColour(Color colour, bool add, float seconds)
        {
            Color from = m_Colour != null ? m_Shown : add ? Color.black : Color.white * m_Layer.Covered;

            m_Layer.Cut(0, false);
            m_Fade = Task.CompletedTask;

            bool clear = colour.r <= 0f && colour.g <= 0f && colour.b <= 0f;

            Save(clear ? ScreenFade.Clear : add ? ScreenFade.ColourAdded : ScreenFade.ColourSubtracted, 0, Pack(colour));

            ShowColour(from, add);

            m_ColourFade = FadeColourAsync(++m_ColourRun, from, colour, seconds, clear);
            m_ColourFade.FireAndForget();
        }

        private async Task FadeColourAsync(int run, Color from, Color to, float seconds, bool clear)
        {
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed = math.min(elapsed + Time.unscaledDeltaTime, seconds);

                SetColour(Color.Lerp(from, to, elapsed / seconds));

                await Awaitable.NextFrameAsync();

                if (run != m_ColourRun)
                {
                    return;
                }
            }

            SetColour(to);

            if (clear)
            {
                RemoveColour();
            }
        }

        private void ShowColour(Color colour, bool add)
        {
            m_Colour ??= new ScreenColour(m_Host, m_ScreenColourShader);
            m_Adds = add;

            SetColour(colour);
        }

        private void SetColour(Color colour)
        {
            m_Shown = colour;

            if (m_Adds)
            {
                m_Colour.Add(colour);
            }
            else
            {
                m_Colour.Subtract(colour);
            }
        }

        private void RemoveColour()
        {
            m_ColourRun++;
            m_ColourFade = Task.CompletedTask;

            m_Colour?.Remove();

            m_Colour = null;
            m_Shown  = Color.clear;
        }

        private void Save(ScreenFade fade, ulong transition, int colour)
        {
            m_MemoryService.WriteByte(MemoryBank.Persistent, m_FadeAddress, (byte)fade);
            m_MemoryService.WriteUlong(MemoryBank.Persistent, m_TransitionAddress, transition);
            m_MemoryService.WriteInt(MemoryBank.Persistent, m_ColourAddress, colour);
        }

        private static int Pack(Color colour)
        {
            Color32 bytes  = colour;
            int     packed = (bytes.r << 16) | (bytes.g << 8) | bytes.b;

            return packed;
        }

        private static Color Unpack(int packed)
        {
            Color colour = new Color32((byte)(packed >> 16), (byte)(packed >> 8), (byte)packed, 255);

            return colour;
        }

        private static ushort AddressOf(IVariableMap variableMap, string name)
        {
            variableMap.TryGetVariable(name, out VariableDefinition variable);

            ushort address = (ushort)variable.Offset;

            return address;
        }
    }
}