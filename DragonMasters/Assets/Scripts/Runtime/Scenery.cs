using UnityEngine;

namespace DragonMasters
{
    /// <summary>Capa que se desplaza con la cámara (0 = pegada al mundo, 1 = pegada a la cámara).</summary>
    public class Parallax : MonoBehaviour
    {
        public float factor;
        public float refY = 2.5f;
        Transform cam;

        void LateUpdate()
        {
            if (cam == null) { var c = Camera.main; if (c == null) return; cam = c.transform; }
            var p = cam.position;
            transform.position = new Vector3(p.x * factor, (p.y - refY) * factor, 0);
        }
    }

    /// <summary>Cielo en degradado que siempre cubre la cámara.</summary>
    public class SkyBackdrop : MonoBehaviour
    {
        SpriteRenderer sr;
        void Awake() { sr = GetComponent<SpriteRenderer>(); }
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            float h = cam.orthographicSize * 2f * 1.15f;
            float w = h * cam.aspect;
            var b = sr.sprite.bounds.size;
            transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 20f);
            transform.localScale = new Vector3(w / b.x, h / b.y, 1);
        }
    }

    /// <summary>Construye el escenario de cada nivel con los edificios, props, barcos y NPCs del Brand 01.</summary>
    public static class Scenery
    {
        public const int OrderSky = -1000, OrderClouds = -900, OrderHills = -800, OrderSea = -700, OrderBoats = -690,
            OrderBuildings = -500, OrderNpcs = -400, OrderProps = -100, OrderGround = 0, OrderFighters = 10;

        public static void Build(Transform root, Theme t, float minX, float maxX, int seed)
        {
            var rnd = new System.Random(seed);
            System.Func<float, float, float> R = (a, b) => a + (float)rnd.NextDouble() * (b - a);
            System.Func<string[], string> Pick = arr => arr[rnd.Next(arr.Length)];

            // Cielo
            var sky = Art.Make("Sky", Art.Gradient(t.skyBottom, t.skyTop), root, Vector3.zero, 1f, OrderSky);
            sky.gameObject.AddComponent<SkyBackdrop>();

            // Sol / luna y estrellas
            var skyLayer = Layer(root, "SkyLayer", 0.95f);
            var sun = Art.Make("Sun", Art.Circle, skyLayer, new Vector3(R(minX, maxX) * 0.05f + 6f, 9f, 0), t.night ? 2.2f : 3f, OrderSky + 5);
            sun.color = t.night ? new Color(1f, 0.97f, 0.85f) : new Color(1f, 0.95f, 0.6f, 0.95f);
            if (t.night)
            {
                for (int i = 0; i < 40; i++)
                {
                    var s = Art.Make("StarDot", Art.Circle, skyLayer, new Vector3(R(-25, 25), R(4, 14), 0), R(0.06f, 0.16f), OrderSky + 4);
                    s.color = new Color(1, 1, 1, R(0.5f, 1f));
                    var b = s.gameObject.AddComponent<Bob>(); b.amp = 0.02f; b.speed = R(1, 3);
                }
            }

            // Nubes (sprite de humo del Brand)
            var cloudLayer = Layer(root, "Clouds", 0.85f);
            for (int i = 0; i < 9; i++)
            {
                var c = Art.Make("Cloud", "effects_vfx/smoke", cloudLayer, new Vector3(R(-30, 30), R(6.5f, 12f), 0), R(1.6f, 3.2f), OrderClouds + i);
                c.color = t.night ? new Color(0.75f, 0.75f, 0.95f, 0.55f) : new Color(1, 1, 1, 0.85f);
                var b = c.gameObject.AddComponent<Bob>(); b.amp = 0.15f; b.speed = R(0.3f, 0.7f); b.drift = R(0.1f, 0.35f);
            }

            // Vehículos aéreos (dirigible)
            if (t.air.Length > 0)
            {
                var airLayer = Layer(root, "Air", 0.75f);
                for (int i = 0; i < 2; i++)
                {
                    var a = Art.Make("Airship", Pick(t.air), airLayer, new Vector3(R(-15, 15) + i * 18, R(8, 11), 0), R(1.8f, 2.6f), OrderClouds + 20);
                    a.flipX = rnd.Next(2) == 0;
                    var b = a.gameObject.AddComponent<Bob>(); b.amp = 0.25f; b.speed = 0.6f; b.drift = a.flipX ? -0.25f : 0.25f;
                }
            }

            // Colinas lejanas (círculos grandes)
            var hills = Layer(root, "Hills", 0.7f);
            float span = (maxX - minX) * 0.3f + 60f;
            for (float x = -span * 0.5f; x < span * 0.5f; x += R(6, 11))
            {
                float r = R(9, 16);
                var h = Art.Make("Hill", Art.Circle, hills, new Vector3(x, -r * 0.62f + 1.0f, 0), r, OrderHills + rnd.Next(5));
                h.color = Color.Lerp(t.hills, t.skyBottom, R(0.15f, 0.35f));
            }

            // Mar con barcos
            if (t.sea)
            {
                var seaLayer = Layer(root, "Sea", 0.55f);
                Art.Rect("Sea", seaLayer, new Vector2(-80, -20), new Vector2(80, 1.1f), new Color(0.2f, 0.55f, 0.85f), OrderSea);
                Art.Rect("SeaLight", seaLayer, new Vector2(-80, 0.95f), new Vector2(80, 1.15f), new Color(0.65f, 0.88f, 1f), OrderSea + 1);
                for (int i = 0; i < 4 && t.boats.Length > 0; i++)
                {
                    var bt = Art.Make("Boat", Pick(t.boats), seaLayer, new Vector3(-20 + i * 13 + R(-3, 3), 0.7f, 0), R(1.8f, 3.0f), OrderBoats + i);
                    bt.flipX = rnd.Next(2) == 0;
                    var b = bt.gameObject.AddComponent<Bob>(); b.amp = 0.08f; b.speed = 1.3f; b.rot = 3f;
                }
            }

            // Edificios a media distancia
            var bLayer = Layer(root, "Buildings", 0.45f);
            float bStart = minX * 0.55f - 12f, bEnd = maxX * 0.55f + 12f;
            for (float x = bStart; x < bEnd; x += R(7, 11))
            {
                var bd = Art.Make("Building", Pick(t.buildings), bLayer, new Vector3(x, 0.2f, 0), R(3.6f, 5.2f), OrderBuildings + rnd.Next(10));
                bd.color = Color.Lerp(Color.white, t.skyBottom, 0.18f);
                bd.flipX = rnd.Next(2) == 0;
            }

            // Espectadores (NPCs del Brand) animando
            if (t.npcs.Length > 0)
            {
                var nLayer = Layer(root, "Npcs", 0.25f);
                for (float x = minX * 0.75f - 6f; x < maxX * 0.75f + 6f; x += R(5, 9))
                {
                    var n = Art.Make("Npc", Pick(t.npcs), nLayer, new Vector3(x, 0.0f, 0), R(1.3f, 1.6f), OrderNpcs + rnd.Next(10));
                    n.color = Color.Lerp(Color.white, t.skyBottom, 0.1f);
                    n.flipX = rnd.Next(2) == 0;
                    var b = n.gameObject.AddComponent<Bob>(); b.amp = 0.06f; b.speed = R(3, 5);
                }
            }

            // Props en primer plano del suelo (detrás de los luchadores)
            var props = new GameObject("Props").transform; props.SetParent(root, false);
            for (float x = minX - 10f; x < maxX + 10f; x += R(3.5f, 6.5f))
            {
                string p = Pick(t.props);
                float hgt = p.Contains("tree") || p.Contains("pine") || p.Contains("palm") ? R(3.2f, 4.6f)
                    : p.Contains("bridge") || p.Contains("well") ? R(1.8f, 2.4f) : R(1.0f, 1.6f);
                var pr = Art.Make("Prop", p, props, new Vector3(x, -0.05f, 0), hgt, OrderProps + rnd.Next(20));
                pr.flipX = rnd.Next(2) == 0;
            }

            // Suelo
            var ground = new GameObject("Ground").transform; ground.SetParent(root, false);
            Art.Rect("Dirt", ground, new Vector2(minX - 60, -30), new Vector2(maxX + 60, 0), t.dirt, OrderGround);
            Art.Rect("Grass", ground, new Vector2(minX - 60, -0.35f), new Vector2(maxX + 60, 0.05f), t.grass, OrderGround + 1);
            Art.Rect("GrassLight", ground, new Vector2(minX - 60, -0.08f), new Vector2(maxX + 60, 0.05f), Color.Lerp(t.grass, Color.white, 0.3f), OrderGround + 2);
            for (int i = 0; i < 60; i++)
            {
                var pebble = Art.Make("Pebble", Art.Circle, ground, new Vector3(R(minX - 30, maxX + 30), R(-6f, -0.8f), 0), R(0.15f, 0.45f), OrderGround + 1);
                pebble.color = Color.Lerp(t.dirt, Color.black, R(0.1f, 0.3f));
            }
        }

        /// <summary>Plataforma de roca/acantilado sobre la que puede estar el enemigo.</summary>
        public static void Platform(Transform root, Theme t, float cx, float halfW, float height)
        {
            if (height <= 0.05f) return;
            var p = new GameObject("Platform").transform; p.SetParent(root, false);
            Art.Rect("Cliff", p, new Vector2(cx - halfW, -1f), new Vector2(cx + halfW, height), Color.Lerp(t.dirt, Color.black, 0.1f), OrderGround + 3);
            Art.Rect("CliffGrass", p, new Vector2(cx - halfW - 0.1f, height - 0.3f), new Vector2(cx + halfW + 0.1f, height + 0.05f), t.grass, OrderGround + 4);
            Art.Rect("CliffGrassLight", p, new Vector2(cx - halfW - 0.1f, height - 0.08f), new Vector2(cx + halfW + 0.1f, height + 0.05f), Color.Lerp(t.grass, Color.white, 0.3f), OrderGround + 5);
            var rocks = Art.Make("Rocks", "environment_props/rocks", p, new Vector3(cx - halfW, -0.1f, 0), Mathf.Min(height + 0.4f, 2.2f), OrderGround + 6);
            rocks.flipX = true;
        }

        static Transform Layer(Transform root, string name, float factor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.AddComponent<Parallax>().factor = factor;
            return go.transform;
        }
    }
}
