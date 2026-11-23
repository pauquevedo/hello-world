using UnityEngine;

namespace DragonMasters
{
    public enum HitKind { None, Body, Head, Ground, Out }

    public struct ShotResult
    {
        public HitKind kind;
        public Vector2 point;
        public int damage;
    }

    /// <summary>Proyectil con física balística propia (determinista y fácil de predecir para la línea de apuntado).</summary>
    public class Projectile : MonoBehaviour
    {
        public const float Gravity = -18f;

        public Vector2 Velocity;
        public bool Done { get; private set; }
        public ShotResult Result;

        WeaponDef def;
        Fighter target, owner;
        Battle battle;
        SpriteRenderer sr;
        float angle, age, trailT;
        float radius;
        float damageMult;

        public static Projectile Launch(Battle battle, WeaponDef def, Fighter owner, Fighter target, Vector2 from, Vector2 vel, float damageMult)
        {
            var sr = Art.Make("Projectile", def.sprite, battle.transform, from, 1, Scenery.OrderFighters + 20);
            Art.FitSize(sr, def.size);
            sr.color = def.tint;
            var p = sr.gameObject.AddComponent<Projectile>();
            p.def = def; p.owner = owner; p.target = target; p.battle = battle; p.sr = sr;
            p.Velocity = vel; p.radius = def.size * 0.28f; p.damageMult = damageMult;
            p.angle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
            p.UpdateRotation();
            return p;
        }

        public Vector2 Position => transform.position;

        void Update()
        {
            if (Done) return;
            float dt = Time.deltaTime;
            const int steps = 4;
            float h = dt / steps;
            for (int i = 0; i < steps && !Done; i++)
            {
                age += h;
                Velocity.y += Gravity * h;
                Vector2 p = (Vector2)transform.position + Velocity * h;
                transform.position = p;
                Collide(p);
            }
            if (def.mode != SpinMode.Arrow) angle += -Mathf.Sign(Velocity.x) * def.spin * dt;
            UpdateRotation();

            // estela de puntitos
            trailT += dt;
            if (!Done && trailT > 0.025f)
            {
                trailT = 0;
                var dot = Particle.Spawn(Art.Circle, transform.position, 0.18f, Vector2.zero, 0.45f, Scenery.OrderFighters + 19, new Color(1, 1, 1, 0.7f), battle.transform);
                dot.gravity = 0; dot.fadeFrom = 0.1f; dot.shrink = 0.8f;
            }
        }

        void UpdateRotation()
        {
            if (Done) return;
            float a = def.mode == SpinMode.Arrow ? Mathf.Atan2(Velocity.y, Velocity.x) * Mathf.Rad2Deg - def.baseAngle : angle;
            transform.rotation = Quaternion.Euler(0, 0, a);
        }

        void Collide(Vector2 p)
        {
            int hit = target.HitTest(p, radius);
            if (hit == 0 && age > 0.35f) hit = -owner.HitTest(p, radius); // ¡te puedes dar a ti mismo!
            if (hit != 0)
            {
                var victim = hit > 0 ? target : owner;
                bool head = Mathf.Abs(hit) == 2;
                float baseDmg = head ? 50f : 25f;
                int dmg = Mathf.RoundToInt(baseDmg * def.damage * damageMult * Random.Range(0.9f, 1.1f));
                Finish(head ? HitKind.Head : HitKind.Body, p, dmg);
                victim.TakeHit(dmg, Velocity, head);
                // el arma se queda clavada en el luchador
                transform.SetParent(victim.Visual, true);
                return;
            }
            if (p.y < battle.GroundAt(p.x))
            {
                transform.position = new Vector3(p.x, battle.GroundAt(p.x) + 0.05f, 0);
                Finish(HitKind.Ground, p, 0);
                return;
            }
            if (p.x < battle.MinX - 25 || p.x > battle.MaxX + 25 || age > 9f)
            {
                Finish(HitKind.Out, p, 0);
            }
        }

        void Finish(HitKind kind, Vector2 p, int dmg)
        {
            Done = true;
            Result = new ShotResult { kind = kind, point = p, damage = dmg };
            if (kind == HitKind.Out) { Destroy(gameObject); return; }
            // clavado con su ángulo
            if (def.mode == SpinMode.Arrow) transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(Velocity.y, Velocity.x) * Mathf.Rad2Deg - def.baseAngle);
            if (kind == HitKind.Ground)
            {
                sr.sortingOrder = Scenery.OrderFighters - 2;
                if (def.mode != SpinMode.Arrow) transform.rotation = Quaternion.Euler(0, 0, Random.Range(-25f, 25f));
            }
        }

        /// <summary>Simula la trayectoria (para la línea de puntos de apuntado).</summary>
        public static int Predict(Vector2 from, Vector2 vel, float maxTime, Vector3[] outPoints, float spacing, System.Func<float, float> groundAt)
        {
            int n = 0;
            Vector2 p = from;
            float dist = 0;
            const float dt = 1f / 120f;
            outPoints[n++] = p;
            for (float t = 0; t < maxTime && n < outPoints.Length; t += dt)
            {
                vel.y += Gravity * dt;
                Vector2 np = p + vel * dt;
                dist += (np - p).magnitude;
                p = np;
                if (p.y < groundAt(p.x)) break;
                if (dist >= spacing) { outPoints[n++] = p; dist = 0; }
            }
            return n;
        }
    }
}
