using System.Collections;
using UnityEngine;

namespace DragonMasters
{
    /// <summary>
    /// Cámara 2D ortográfica: encuadres fijos, seguimiento del proyectil, barridos rápidos de un lado al otro y temblor.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class CameraRig : MonoBehaviour
    {
        public Camera Cam { get; private set; }

        Vector2 pos, target;
        float size, targetSize;
        float followSharpness = 6f;
        Transform follow;
        System.Func<Vector2> followFn;
        float shake;
        bool panning;

        public float HalfWidth => Cam.orthographicSize * Cam.aspect;

        void Awake()
        {
            Cam = GetComponent<Camera>();
            size = targetSize = Cam.orthographicSize;
            pos = target = transform.position;
        }

        public void SetImmediate(Vector2 center, float orthoSize)
        {
            StopFollow();
            panning = false;
            pos = target = center;
            size = targetSize = orthoSize;
            Apply(Vector2.zero);
        }

        public void MoveTo(Vector2 center, float orthoSize, float sharpness = 6f)
        {
            StopFollow();
            target = center; targetSize = orthoSize; followSharpness = sharpness;
        }

        /// <summary>Sigue una función (p.ej. la posición del proyectil + adelanto).</summary>
        public void Follow(System.Func<Vector2> fn, float sharpness = 8f)
        {
            followFn = fn; follow = null; followSharpness = sharpness; panning = false;
        }

        public void SetTargetSize(float s) => targetSize = s;

        public void StopFollow() { followFn = null; follow = null; }

        /// <summary>Barrido de cámara con aceleración/frenado (la cámara "corre" hasta el otro lado).</summary>
        public IEnumerator Pan(Vector2 to, float toSize, float duration)
        {
            StopFollow();
            panning = true;
            Vector2 from = pos; float fromSize = size;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, Mathf.SmoothStep(0, 1, t / duration));
                pos = target = Vector2.LerpUnclamped(from, to, k);
                // un pequeño "zoom out" a mitad de recorrido para dar sensación de velocidad
                float bump = Mathf.Sin(k * Mathf.PI) * 0.8f;
                size = targetSize = Mathf.Lerp(fromSize, toSize, k) + bump;
                yield return null;
            }
            pos = target = to; size = targetSize = toSize;
            panning = false;
        }

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (!panning)
            {
                if (followFn != null) target = followFn();
                float k = 1f - Mathf.Exp(-followSharpness * dt);
                pos = Vector2.Lerp(pos, target, k);
                size = Mathf.Lerp(size, targetSize, 1f - Mathf.Exp(-3f * dt));
            }
            Vector2 offset = Vector2.zero;
            if (shake > 0.001f)
            {
                offset = Random.insideUnitCircle * shake;
                shake = Mathf.MoveTowards(shake, 0, dt * 1.8f);
            }
            Apply(offset);
        }

        void Apply(Vector2 offset)
        {
            transform.position = new Vector3(pos.x + offset.x, pos.y + offset.y, -10f);
            Cam.orthographicSize = size;
        }
    }
}
