using UnityEngine;

namespace DragonRunner
{
    /// <summary>Efecto de un solo sprite que crece, sube un poco y se desvanece.</summary>
    public class FxBurst : MonoBehaviour
    {
        float life, age, startScale, rise;
        SpriteRenderer sr;

        public static void Spawn(string sprite, Vector3 pos, float height, float life = 0.45f, float rise = 0.6f)
        {
            var r = Art.Make("fx", Art.Get(sprite), height, null, 50);
            r.transform.position = pos;
            var fx = r.gameObject.AddComponent<FxBurst>();
            fx.sr = r;
            fx.life = life;
            fx.rise = rise;
            fx.startScale = r.transform.localScale.x;
        }

        void Update()
        {
            age += Time.deltaTime;
            float k = age / life;
            if (k >= 1f) { Destroy(gameObject); return; }
            transform.localScale = Vector3.one * startScale * (0.6f + 0.6f * Mathf.Sqrt(k));
            transform.position += Vector3.up * rise * Time.deltaTime;
            var c = sr.color; c.a = 1f - k * k; sr.color = c;
        }
    }
}
