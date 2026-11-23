using UnityEngine;

namespace DragonRunner
{
    public enum Kind { Decor, Obstacle, Enemy, Flyer, Coin, Gem, Heart }

    /// <summary>Todo lo que se desplaza hacia la izquierda: obstáculos, enemigos, monedas y decorado.</summary>
    public class Scroller : MonoBehaviour
    {
        public Kind kind;
        public float parallax = 1f;      // 1 = se mueve con el suelo; menos = más lejos
        public float radius = 0.5f;      // radio de colisión
        public Vector2 hitOffset;        // centro de colisión respecto al pivote (pies)
        public float bobAmplitude;       // flotación vertical (fantasmas, monedas)
        public float bobSpeed = 3f;
        public float extraSpeed;         // enemigos que corren hacia el jugador
        public bool alive = true;

        float baseY, t;

        void Start()
        {
            baseY = transform.position.y;
            t = Random.value * 10f;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            float dt = Time.deltaTime;
            t += dt;
            var p = transform.position;
            p.x -= (gm.WorldSpeed * parallax + (gm.WorldSpeed > 0.1f ? extraSpeed : 0f)) * dt;
            if (bobAmplitude > 0f) p.y = baseY + Mathf.Sin(t * bobSpeed) * bobAmplitude;
            transform.position = p;

            if (p.x < gm.LeftEdge - 4f) gm.Despawn(this);
        }

        public Vector2 HitCenter => (Vector2)transform.position + hitOffset;
    }
}
