using UnityEngine;
using UnityEngine.UI;

namespace DragonMasters
{
    /// <summary>Efecto de sprite que "explota": crece, gira un poco y se desvanece.</summary>
    public class PopFx : MonoBehaviour
    {
        SpriteRenderer sr;
        float life, t, size, spin;
        Color baseColor;

        public static PopFx Spawn(string sprite, Vector2 pos, float size, float life = 0.6f, int order = 40, Transform parent = null)
        {
            var srr = Art.Make("fx", sprite, parent, pos, size, order);
            Art.FitSize(srr, size);
            var fx = srr.gameObject.AddComponent<PopFx>();
            fx.sr = srr; fx.life = life; fx.size = srr.transform.localScale.x;
            fx.spin = Random.Range(-60f, 60f);
            fx.baseColor = srr.color;
            srr.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));
            srr.transform.localScale = Vector3.one * fx.size * 0.3f;
            return fx;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = t / life;
            float s = k < 0.25f ? Mathf.Lerp(0.3f, 1.15f, k / 0.25f) : Mathf.Lerp(1.15f, 1.0f, (k - 0.25f) / 0.75f);
            transform.localScale = Vector3.one * size * s;
            transform.Rotate(0, 0, spin * Time.deltaTime);
            var c = baseColor; c.a = k < 0.6f ? 1 : Mathf.Lerp(1, 0, (k - 0.6f) / 0.4f);
            sr.color = c;
            if (t >= life) Destroy(gameObject);
        }
    }

    /// <summary>Partícula simple con gravedad (monedas, chispas, puntos de estela).</summary>
    public class Particle : MonoBehaviour
    {
        public Vector2 vel;
        public float gravity = -14f, life = 1f, spin, fadeFrom = 0.6f, shrink;
        float t; SpriteRenderer sr; Color c0; Vector3 s0;

        public static Particle Spawn(Sprite sprite, Vector2 pos, float size, Vector2 vel, float life, int order, Color color, Transform parent = null)
        {
            var sr = Art.Make("p", sprite, parent, pos, size, order);
            Art.FitSize(sr, size);
            sr.color = color;
            var p = sr.gameObject.AddComponent<Particle>();
            p.vel = vel; p.life = life; p.sr = sr; p.c0 = color; p.s0 = sr.transform.localScale;
            return p;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            vel.y += gravity * dt;
            transform.position += (Vector3)(vel * dt);
            transform.Rotate(0, 0, spin * dt);
            float k = t / life;
            var c = c0; c.a = c0.a * (k < fadeFrom ? 1 : Mathf.Lerp(1, 0, (k - fadeFrom) / (1 - fadeFrom)));
            sr.color = c;
            if (shrink > 0) transform.localScale = s0 * Mathf.Lerp(1, 1 - shrink, k);
            if (t >= life) Destroy(gameObject);
        }
    }

    /// <summary>Texto de UI que sigue una posición del mundo, sube y se desvanece (daño, ¡HEADSHOT!...).</summary>
    public class FloatingText : MonoBehaviour
    {
        Vector3 world; float t, life; Text text; RectTransform rt; Color c0; Outline outline;

        public static void Spawn(string msg, Vector3 worldPos, Color color, int fontSize = 64, float life = 1.2f)
        {
            if (GameManager.I == null) return;
            var label = UI.Label(GameManager.I.Canvas.transform, msg, fontSize, color, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 160));
            var ft = label.gameObject.AddComponent<FloatingText>();
            ft.world = worldPos; ft.life = life; ft.text = label; ft.rt = label.rectTransform; ft.c0 = color;
            ft.outline = label.GetComponent<Outline>();
            ft.Place();
        }

        void Place()
        {
            var cam = GameManager.I.Rig.Cam;
            Vector3 sp = cam.WorldToScreenPoint(world + Vector3.up * (t * 1.4f));
            var canvasRt = (RectTransform)GameManager.I.Canvas.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, sp, null, out var local);
            rt.anchoredPosition = local;
            float pop = t < 0.15f ? Mathf.Lerp(0.4f, 1.25f, t / 0.15f) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((t - 0.15f) / 0.2f));
            rt.localScale = Vector3.one * pop;
        }

        void Update()
        {
            t += Time.deltaTime;
            Place();
            float a = t < life * 0.6f ? 1 : Mathf.Lerp(1, 0, (t - life * 0.6f) / (life * 0.4f));
            var c = c0; c.a = a; text.color = c;
            if (outline) { var oc = outline.effectColor; oc.a = a; outline.effectColor = oc; }
            if (t >= life) Destroy(gameObject);
        }
    }

    /// <summary>Balanceo suave (barcos, espectadores, nubes).</summary>
    public class Bob : MonoBehaviour
    {
        public float amp = 0.1f, speed = 2f, rot = 0f, drift = 0f, phase;
        Vector3 basePos;
        void Start() { basePos = transform.localPosition; phase = Random.value * 10f; }
        void Update()
        {
            float t = Time.time * speed + phase;
            basePos.x += drift * Time.deltaTime;
            transform.localPosition = basePos + new Vector3(0, Mathf.Sin(t) * amp, 0);
            if (rot != 0) transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 0.8f) * rot);
        }
    }
}
