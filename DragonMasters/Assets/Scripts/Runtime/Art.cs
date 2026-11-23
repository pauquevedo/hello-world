using System.Collections.Generic;
using UnityEngine;

namespace DragonMasters
{
    /// <summary>Carga los sprites recortados de Resources/Sprites y crea sprites generados (blanco, círculo, degradado).</summary>
    public static class Art
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static Sprite white, circle;

        public static Sprite Get(string path)
        {
            if (cache.TryGetValue(path, out var s)) return s;
            var tex = Resources.Load<Texture2D>("Sprites/" + path);
            if (tex == null)
            {
                Debug.LogWarning("[DragonMasters] Sprite no encontrado: " + path);
                return White;
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            // Botones y marcos: 9-slice para poder estirarlos sin deformar las esquinas
            Vector4 border = Vector4.zero;
            if (path.StartsWith("ui_buttons/btn_") || path == "ui_buttons/panel_frame")
                border = new Vector4(tex.width * 0.28f, tex.height * 0.3f, tex.width * 0.28f, tex.height * 0.3f);
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), PivotFor(path), 200f, 0, SpriteMeshType.FullRect, border);
            s.name = path;
            cache[path] = s;
            return s;
        }

        // Personajes, edificios y props se apoyan en el suelo (pivote abajo); efectos, objetos y armas giran sobre su centro.
        static Vector2 PivotFor(string path)
        {
            if (path.StartsWith("effects_vfx") || path.StartsWith("items_collectibles") ||
                path.StartsWith("weapons") || path.StartsWith("ui_buttons"))
                return new Vector2(0.5f, 0.5f);
            if (path.StartsWith("vehicles_boats")) return new Vector2(0.5f, 0.15f);
            return new Vector2(0.5f, 0.02f);
        }

        public static Sprite White
        {
            get
            {
                if (white == null)
                {
                    var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    var px = new Color32[16];
                    for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
                    t.SetPixels32(px); t.Apply();
                    white = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                    white.name = "white";
                }
                return white;
            }
        }

        /// <summary>Círculo suave de 1 unidad de diámetro.</summary>
        public static Sprite Circle
        {
            get
            {
                if (circle == null)
                {
                    const int n = 64;
                    var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                    var px = new Color32[n * n];
                    for (int y = 0; y < n; y++)
                        for (int x = 0; x < n; x++)
                        {
                            float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                            float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                            byte a = (byte)(Mathf.Clamp01((1f - d) * 20f) * 255);
                            px[y * n + x] = new Color32(255, 255, 255, a);
                        }
                    t.SetPixels32(px); t.Apply();
                    circle = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
                    circle.name = "circle";
                }
                return circle;
            }
        }

        /// <summary>Degradado vertical de 1x1 unidades (abajo -> arriba).</summary>
        public static Sprite Gradient(Color bottom, Color top)
        {
            const int h = 128;
            var t = new Texture2D(2, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[2 * h];
            for (int y = 0; y < h; y++)
            {
                var c = Color.Lerp(bottom, top, Mathf.SmoothStep(0, 1, y / (float)(h - 1)));
                px[y * 2] = c; px[y * 2 + 1] = c;
            }
            t.SetPixels(px); t.Apply();
            return Sprite.Create(t, new Rect(0, 0, 2, h), new Vector2(0.5f, 0.5f), h);
        }

        /// <summary>Crea un SpriteRenderer escalado para que mida 'height' unidades de alto.</summary>
        public static SpriteRenderer Make(string name, Sprite sprite, Transform parent, Vector3 pos, float height, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            FitHeight(sr, height);
            return sr;
        }

        public static SpriteRenderer Make(string name, string path, Transform parent, Vector3 pos, float height, int order)
            => Make(name, Get(path), parent, pos, height, order);

        public static void FitHeight(SpriteRenderer sr, float height)
        {
            float h = sr.sprite.bounds.size.y;
            float s = h > 0 ? height / h : 1f;
            sr.transform.localScale = new Vector3(s, s, 1);
        }

        public static void FitSize(SpriteRenderer sr, float maxSize)
        {
            var b = sr.sprite.bounds.size;
            float s = maxSize / Mathf.Max(b.x, b.y, 0.0001f);
            sr.transform.localScale = new Vector3(s, s, 1);
        }

        /// <summary>Rectángulo sólido (para suelo, acantilados, mar...). Pivote centrado.</summary>
        public static SpriteRenderer Rect(string name, Transform parent, Vector2 min, Vector2 max, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = (min + max) * 0.5f;
            go.transform.localScale = new Vector3(max.x - min.x, max.y - min.y, 1);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = White; sr.color = color; sr.sortingOrder = order;
            return sr;
        }
    }
}
