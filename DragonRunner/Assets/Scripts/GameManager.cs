using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DragonRunner
{
    /// <summary>
    /// Runner infinito: el dragón corre solo, el mundo acelera, tú saltas, aleteas y planeas.
    /// Recoge monedas y gemas, pisa enemigos para eliminarlos y esquiva rocas, barriles y fantasmas.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        enum State { Title, Playing, Paused, Dying, GameOver }

        public static GameManager Instance { get; private set; }

        // Ajustes de juego
        const float GroundY = -3f;
        const float StartSpeed = 7f;
        const float MaxSpeed = 16f;
        const float Acceleration = 0.12f;   // unidades/s por segundo
        const int MaxLives = 3;

        public float WorldSpeed { get; private set; }
        public float LeftEdge { get; private set; }
        public float RightEdge { get; private set; }

        State state;
        Camera cam;
        DragonPlayer player;
        GameUI ui;
        AudioSource audioSource;
        readonly Dictionary<string, AudioClip> sfx = new Dictionary<string, AudioClip>();
        readonly List<Scroller> active = new List<Scroller>();
        readonly List<Scroller> scratch = new List<Scroller>();

        float distance, runTime, nextHazardAt, nextDecorAt, nextFarAt, nextCloudAt, nextTuftAt, shake, dyingTimer;
        int coins, lives, stomps, best;
        Vector3 camBase;

        static readonly Color Sky = Art.Hex("#8ED6F5");

        // ---------- Arranque ----------

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            best = PlayerPrefs.GetInt("dragonrunner.best", 0);

            SetupCamera();
            EnsureGlobalLight();
            BuildBackground();
            BuildSounds();

            var pgo = new GameObject("Dragon");
            player = pgo.AddComponent<DragonPlayer>();
            player.Init(GroundY);

            ui = new GameObject("GameUI").AddComponent<GameUI>();
            ui.transform.SetParent(transform, false);
            ui.Build();
            ui.OnPlay = StartRun;
            ui.OnPause = () => SetPaused(true);
            ui.OnResume = () => SetPaused(false);
            ui.OnHome = GoTitle;

            UpdateEdges();
            PrewarmDecor();
            GoTitle();
        }

        void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.gameObject.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Sky;
            camBase = cam.transform.position;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        /// <summary>
        /// Con el renderizador 2D de URP los sprites "Lit" salen negros si no hay ninguna luz 2D.
        /// Si el proyecto usa URP y la escena no tiene luz, añadimos una Global Light 2D.
        /// </summary>
        void EnsureGlobalLight()
        {
            var lightType = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
            if (lightType == null) return;
            if (FindObjectsByType(lightType, FindObjectsSortMode.None).Length > 0) return;
            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent(lightType);
            var prop = lightType.GetProperty("lightType");
            if (prop != null && prop.CanWrite)
                prop.SetValue(light, Enum.Parse(prop.PropertyType, "Global"));
        }

        void BuildBackground()
        {
            // Degradado del cielo pegado a la cámara.
            var tex = new Texture2D(1, 64) { wrapMode = TextureWrapMode.Clamp };
            Color top = Art.Hex("#5BB8EE"), bottom = Art.Hex("#D6F2FF");
            for (int y = 0; y < 64; y++) tex.SetPixel(0, y, Color.Lerp(bottom, top, y / 63f));
            tex.Apply();
            var skySprite = Sprite.Create(tex, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f), 64);
            var sky = Art.Make("Sky", skySprite, 10.5f, cam.transform, -100);
            sky.transform.localPosition = new Vector3(0f, 0f, 10f);
            sky.transform.localScale = new Vector3(40f * 64f, sky.transform.localScale.y, 1f); // el sprite mide 1/64 de ancho

            // Suelo: hierba + tierra (colores sacados del estilo de Brand 01).
            Rect("Dirt", Art.Hex("#D9965A"), new Vector2(0f, GroundY - 3f), new Vector2(80f, 6f), 0);
            Rect("DirtBand", Art.Hex("#C27D44"), new Vector2(0f, GroundY - 1.3f), new Vector2(80f, 0.35f), 1);
            Rect("Grass", Art.Hex("#7BC950"), new Vector2(0f, GroundY - 0.22f), new Vector2(80f, 0.44f), 2);
            Rect("GrassEdge", Art.Hex("#4E9A34"), new Vector2(0f, GroundY - 0.47f), new Vector2(80f, 0.08f), 3);
        }

        void Rect(string name, Color c, Vector2 center, Vector2 size, int order)
        {
            var sr = Art.Make(name, Art.White, 1f, null, order);
            sr.color = c;
            sr.transform.position = center;
            sr.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        void BuildSounds()
        {
            sfx["jump"] = Art.Tone("jump", 0.14f, 320f, 640f, 0.18f, square: true);
            sfx["flap"] = Art.Tone("flap", 0.10f, 450f, 760f, 0.14f, square: true);
            sfx["coin"] = Art.Arpeggio("coin", new[] { 988f, 1319f }, 0.06f, 0.25f);
            sfx["gem"] = Art.Arpeggio("gem", new[] { 784f, 988f, 1175f, 1568f }, 0.07f, 0.25f);
            sfx["stomp"] = Art.Tone("stomp", 0.14f, 260f, 90f, 0.25f, square: true);
            sfx["hit"] = Art.Tone("hit", 0.25f, 0f, 0f, 0.35f, noise: true);
            sfx["over"] = Art.Arpeggio("over", new[] { 523f, 440f, 349f, 262f }, 0.16f, 0.3f);
            sfx["click"] = Art.Tone("click", 0.05f, 800f, 950f, 0.2f);
        }

        public void Sfx(string name)
        {
            if (sfx.TryGetValue(name, out var clip)) audioSource.PlayOneShot(clip);
        }

        // ---------- Estados ----------

        void GoTitle()
        {
            Time.timeScale = 1f;
            ClearHazards();
            state = State.Title;
            WorldSpeed = 3f;
            player.gameObject.SetActive(false);
            ui.ShowTitle(best);
        }

        void StartRun()
        {
            Time.timeScale = 1f;
            ClearHazards();
            distance = runTime = 0f;
            coins = stomps = 0;
            lives = MaxLives;
            WorldSpeed = StartSpeed;
            nextHazardAt = 12f;
            player.gameObject.SetActive(true);
            player.transform.position = new Vector3(LeftEdge + 3.2f, GroundY, 0f);
            player.ResetState();
            state = State.Playing;
            ui.ShowHud();
            ui.SetHud(0, 0, lives);
        }

        void SetPaused(bool paused)
        {
            if (paused && state != State.Playing) return;
            if (!paused && state != State.Paused) return;
            state = paused ? State.Paused : State.Playing;
            Time.timeScale = paused ? 0f : 1f;
            ui.ShowPause(paused);
        }

        void Die()
        {
            state = State.Dying;
            dyingTimer = 0.9f;
            player.Die();
            Sfx("over");
        }

        void FinishRun()
        {
            state = State.GameOver;
            int score = Score;
            bool newBest = score > best;
            if (newBest)
            {
                best = score;
                PlayerPrefs.SetInt("dragonrunner.best", best);
                PlayerPrefs.Save();
                FxBurst.Spawn("effects_vfx/confetti", player.transform.position + Vector3.up * 1.5f, 3f, 1.2f, 0.5f);
            }
            ui.ShowGameOver(score, best, Mathf.FloorToInt(distance), coins, newBest);
        }

        int Score => Mathf.FloorToInt(distance) + coins * 10 + stomps * 25;

        // ---------- Bucle ----------

        void Update()
        {
            UpdateEdges();
            float dt = Time.deltaTime;

            switch (state)
            {
                case State.Playing:
                    if (TapInput.PausePressed) { SetPaused(true); break; }
                    runTime += dt;
                    WorldSpeed = Mathf.Min(MaxSpeed, StartSpeed + runTime * Acceleration);
                    distance += WorldSpeed * dt;
                    player.Tick(dt, WorldSpeed);
                    SpawnHazards();
                    CheckCollisions();
                    ui.SetHud(Mathf.FloorToInt(distance), coins, lives);
                    break;
                case State.Paused:
                    if (TapInput.PausePressed) SetPaused(false);
                    break;
                case State.Dying:
                    WorldSpeed = Mathf.MoveTowards(WorldSpeed, 0f, 20f * dt);
                    dyingTimer -= dt;
                    if (dyingTimer <= 0f) FinishRun();
                    break;
                case State.GameOver:
                    WorldSpeed = 0f;
                    break;
            }

            SpawnDecor();
            UpdateShake(dt);
        }

        void UpdateEdges()
        {
            float half = cam.orthographicSize * cam.aspect;
            LeftEdge = camBase.x - half;
            RightEdge = camBase.x + half;
        }

        void UpdateShake(float dt)
        {
            if (shake > 0f)
            {
                shake = Mathf.Max(0f, shake - dt);
                cam.transform.position = camBase + (Vector3)(Random.insideUnitCircle * shake * 0.8f);
            }
            else cam.transform.position = camBase;
        }

        // ---------- Colisiones ----------

        void CheckCollisions()
        {
            scratch.Clear();
            scratch.AddRange(active);
            Vector2 pc = player.HitCenter;
            foreach (var s in scratch)
            {
                if (s == null || !s.alive || s.kind == Kind.Decor) continue;
                if ((s.HitCenter - pc).sqrMagnitude > (s.radius + player.Radius) * (s.radius + player.Radius)) continue;

                switch (s.kind)
                {
                    case Kind.Coin:
                        coins++;
                        Sfx("coin");
                        FxBurst.Spawn("effects_vfx/star_burst", s.transform.position, 0.9f, 0.3f);
                        Despawn(s);
                        break;
                    case Kind.Gem:
                        coins += 10;
                        Sfx("gem");
                        player.Happy();
                        FxBurst.Spawn("effects_vfx/confetti", s.transform.position, 1.8f, 0.6f);
                        Despawn(s);
                        break;
                    case Kind.Heart:
                        lives = Mathf.Min(MaxLives, lives + 1);
                        Sfx("gem");
                        player.Happy();
                        FxBurst.Spawn("effects_vfx/magic_swirl", s.transform.position, 1.4f, 0.5f);
                        Despawn(s);
                        break;
                    case Kind.Enemy when player.Falling && pc.y > s.HitCenter.y + 0.15f:
                        stomps++;
                        Sfx("stomp");
                        player.Bounce();
                        FxBurst.Spawn("effects_vfx/impact", s.HitCenter, 1.3f, 0.35f, 0f);
                        FxBurst.Spawn("effects_vfx/star_burst", s.HitCenter + Vector2.up * 0.5f, 1f, 0.5f);
                        Despawn(s);
                        break;
                    default:
                        Hurt(s);
                        break;
                }
                if (state != State.Playing) return;
            }
        }

        void Hurt(Scroller by)
        {
            if (player.Invulnerable > 0f) return;
            lives--;
            Sfx("hit");
            shake = 0.3f;
            FxBurst.Spawn("effects_vfx/impact", player.HitCenter, 1.6f, 0.35f, 0f);
            if (by.kind == Kind.Flyer && by.name.Contains("storm"))
                FxBurst.Spawn("effects_vfx/lightning", player.HitCenter, 1.6f, 0.35f, 0f);
            ui.SetHud(Mathf.FloorToInt(distance), coins, lives);
            if (lives <= 0) Die();
            else player.Hurt();
        }

        // ---------- Generación de peligros y monedas ----------

        struct Def
        {
            public string sprite; public float height, minTime, extraSpeed;
            public Def(string s, float h, float t, float extra = 0f) { sprite = s; height = h; minTime = t; extraSpeed = extra; }
        }

        static readonly Def[] GroundObstacles =
        {
            new Def("environment_props/rocks", 1.0f, 0f),
            new Def("environment_props/barrel_crate", 1.15f, 0f),
            new Def("environment_props/fence", 1.0f, 10f),
            new Def("environment_props/well", 1.9f, 40f),
        };

        static readonly Def[] GroundEnemies =
        {
            new Def("enemies/slime", 0.95f, 0f),
            new Def("enemies/mushroom", 1.25f, 5f),
            new Def("enemies/raccoon", 1.35f, 15f, 2.5f),
            new Def("enemies/robot", 1.45f, 30f, 1f),
            new Def("enemies/skeleton_pirate", 1.6f, 45f, 1.5f),
            new Def("enemies/golem", 1.9f, 70f),
        };

        static readonly Def[] Flyers =
        {
            new Def("enemies/ghost", 1.2f, 12f),
            new Def("enemies/storm_cloud", 1.3f, 25f),
        };

        void SpawnHazards()
        {
            if (distance < nextHazardAt) return;
            float x = RightEdge + 2f;
            float r = Random.value;

            if (r < 0.40f)
            {
                var d = Pick(GroundObstacles);
                SpawnGround(d, Kind.Obstacle, x);
                CoinArc(x, d.height);
            }
            else if (r < 0.75f)
            {
                var d = Pick(GroundEnemies);
                SpawnGround(d, Kind.Enemy, x);
                if (Random.value < 0.5f) CoinRow(x + 3f, GroundY + 0.5f, 4);
            }
            else if (runTime > 12f && r < 0.9f)
            {
                var d = Pick(Flyers);
                bool high = d.sprite.Contains("storm");
                float y = GroundY + (high ? 3.3f : 2.2f);
                var s = Spawn(d.sprite, Kind.Flyer, new Vector3(x, y, 0f), d.height, 1f, 12);
                s.bobAmplitude = high ? 0.25f : 0.45f;
                s.bobSpeed = high ? 1.5f : 3f;
                CoinRow(x - 1.5f, GroundY + 0.5f, 5);
                if (Random.value < 0.4f) SpawnGround(Pick(GroundObstacles), Kind.Obstacle, x + 5f);
            }
            else
            {
                CoinRow(x, GroundY + (Random.value < 0.5f ? 0.5f : 3.0f), 6);
            }

            // Premios raros
            if (Random.value < 0.08f) Spawn("items_collectibles/gem", Kind.Gem, new Vector3(x + 4f, GroundY + 3.4f, 0f), 0.8f, 1f, 11).bobAmplitude = 0.2f;
            else if (lives < MaxLives && Random.value < 0.05f) Spawn("items_collectibles/heart", Kind.Heart, new Vector3(x + 4f, GroundY + 3.0f, 0f), 0.75f, 1f, 11).bobAmplitude = 0.2f;

            // Cada vez menos hueco entre peligros (en segundos de carrera).
            float gapSeconds = Mathf.Lerp(1.6f, 0.85f, Mathf.Clamp01(runTime / 120f)) + Random.Range(0f, 0.6f);
            nextHazardAt = distance + Mathf.Max(6.5f, WorldSpeed * gapSeconds);
        }

        Def Pick(Def[] table)
        {
            int count = 0;
            foreach (var d in table) if (runTime >= d.minTime) count++;
            int idx = Random.Range(0, Mathf.Max(1, count));
            foreach (var d in table)
                if (runTime >= d.minTime && idx-- == 0) return d;
            return table[0];
        }

        void SpawnGround(Def d, Kind kind, float x)
        {
            var s = Spawn(d.sprite, kind, new Vector3(x, GroundY - 0.05f, 0f), d.height, 1f, 10);
            s.extraSpeed = d.extraSpeed;
            if (d.sprite.Contains("fence")) { s.radius = d.height * 0.42f; }
        }

        void CoinArc(float x, float obstacleHeight)
        {
            float peak = obstacleHeight + 1.4f;
            for (int i = -2; i <= 2; i++)
            {
                float k = 1f - (i * i) / 4.5f;
                Spawn("items_collectibles/coin", Kind.Coin, new Vector3(x + i * 0.85f, GroundY + 0.4f + peak * k, 0f), 0.55f, 1f, 11);
            }
        }

        void CoinRow(float x, float y, int n)
        {
            for (int i = 0; i < n; i++)
                Spawn("items_collectibles/coin", Kind.Coin, new Vector3(x + i * 0.85f, y, 0f), 0.55f, 1f, 11);
        }

        Scroller Spawn(string sprite, Kind kind, Vector3 pos, float height, float parallax, int order)
        {
            var sr = Art.Make(sprite, Art.Get(sprite), height, null, order);
            sr.transform.position = pos;
            var s = sr.gameObject.AddComponent<Scroller>();
            s.kind = kind;
            s.parallax = parallax;
            s.radius = kind == Kind.Coin ? 0.35f : height * 0.36f;
            s.hitOffset = new Vector2(0f, kind == Kind.Coin || kind == Kind.Gem || kind == Kind.Heart ? height * 0.5f : height * 0.42f);
            active.Add(s);
            return s;
        }

        public void Despawn(Scroller s)
        {
            if (s == null) return;
            s.alive = false;
            active.Remove(s);
            Destroy(s.gameObject);
        }

        void ClearHazards()
        {
            scratch.Clear();
            scratch.AddRange(active);
            foreach (var s in scratch)
                if (s != null && s.kind != Kind.Decor) Despawn(s);
        }

        // ---------- Decorado con parallax ----------

        static readonly string[] FarThings = { "buildings/castle", "buildings/windmill", "buildings/tower", "buildings/lighthouse", "buildings/house" };
        static readonly string[] MidThings = { "environment_props/pine", "environment_props/fruit_tree", "environment_props/palm", "environment_props/pine", "environment_props/flower_bush" };

        // Cada capa lleva su propio recorrido en pantalla (velocidad × parallax).
        float farTravel, cloudTravel, midTravel, tuftTravel;

        void PrewarmDecor()
        {
            // Rellena la pantalla de decorado para que el arranque no se vea vacío.
            float start = LeftEdge - 3f;
            for (float x = start; x < RightEdge + 4f; x += 0.25f)
            {
                float d = x - start;
                farTravel = cloudTravel = midTravel = tuftTravel = d;
                DecorAt(x);
            }
            farTravel = cloudTravel = midTravel = tuftTravel = 0f;
            nextFarAt -= RightEdge + 4f - start;
            nextCloudAt -= RightEdge + 4f - start;
            nextDecorAt -= RightEdge + 4f - start;
            nextTuftAt -= RightEdge + 4f - start;
        }

        void SpawnDecor()
        {
            float dt = Time.deltaTime;
            farTravel += WorldSpeed * 0.15f * dt;
            cloudTravel += WorldSpeed * 0.08f * dt;
            midTravel += WorldSpeed * 0.45f * dt;
            tuftTravel += WorldSpeed * dt;
            DecorAt(RightEdge + 4f);
        }

        void DecorAt(float x)
        {
            if (farTravel >= nextFarAt)
            {
                var name = FarThings[Random.Range(0, FarThings.Length)];
                SpawnDecor(name, x, GroundY + 0.1f, Random.Range(3.2f, 4.6f), 0.15f, -30, 0.55f);
                nextFarAt = farTravel + Random.Range(5f, 9f);
            }
            if (cloudTravel >= nextCloudAt)
            {
                var c = SpawnDecor("effects_vfx/smoke", x, Random.Range(1.8f, 3.8f), Random.Range(1.0f, 1.8f), 0.08f, -40, 0f);
                c.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.85f);
                nextCloudAt = cloudTravel + Random.Range(4f, 8f);
            }
            if (midTravel >= nextDecorAt)
            {
                var name = MidThings[Random.Range(0, MidThings.Length)];
                float h = name.Contains("bush") ? Random.Range(1.0f, 1.4f) : Random.Range(2.4f, 3.4f);
                SpawnDecor(name, x, GroundY + 0.05f, h, 0.45f, -20, 0.25f);
                nextDecorAt = midTravel + Random.Range(2.5f, 5f);
            }
            if (tuftTravel >= nextTuftAt)
            {
                var tuft = SpawnDecor(null, x, GroundY - Random.Range(0.8f, 2.6f), 0.12f, 1f, 1, 0f);
                tuft.GetComponent<SpriteRenderer>().color = Art.Hex("#C27D44");
                tuft.transform.localScale = new Vector3(Random.Range(0.5f, 1.4f), 0.12f, 1f);
                nextTuftAt = tuftTravel + Random.Range(0.8f, 2f);
            }
        }

        Scroller SpawnDecor(string sprite, float x, float y, float height, float parallax, int order, float haze)
        {
            var sr = Art.Make(sprite ?? "tuft", sprite == null ? Art.White : Art.Get(sprite), height, null, order);
            sr.transform.position = new Vector3(x, y, 0f);
            if (haze > 0f) sr.color = Color.Lerp(Color.white, Sky, haze);
            var s = sr.gameObject.AddComponent<Scroller>();
            s.kind = Kind.Decor;
            s.parallax = parallax;
            active.Add(s);
            return s;
        }
    }
}
