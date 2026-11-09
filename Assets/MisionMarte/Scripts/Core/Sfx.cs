using System.Collections.Generic;
using UnityEngine;

namespace MisionMarte
{
    public enum SfxType { Jump, Collect, Nut, Boost, Hurt, Checkpoint, Success, Blip, Slide, Activate, Error, Hint }

    /// <summary>
    /// Efectos de sonido generados por código (no hace falta importar audios).
    /// Uso: Sfx.Play(SfxType.Jump);
    /// </summary>
    public static class Sfx
    {
        public static float Volume = 0.35f;
        static AudioSource source;
        static Dictionary<SfxType, AudioClip> clips;
        const int Rate = 22050;

        public static void Play(SfxType type)
        {
            if (source == null) Build();
            if (clips.TryGetValue(type, out var c) && c != null) source.PlayOneShot(c, Volume * AccessibilitySettings.SfxEffective);
        }

        static void Build()
        {
            var go = new GameObject("[SFX]");
            Object.DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.ignoreListenerPause = true;
            clips = new Dictionary<SfxType, AudioClip>
            {
                { SfxType.Jump,       Tone("jump", 300, 620, 0.12f, true) },
                { SfxType.Collect,    Arp("collect", new[] { 660f, 880f, 1320f }, 0.06f) },
                { SfxType.Nut,        Tone("nut", 1200, 1500, 0.07f, true) },
                { SfxType.Boost,      Tone("boost", 200, 1200, 0.35f, true) },
                { SfxType.Hurt,       Tone("hurt", 400, 90, 0.3f, true) },
                { SfxType.Checkpoint, Arp("check", new[] { 523f, 659f, 784f, 1046f }, 0.07f) },
                { SfxType.Success,    Arp("success", new[] { 523f, 659f, 784f, 659f, 784f, 1046f }, 0.11f) },
                { SfxType.Blip,       Tone("blip", 900, 900, 0.03f, false) },
                { SfxType.Slide,      Noise("slide", 0.22f) },
                { SfxType.Activate,   Arp("activate", new[] { 300f, 450f, 600f, 900f }, 0.08f) },
                { SfxType.Error,      Tone("error", 260, 110, 0.28f, true) },
                { SfxType.Hint,       Arp("hint", new[] { 700f, 900f, 1100f }, 0.07f) },
            };
        }

        static AudioClip Tone(string name, float f0, float f1, float dur, bool square)
        {
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += f / Rate;
                float s = square ? (Mathf.Repeat(phase, 1f) < 0.5f ? 1f : -1f) * 0.5f : Mathf.Sin(phase * Mathf.PI * 2f);
                data[i] = s * (1f - t) * 0.6f;
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Arp(string name, float[] notes, float noteDur)
        {
            int per = Mathf.CeilToInt(Rate * noteDur);
            var data = new float[per * notes.Length];
            float phase = 0f;
            for (int k = 0; k < notes.Length; k++)
                for (int i = 0; i < per; i++)
                {
                    phase += notes[k] / Rate;
                    float env = 1f - (float)i / per * 0.7f;
                    data[k * per + i] = (Mathf.Repeat(phase, 1f) < 0.5f ? 0.3f : -0.3f) * env;
                }
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Noise(string name, float dur)
        {
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            var r = new System.Random(5);
            float last = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                last = Mathf.Lerp(last, (float)(r.NextDouble() * 2 - 1), 0.25f);
                data[i] = last * (1f - t) * 0.5f;
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
