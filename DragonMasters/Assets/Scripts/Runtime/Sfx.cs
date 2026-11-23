using System.Collections.Generic;
using UnityEngine;

namespace DragonMasters
{
    /// <summary>Efectos de sonido sintetizados en tiempo de ejecución (no hacen falta archivos de audio).</summary>
    public static class Sfx
    {
        const int Rate = 44100;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static AudioSource[] pool;
        static int next;

        public static void Init(GameObject host)
        {
            if (pool != null) return;
            pool = new AudioSource[8];
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i] = host.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
            }
            var rng = new System.Random(7);
            System.Func<float> Noise = () => (float)(rng.NextDouble() * 2 - 1);

            clips["whoosh"] = Synth(0.35f, (t, d) =>
            {
                float env = Mathf.Sin(Mathf.PI * t / d);
                return Noise() * env * 0.35f * (0.5f + 0.5f * Mathf.Sin(t * 90f));
            }, lowpass: 0.12f);
            clips["hit"] = Synth(0.28f, (t, d) =>
            {
                float f = Mathf.Lerp(140, 45, t / d);
                float env = Mathf.Exp(-t * 18f);
                return (Mathf.Sin(2 * Mathf.PI * f * t) * 0.9f + Noise() * 0.4f * Mathf.Exp(-t * 40f)) * env;
            });
            clips["ding"] = Synth(0.6f, (t, d) =>
                (Mathf.Sin(2 * Mathf.PI * 1320 * t) * 0.5f + Mathf.Sin(2 * Mathf.PI * 1980 * t) * 0.25f) * Mathf.Exp(-t * 6f));
            clips["thud"] = Synth(0.22f, (t, d) => Noise() * Mathf.Exp(-t * 25f) * 0.6f, lowpass: 0.08f);
            clips["coin"] = Synth(0.22f, (t, d) =>
                Mathf.Sign(Mathf.Sin(2 * Mathf.PI * (t < 0.07f ? 988 : 1319) * t)) * 0.18f * Mathf.Exp(-t * 10f));
            clips["click"] = Synth(0.06f, (t, d) => Mathf.Sin(2 * Mathf.PI * 700 * t) * Mathf.Exp(-t * 60f) * 0.6f);
            clips["throw"] = Synth(0.18f, (t, d) =>
                Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(300, 900, t / d) * t) * 0.25f * (1 - t / d));
            clips["win"] = Notes(new[] { 523f, 659f, 784f, 1046f }, 0.13f);
            clips["lose"] = Notes(new[] { 392f, 330f, 262f, 196f }, 0.2f);
            clips["buy"] = Notes(new[] { 784f, 988f, 1175f, 1568f }, 0.07f);
        }

        delegate float Wave(float t, float dur);

        static AudioClip Synth(float dur, Wave wave, float lowpass = 1f)
        {
            int n = Mathf.CeilToInt(dur * Rate);
            var data = new float[n];
            float y = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float x = wave(t, dur);
                y += (x - y) * lowpass;
                data[i] = Mathf.Clamp(y, -1, 1);
            }
            var clip = AudioClip.Create("sfx", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Notes(float[] freqs, float noteLen)
        {
            float dur = noteLen * freqs.Length + 0.3f;
            return Synth(dur, (t, d) =>
            {
                int idx = Mathf.Min((int)(t / noteLen), freqs.Length - 1);
                float lt = t - idx * noteLen;
                float f = freqs[idx];
                return (Mathf.Sin(2 * Mathf.PI * f * t) * 0.35f + Mathf.Sin(4 * Mathf.PI * f * t) * 0.1f) * Mathf.Exp(-lt * 5f);
            });
        }

        public static void Play(string name, float volume = 1f, float pitchJitter = 0.08f)
        {
            if (pool == null || !clips.TryGetValue(name, out var clip)) return;
            var src = pool[next];
            next = (next + 1) % pool.Length;
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.PlayOneShot(clip, volume);
        }
    }
}
