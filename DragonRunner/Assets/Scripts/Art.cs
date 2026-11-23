using System.Collections.Generic;
using UnityEngine;

namespace DragonRunner
{
    /// <summary>Carga los sprites recortados de Resources/Sprites y crea sonidos sencillos por código.</summary>
    public static class Art
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static Sprite white;

        /// <summary>Sprite con el pivote abajo en el centro (los pies apoyan en el suelo).</summary>
        public static Sprite Get(string path)
        {
            if (cache.TryGetValue(path, out var s)) return s;
            var tex = Resources.Load<Texture2D>("Sprites/" + path);
            if (tex == null)
            {
                Debug.LogWarning("DragonRunner: no encuentro el sprite Resources/Sprites/" + path);
                return White;
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), tex.height, 0, SpriteMeshType.FullRect);
            s.name = path;
            cache[path] = s;
            return s;
        }

        /// <summary>Sprite blanco de 1×1 unidades, para pintar el suelo y los fundidos.</summary>
        public static Sprite White
        {
            get
            {
                if (white != null) return white;
                var tex = new Texture2D(4, 4);
                var px = new Color[16];
                for (int i = 0; i < px.Length; i++) px[i] = Color.white;
                tex.SetPixels(px);
                tex.Apply();
                white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
                return white;
            }
        }

        /// <summary>Crea un SpriteRenderer con la altura indicada en unidades del mundo.</summary>
        public static SpriteRenderer Make(string name, Sprite sprite, float height, Transform parent = null, int order = 0)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            float h = sprite.bounds.size.y;
            go.transform.localScale = Vector3.one * (h > 0 ? height / h : 1f);
            return sr;
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        // ---------- Sonidos generados (no hay audio en Brand 01) ----------

        const int Rate = 44100;

        public static AudioClip Tone(string name, float seconds, float f0, float f1, float volume, bool square = false, bool noise = false)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            float phase = 0f;
            var rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += f / Rate;
                float v = noise ? (float)(rng.NextDouble() * 2 - 1)
                        : square ? (Mathf.Repeat(phase, 1f) < 0.5f ? 1f : -1f)
                        : Mathf.Sin(phase * Mathf.PI * 2f);
                float env = Mathf.Min(1f, t * 40f) * (1f - t) * (1f - t);
                data[i] = v * env * volume;
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip Arpeggio(string name, float[] notes, float noteLen, float volume)
        {
            int per = Mathf.CeilToInt(noteLen * Rate);
            var data = new float[per * notes.Length];
            for (int k = 0; k < notes.Length; k++)
                for (int i = 0; i < per; i++)
                {
                    float t = (float)i / per;
                    float env = Mathf.Min(1f, t * 30f) * (1f - t);
                    data[k * per + i] = Mathf.Sin(2f * Mathf.PI * notes[k] * i / Rate) * env * volume;
                }
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
