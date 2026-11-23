using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DragonMasters
{
    /// <summary>
    /// Combate por turnos estilo "arquero": arrastras hacia atrás para apuntar (la cámara solo te enseña a ti),
    /// sueltas, y la cámara corre siguiendo el arma hasta el enemigo. Luego el enemigo responde.
    /// </summary>
    public class Battle : MonoBehaviour
    {
        const float FrameSize = 4.8f;
        const float MinSpeed = 8f, MaxSpeed = 30f;
        const float PlatformHalf = 2.4f;

        public float MinX { get; private set; }
        public float MaxX { get; private set; }

        int level, round;
        float enemyX, platformH;
        EnemyDef edef;
        HeroDef hero;
        WeaponDef heroWeapon, enemyWeapon;
        Fighter player, enemy;
        float enemyDmgMult;
        int enemyMisses, coinsEarned, headshots;
        CameraRig rig;

        // apuntado
        bool aiming, dragging, fired;
        Vector2 dragStart, aimVel;
        float aimPower, aimAngle;
        readonly Vector3[] pts = new Vector3[24];
        SpriteRenderer[] dots, ghostDots;
        Vector3[] ghostPts; int ghostCount;

        // HUD
        GameObject hud;
        HpBar playerBar, enemyBar;
        Text playerHpText, enemyHpText, hintText, aimText, coinsText;
        GameObject pausePanel;
        bool paused, finished;

        public float GroundAt(float x) => (platformH > 0 && Mathf.Abs(x - enemyX) <= PlatformHalf) ? platformH : 0f;

        public void Init(int levelIndex)
        {
            rig = GameManager.I.Rig;
            level = levelIndex;
            int n = Catalog.Enemies.Length;
            edef = Catalog.Enemies[level % n];
            round = level / n;
            hero = Catalog.Hero(Save.Hero);
            heroWeapon = Catalog.Weapon(hero.weapon);
            enemyWeapon = Catalog.Weapon(edef.weapon);

            var rnd = new System.Random(level * 31 + 7);
            enemyX = 22f + Mathf.Min(level, 8) * 1.2f + (float)rnd.NextDouble() * 4f;
            platformH = level == 0 || rnd.NextDouble() < 0.35 ? 0f : 0.8f + (float)rnd.NextDouble() * 2.6f;
            MinX = -6f; MaxX = enemyX + 6f;

            Scenery.Build(transform, edef.theme, MinX, MaxX, level * 13 + 1);
            Scenery.Platform(transform, edef.theme, enemyX, PlatformHalf, platformH);

            player = Fighter.Create(transform, hero.name, hero.sprite, Vector2.zero, 2.4f, 0, true, 100);
            int ehp = Mathf.RoundToInt(edef.hp * (1f + 0.35f * round));
            enemy = Fighter.Create(transform, edef.name, edef.sprite, new Vector2(enemyX, platformH), edef.height, edef.hover, false, ehp);
            enemy.HeadY = edef.headY;
            enemyDmgMult = edef.damage * (1f + 0.15f * round);

            dots = MakeDots(new Color(1, 1, 1, 0.95f));
            ghostDots = MakeDots(new Color(1, 1, 1, 0.3f));
            ghostPts = new Vector3[pts.Length];

            BuildHud();
            player.OnHpChanged = hp => { playerBar.Set(hp / (float)player.MaxHp); playerHpText.text = hp.ToString(); };
            enemy.OnHpChanged = hp => { enemyBar.Set(hp / (float)enemy.MaxHp); enemyHpText.text = hp.ToString(); };

            StartCoroutine(Run());
        }

        SpriteRenderer[] MakeDots(Color c)
        {
            var arr = new SpriteRenderer[pts.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = Art.Make("AimDot", Art.Circle, transform, Vector3.zero, 0.22f, Scenery.OrderFighters + 30);
                arr[i].color = c;
                arr[i].gameObject.SetActive(false);
            }
            return arr;
        }

        // ---------- Encuadres de cámara ----------

        Vector2 PlayerFrame()
        {
            float halfW = FrameSize * rig.Cam.aspect;
            return new Vector2(player.Feet.x + halfW * 0.42f, FrameSize * 0.62f - 0.2f);
        }

        Vector2 EnemyFrame()
        {
            float halfW = FrameSize * rig.Cam.aspect;
            return new Vector2(enemyX - halfW * 0.42f, platformH * 0.6f + FrameSize * 0.62f - 0.2f);
        }

        // ---------- Flujo de la partida ----------

        IEnumerator Run()
        {
            // Presentación: primero vemos al enemigo, y la cámara corre hasta nosotros.
            rig.SetImmediate(EnemyFrame(), FrameSize);
            Banner.Show(hud.transform, $"NIVEL {level + 1}\n<size=60>{edef.name}</size>", UI.Gold, 1.4f);
            yield return new WaitForSeconds(1.5f);
            yield return rig.Pan(PlayerFrame(), FrameSize, 1.1f);

            while (true)
            {
                yield return PlayerTurn();
                if (enemy.Dead) { yield return Victory(); yield break; }
                if (player.Dead) { yield return Defeat(); yield break; }

                yield return new WaitForSeconds(0.35f);
                yield return rig.Pan(EnemyFrame(), FrameSize, 0.9f);
                yield return EnemyTurn();
                if (player.Dead) { yield return Defeat(); yield break; }
                if (enemy.Dead) { yield return Victory(); yield break; }

                yield return new WaitForSeconds(0.35f);
                yield return rig.Pan(PlayerFrame(), FrameSize, 0.9f);
            }
        }

        IEnumerator PlayerTurn()
        {
            Banner.Show(hud.transform, "¡TU TURNO!", Color.white, 0.9f);
            rig.MoveTo(PlayerFrame(), FrameSize);
            fired = false; dragging = false; aimPower = 0;
            aimAngle = 40f;
            player.ShowHeld(heroWeapon, aimAngle);
            SetGhostVisible(true);
            hintText.gameObject.SetActive(!Save.TutorialDone);
            aiming = true;
            while (!fired) yield return null;
            aiming = false;
            hintText.gameObject.SetActive(false);
            aimText.gameObject.SetActive(false);
            SetDots(dots, pts, 0);
            SetGhostVisible(false);
            Save.TutorialDone = true;

            // guardar el tiro como "fantasma" para el siguiente turno
            ghostCount = Projectile.Predict(player.HandPos, aimVel, 0.55f, ghostPts, 0.55f, GroundAt);

            player.HideHeld();
            player.SetAimLean(0);
            player.ThrowKick();
            Sfx.Play("throw");
            var proj = Projectile.Launch(this, heroWeapon, player, enemy, player.HandPos, aimVel, 1f);
            yield return FollowShot(proj, true);
        }

        IEnumerator EnemyTurn()
        {
            Banner.Show(hud.transform, $"Turno de {edef.name}", new Color(1f, 0.7f, 0.6f), 0.9f);
            yield return new WaitForSeconds(0.45f);

            Vector2 from = enemy.HandPos;
            Vector2 to = player.BodyCenter + new Vector2(0, Random.Range(-0.3f, 1.1f));
            float ang = Random.Range(35f, 62f);
            float v = SolveSpeed(from, to, ref ang);
            float err = edef.error * Mathf.Max(0.4f, 1f - 0.25f * round) * Mathf.Pow(0.7f, enemyMisses);
            if (level == 0 && enemyMisses == 0) err *= 1.4f;
            v *= 1f + Random.Range(-1f, 1f) * err;
            ang += Random.Range(-1f, 1f) * err * 12f;
            v = Mathf.Clamp(v, MinSpeed, MaxSpeed * 1.1f);

            // preparación: el enemigo echa el brazo atrás
            for (float t = 0; t < 0.6f; t += Time.deltaTime)
            {
                float k = t / 0.6f;
                enemy.ShowHeld(enemyWeapon, 180f - ang - Mathf.Sin(k * Mathf.PI) * 25f);
                enemy.SetAimLean(k);
                yield return null;
            }
            enemy.HideHeld();
            enemy.SetAimLean(0);
            enemy.ThrowKick();
            Sfx.Play("throw");
            float rad = ang * Mathf.Deg2Rad;
            var vel = new Vector2(-Mathf.Cos(rad), Mathf.Sin(rad)) * v;
            var proj = Projectile.Launch(this, enemyWeapon, enemy, player, enemy.HandPos, vel, enemyDmgMult);
            yield return FollowShot(proj, false);
            bool hit = proj != null && (proj.Result.kind == HitKind.Body || proj.Result.kind == HitKind.Head);
            enemyMisses = hit ? 0 : enemyMisses + 1;
        }

        /// <summary>Velocidad necesaria para pasar por 'to' con el ángulo dado (lanzando hacia la izquierda).</summary>
        static float SolveSpeed(Vector2 from, Vector2 to, ref float angDeg)
        {
            float g = -Projectile.Gravity;
            float dx = Mathf.Abs(to.x - from.x);
            float dy = to.y - from.y;
            for (int i = 0; i < 20; i++)
            {
                float th = angDeg * Mathf.Deg2Rad;
                float c = Mathf.Cos(th);
                float denom = 2f * c * c * (dx * Mathf.Tan(th) - dy);
                if (denom > 0.01f) return Mathf.Sqrt(g * dx * dx / denom);
                angDeg += 4f;
            }
            return MaxSpeed;
        }

        IEnumerator FollowShot(Projectile proj, bool byPlayer)
        {
            Sfx.Play("whoosh", 0.8f);
            Vector2 last = proj.Position;
            rig.Follow(() =>
            {
                if (proj != null && !proj.Done) last = proj.Position + proj.Velocity * 0.12f;
                float s = Mathf.Clamp(5.2f + Mathf.Max(0, last.y - 5f) * 0.45f, 5.2f, 9.5f);
                rig.SetTargetSize(s);
                return new Vector2(last.x, Mathf.Max(s * 0.62f - 0.2f, last.y - s * 0.4f));
            }, 7f);

            while (proj != null && !proj.Done) yield return null;
            var r = proj != null ? proj.Result : new ShotResult { kind = HitKind.Out };
            var weapon = byPlayer ? heroWeapon : enemyWeapon;

            switch (r.kind)
            {
                case HitKind.Head:
                    Sfx.Play("hit"); Sfx.Play("ding", 0.7f, 0);
                    PopFx.Spawn(weapon.hitFx, r.point, 2.4f, 0.7f);
                    PopFx.Spawn("effects_vfx/star_burst", r.point + Vector2.up * 0.4f, 1.6f, 0.6f);
                    FloatingText.Spawn("¡HEADSHOT!", r.point + Vector2.up * 1.2f, UI.Gold, 86, 1.4f);
                    FloatingText.Spawn("-" + r.damage, r.point + Vector2.up * 0.2f, new Color(1f, 0.4f, 0.35f), 70);
                    rig.Shake(0.45f);
                    Blood(r.point, 14);
                    if (byPlayer) { headshots++; AddCoins(5, r.point); }
#if UNITY_IOS || UNITY_ANDROID
                    Handheld.Vibrate();
#endif
                    break;
                case HitKind.Body:
                    Sfx.Play("hit");
                    PopFx.Spawn(weapon.hitFx, r.point, 1.8f, 0.6f);
                    FloatingText.Spawn("-" + r.damage, r.point + Vector2.up * 0.6f, new Color(1f, 0.45f, 0.4f), 64);
                    rig.Shake(0.25f);
                    Blood(r.point, 7);
                    if (byPlayer) AddCoins(2, r.point);
                    break;
                case HitKind.Ground:
                    Sfx.Play("thud");
                    PopFx.Spawn("effects_vfx/smoke", r.point + Vector2.up * 0.3f, 1.3f, 0.6f);
                    FloatingText.Spawn(Random.value < 0.5f ? "¡Fallo!" : "¡Casi!", r.point + Vector2.up * 1.2f, Color.white, 54, 1f);
                    break;
                case HitKind.Out:
                    FloatingText.Spawn("¡Fuera!", (Vector3)last + Vector3.down * 1f, Color.white, 54, 1f);
                    break;
            }
            yield return new WaitForSeconds(r.kind == HitKind.Head ? 1.1f : 0.8f);
        }

        void Blood(Vector2 p, int n)
        {
            // "salpicadura" de estrellitas en vez de sangre: es un juego para todos
            for (int i = 0; i < n; i++)
            {
                var c = Color.HSVToRGB(Random.value, 0.6f, 1f);
                var part = Particle.Spawn(Art.Circle, p, Random.Range(0.12f, 0.28f), Random.insideUnitCircle * 6f + Vector2.up * 3f, Random.Range(0.5f, 0.9f), Scenery.OrderFighters + 25, c, transform);
                part.shrink = 0.6f;
            }
        }

        void AddCoins(int amount, Vector2 at)
        {
            coinsEarned += amount;
            coinsText.text = (Save.Coins + coinsEarned).ToString();
            for (int i = 0; i < Mathf.Min(amount, 6); i++)
            {
                var c = Particle.Spawn(Art.Get("items_collectibles/coin"), at, 0.5f, new Vector2(Random.Range(-3f, 3f), Random.Range(5f, 9f)), 1.2f, Scenery.OrderFighters + 40, Color.white, transform);
                c.spin = Random.Range(-360f, 360f);
            }
            Sfx.Play("coin", 0.6f);
        }

        IEnumerator Victory()
        {
            finished = true;
            Sfx.Play("win");
            var c = enemy.BodyCenter;
            PopFx.Spawn("effects_vfx/confetti", c + Vector2.up * 1.5f, 4f, 1.4f);
            PopFx.Spawn("effects_vfx/explosion", c, 2.5f, 0.7f);
            int bonus = 25 + 10 * Mathf.Min(level, 9) + 20 * round;
            AddCoins(bonus, c);
            rig.MoveTo(EnemyFrame(), FrameSize * 0.9f, 3f);
            yield return new WaitForSeconds(1.8f);
            Save.Coins += coinsEarned;
            Save.Level = level + 1;
            Results.Show(hud.transform, true, coinsEarned, headshots, level);
        }

        IEnumerator Defeat()
        {
            finished = true;
            Sfx.Play("lose");
            rig.MoveTo(PlayerFrame(), FrameSize * 0.9f, 3f);
            yield return new WaitForSeconds(1.8f);
            Save.Coins += coinsEarned;
            Results.Show(hud.transform, false, coinsEarned, headshots, level);
        }

        // ---------- Entrada (arrastrar y soltar) ----------

        void Update()
        {
            if (aiming && !paused) HandleAim();
            if (aimText != null && aimText.gameObject.activeSelf) PlaceOverWorld(aimText.rectTransform, player.HeadCenter + Vector2.up * 1.1f);
            if (hintText != null && hintText.gameObject.activeSelf)
                hintText.transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 5f) * 0.04f);
        }

        static bool PointerOverUI()
        {
            var es = EventSystem.current;
            if (es == null) return false;
            if (Input.touchCount > 0) return es.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            return es.IsPointerOverGameObject();
        }

        void HandleAim()
        {
            if (Input.GetMouseButtonDown(0) && !PointerOverUI())
            {
                dragging = true;
                dragStart = Input.mousePosition;
            }
            if (!dragging) return;

            Vector2 cur = Input.mousePosition;
            Vector2 d = dragStart - cur; // tirar hacia atrás = lanzar hacia delante
            float maxPx = Screen.height * 0.38f;
            aimPower = Mathf.Clamp01(d.magnitude / maxPx);
            if (d.sqrMagnitude > 4f) aimAngle = Mathf.Clamp(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, -20f, 88f);
            float rad = aimAngle * Mathf.Deg2Rad;
            aimVel = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * Mathf.Lerp(MinSpeed, MaxSpeed, aimPower);

            int n = aimPower > 0.05f ? Projectile.Predict(player.HandPos, aimVel, 0.25f + aimPower * 0.35f, pts, 0.55f, GroundAt) : 0;
            SetDots(dots, pts, n);
            player.ShowHeld(heroWeapon, aimAngle);
            player.SetAimLean(aimPower);
            aimText.gameObject.SetActive(aimPower > 0.05f);
            aimText.text = $"{Mathf.RoundToInt(aimAngle)}°   {Mathf.RoundToInt(aimPower * 100)}%";

            if (Input.GetMouseButtonUp(0))
            {
                dragging = false;
                if (aimPower >= 0.12f) fired = true;
                else { SetDots(dots, pts, 0); player.SetAimLean(0); aimText.gameObject.SetActive(false); }
            }
        }

        void SetDots(SpriteRenderer[] arr, Vector3[] p, int n)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                bool on = i > 0 && i < n;
                arr[i].gameObject.SetActive(on);
                if (!on) continue;
                arr[i].transform.position = p[i];
                float k = 1f - i / (float)arr.Length;
                arr[i].transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 0.26f, k);
            }
        }

        void SetGhostVisible(bool on) => SetDots(ghostDots, ghostPts, on ? ghostCount : 0);

        void PlaceOverWorld(RectTransform rt, Vector2 world)
        {
            var sp = rig.Cam.WorldToScreenPoint(world);
            var parent = (RectTransform)rt.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, sp, null, out var local))
                rt.anchoredPosition = local;
        }

        // ---------- HUD ----------

        void BuildHud()
        {
            var root = UI.Stretch(GameManager.I.UiRoot, "BattleHUD");
            hud = root.gameObject;
            GameManager.I.RegisterScreen(hud);

            // Jugador (arriba izquierda)
            UI.Img(root, "ui_buttons/panel_frame", new Vector2(0, 1), new Vector2(110, -90), new Vector2(170, 150), false);
            UI.Img(root, hero.sprite, new Vector2(0, 1), new Vector2(110, -88), new Vector2(120, 120));
            UI.Label(root, hero.name, 40, Color.white, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(470, -48), new Vector2(560, 50));
            playerBar = UI.HpBar(root, new Vector2(0, 1), new Vector2(470, -110), new Vector2(560, 50), new Color(0.35f, 0.85f, 0.3f), false);
            UI.Img(root, "items_collectibles/heart", new Vector2(0, 1), new Vector2(200, -110), new Vector2(70, 70));
            playerHpText = UI.Label(playerBar.transform, player.Hp.ToString(), 36, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 50));

            // Enemigo (arriba derecha)
            UI.Img(root, "ui_buttons/panel_frame", new Vector2(1, 1), new Vector2(-110, -90), new Vector2(170, 150), false);
            UI.Img(root, edef.sprite, new Vector2(1, 1), new Vector2(-110, -88), new Vector2(120, 120));
            UI.Label(root, edef.name, 40, Color.white, TextAnchor.MiddleRight, new Vector2(1, 1), new Vector2(-470, -48), new Vector2(560, 50));
            enemyBar = UI.HpBar(root, new Vector2(1, 1), new Vector2(-470, -110), new Vector2(560, 50), new Color(0.95f, 0.35f, 0.3f), true);
            UI.Img(root, "items_collectibles/heart", new Vector2(1, 1), new Vector2(-200, -110), new Vector2(70, 70));
            enemyHpText = UI.Label(enemyBar.transform, enemy.Hp.ToString(), 36, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 50));

            // Centro: nivel, monedas y pausa
            UI.Label(root, $"NIVEL {level + 1}", 44, UI.Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -45), new Vector2(400, 60));
            UI.Img(root, "items_collectibles/coin", new Vector2(0.5f, 1), new Vector2(-60, -110), new Vector2(60, 60));
            coinsText = UI.Label(root, Save.Coins.ToString(), 44, Color.white, TextAnchor.MiddleLeft, new Vector2(0.5f, 1), new Vector2(60, -110), new Vector2(180, 60));
            UI.Button(root, "ui_buttons/icon_pause", null, new Vector2(1, 0), new Vector2(-90, 90), new Vector2(120, 120), TogglePause);

            hintText = UI.Label(root, "Arrastra hacia atrás para apuntar y suelta para lanzar", 46, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(1400, 80));
            aimText = UI.Label(root, "", 44, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 60));
            aimText.gameObject.SetActive(false);
            hintText.gameObject.SetActive(false);
        }

        void TogglePause()
        {
            if (finished) return;
            paused = !paused;
            Time.timeScale = paused ? 0 : 1;
            if (paused)
            {
                var dim = UI.Panel(hud.transform, new Color(0, 0, 0, 0.55f));
                pausePanel = dim.gameObject;
                UI.Img(dim.transform, "ui_buttons/panel_frame", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 640), false);
                UI.Label(dim.transform, "PAUSA", 96, UI.Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 190), new Vector2(600, 120));
                UI.Img(dim.transform, "dragon_expressions/sleeping", new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(260, 180));
                UI.Button(dim.transform, "ui_buttons/icon_play", null, new Vector2(0.5f, 0.5f), new Vector2(-190, -170), new Vector2(150, 150), TogglePause);
                UI.Button(dim.transform, "ui_buttons/icon_home", null, new Vector2(0.5f, 0.5f), new Vector2(0, -170), new Vector2(150, 150), () =>
                {
                    Time.timeScale = 1; GameManager.I.ShowMenu();
                });
                var snd = UI.Button(dim.transform, "ui_buttons/icon_sound", null, new Vector2(0.5f, 0.5f), new Vector2(190, -170), new Vector2(150, 150), null);
                var sndImg = snd.GetComponent<Image>();
                sndImg.color = Save.Muted ? new Color(1, 1, 1, 0.4f) : Color.white;
                snd.onClick.AddListener(() => { Save.Muted = !Save.Muted; sndImg.color = Save.Muted ? new Color(1, 1, 1, 0.4f) : Color.white; });
            }
            else if (pausePanel != null) Destroy(pausePanel);
        }

        void OnDestroy()
        {
            Time.timeScale = 1;
        }
    }

    /// <summary>Rótulo grande que aparece con un "pop" y se desvanece.</summary>
    public class Banner : MonoBehaviour
    {
        float t, life; Text text; Outline outline;

        public static void Show(Transform parent, string msg, Color color, float life)
        {
            var label = UI.Label(parent, msg, 96, color, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(1400, 260));
            label.supportRichText = true;
            var b = label.gameObject.AddComponent<Banner>();
            b.life = life; b.text = label; b.outline = label.GetComponent<Outline>();
        }

        void Update()
        {
            t += Time.deltaTime;
            float pop = t < 0.18f ? Mathf.Lerp(0.3f, 1.15f, t / 0.18f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((t - 0.18f) / 0.15f));
            transform.localScale = Vector3.one * pop;
            float a = t < life * 0.7f ? 1f : Mathf.Lerp(1f, 0f, (t - life * 0.7f) / (life * 0.3f));
            var c = text.color; c.a = a; text.color = c;
            if (outline) { var oc = outline.effectColor; oc.a = a; outline.effectColor = oc; }
            if (t >= life) Destroy(gameObject);
        }
    }
}
