using System.Collections;
using UnityEngine;

namespace DragonMasters
{
    /// <summary>
    /// Un luchador (tu dragón o el enemigo). La raíz está a la altura de los pies; el hijo "Visual" se inclina,
    /// se aplasta y parpadea al recibir golpes, como un mini-ragdoll.
    /// </summary>
    public class Fighter : MonoBehaviour
    {
        public string DisplayName;
        public int MaxHp, Hp;
        public float Height, Hover, HeadY = 0.72f;
        public bool FacingRight;
        public bool Dead { get; private set; }
        public SpriteRenderer Body { get; private set; }
        public Transform Visual { get; private set; }
        public System.Action<int> OnHpChanged;

        SpriteRenderer held;
        float tilt, tiltVel, tiltTarget, flash, squash, idleT;
        Color baseColor = Color.white;

        public static Fighter Create(Transform parent, string name, string sprite, Vector2 feet, float height, float hover, bool facingRight, int hp)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            var f = go.AddComponent<Fighter>();
            f.DisplayName = name; f.Height = height; f.Hover = hover; f.FacingRight = facingRight;
            f.MaxHp = f.Hp = hp;

            var vis = new GameObject("Visual").transform;
            vis.SetParent(go.transform, false);
            vis.localPosition = new Vector3(0, hover, 0);
            f.Visual = vis;
            f.Body = Art.Make("Body", sprite, vis, Vector3.zero, height, Scenery.OrderFighters);
            f.Body.flipX = !facingRight;

            // sombra
            var sh = Art.Make("Shadow", Art.Circle, go.transform, new Vector3(0, 0.02f, 0), 1f, Scenery.OrderFighters - 1);
            sh.transform.localScale = new Vector3(height * 0.55f, height * 0.12f, 1);
            sh.color = new Color(0, 0, 0, 0.25f);
            return f;
        }

        public Vector2 Feet => transform.position;
        public Vector2 HeadCenter => (Vector2)transform.position + new Vector2(0, Hover + Height * HeadY);
        public float HeadRadius => Height * 0.2f;
        public Vector2 BodyCenter => (Vector2)transform.position + new Vector2(0, Hover + Height * 0.35f);
        public Vector2 HandPos => (Vector2)transform.position + new Vector2((FacingRight ? 1 : -1) * Height * 0.42f, Hover + Height * 0.55f);

        /// <summary>¿El punto toca al luchador? 2 = cabeza, 1 = cuerpo, 0 = nada.</summary>
        public int HitTest(Vector2 p, float radius)
        {
            if (Dead) return 0;
            if ((p - HeadCenter).sqrMagnitude < (HeadRadius + radius) * (HeadRadius + radius)) return 2;
            float hw = Height * 0.3f + radius;
            Vector2 feet = (Vector2)transform.position + new Vector2(0, Hover);
            if (p.x > feet.x - hw && p.x < feet.x + hw && p.y > feet.y - radius && p.y < feet.y + Height * (HeadY - 0.1f) + radius)
                return 1;
            return 0;
        }

        public void ShowHeld(WeaponDef w, float angleDeg)
        {
            if (held == null)
            {
                held = Art.Make("Held", w.sprite, transform, Vector3.zero, 1, Scenery.OrderFighters + 2);
            }
            held.sprite = Art.Get(w.sprite);
            Art.FitSize(held, w.size * 0.9f);
            held.color = w.tint;
            held.gameObject.SetActive(true);
            held.transform.position = HandPos + (Vector2)(Quaternion.Euler(0, 0, angleDeg) * Vector2.left * 0.35f);
            float rot = w.mode == SpinMode.Arrow ? angleDeg - w.baseAngle : angleDeg + (FacingRight ? 30 : -30);
            held.transform.rotation = Quaternion.Euler(0, 0, rot);
            held.flipX = false;
        }

        public void HideHeld() { if (held) held.gameObject.SetActive(false); }

        /// <summary>Inclinación hacia atrás mientras se apunta (0..1).</summary>
        public void SetAimLean(float k) => tiltTarget = (FacingRight ? 1 : -1) * 8f * k;

        public void ThrowKick()
        {
            tiltVel += (FacingRight ? -1 : 1) * 260f;
            squash = 0.12f;
        }

        public void TakeHit(int dmg, Vector2 projVel, bool head)
        {
            if (Dead) return;
            Hp = Mathf.Max(0, Hp - dmg);
            OnHpChanged?.Invoke(Hp);
            flash = 1f;
            squash = head ? 0.22f : 0.15f;
            float dir = Mathf.Sign(projVel.x);
            tiltVel += -dir * (head ? 520f : 360f);
            if (Hp <= 0) StartCoroutine(Die(dir));
        }

        IEnumerator Die(float dir)
        {
            Dead = true;
            HideHeld();
            float start = tilt;
            float end = -dir * 88f;
            Vector3 v0 = Visual.localPosition;
            for (float t = 0; t < 0.7f; t += Time.deltaTime)
            {
                float k = t / 0.7f;
                float bounce = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 2.5f)) * (1 - k) * 0.5f;
                tilt = Mathf.Lerp(start, end, 1 - (1 - k) * (1 - k));
                Visual.localPosition = new Vector3(v0.x + dir * k * 0.6f, Mathf.Lerp(v0.y, 0.1f, k) + bounce, 0);
                Visual.localRotation = Quaternion.Euler(0, 0, tilt);
                yield return null;
            }
            Body.color = new Color(0.75f, 0.75f, 0.8f);
        }

        void Update()
        {
            if (Dead) return;
            float dt = Time.deltaTime;
            idleT += dt;
            // muelle amortiguado para la inclinación
            float acc = -90f * (tilt - tiltTarget) - 9f * tiltVel;
            tiltVel += acc * dt;
            tilt += tiltVel * dt;
            tilt = Mathf.Clamp(tilt, -45f, 45f);
            Visual.localRotation = Quaternion.Euler(0, 0, tilt);

            squash = Mathf.MoveTowards(squash, 0, dt * 0.8f);
            float breathe = Mathf.Sin(idleT * 3f) * 0.025f;
            float sy = 1f + breathe - squash * Mathf.Cos(idleT * 30f);
            float sx = 1f - breathe * 0.5f + squash * 0.5f;
            Visual.localScale = new Vector3(sx, sy, 1);
            float hoverBob = Hover > 0 ? Mathf.Sin(idleT * 2f) * 0.15f : 0;
            Visual.localPosition = new Vector3(0, Hover + hoverBob, 0);

            flash = Mathf.MoveTowards(flash, 0, dt * 3f);
            Body.color = Color.Lerp(baseColor, new Color(1f, 0.35f, 0.35f), flash);
        }
    }
}
