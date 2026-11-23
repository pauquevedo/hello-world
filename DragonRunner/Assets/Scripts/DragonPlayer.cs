using UnityEngine;

namespace DragonRunner
{
    /// <summary>
    /// El dragón protagonista. Toca para saltar, toca otra vez en el aire para un segundo
    /// salto (aleteo) y mantén pulsado mientras cae para planear con las alas.
    /// </summary>
    public class DragonPlayer : MonoBehaviour
    {
        public const float Height = 1.7f;
        const float Gravity = -42f;
        const float JumpVelocity = 14.5f;
        const float FlapVelocity = 12f;
        const float GlideFallSpeed = -2.6f;

        public float GroundY;
        public float Radius => 0.62f;
        public Vector2 HitCenter => (Vector2)transform.position + new Vector2(0.05f, 0.8f);
        public bool Grounded { get; private set; } = true;
        public bool Falling => vy < 0f && !Grounded;
        public bool Gliding { get; private set; }
        public float Invulnerable { get; private set; }

        SpriteRenderer body;
        Transform visual;
        float vy, runPhase, faceTimer, baseScale;
        int airJumps;
        Sprite runSprite, jumpSprite, glideSprite, hurtSprite, sadSprite, happySprite;

        public void Init(float groundY)
        {
            GroundY = groundY;
            runSprite = Art.Get("dragon_expressions/point");
            jumpSprite = Art.Get("dragon_expressions/celebrate");
            glideSprite = Art.Get("dragon_costumes/aviator");
            hurtSprite = Art.Get("dragon_expressions/surprised");
            sadSprite = Art.Get("dragon_expressions/sad");
            happySprite = Art.Get("dragon_expressions/wink");

            body = Art.Make("Body", runSprite, Height, transform, 20);
            visual = body.transform;
            baseScale = visual.localScale.x;

            var shadow = Art.Make("Shadow", Art.White, 0.18f, transform, 19);
            shadow.color = new Color(0f, 0f, 0f, 0.22f);
            shadow.transform.localScale = new Vector3(1.1f, 0.18f, 1f);
            shadow.name = "Shadow";
            ResetState();
        }

        public void ResetState()
        {
            transform.position = new Vector3(transform.position.x, GroundY, 0f);
            vy = 0f;
            Grounded = true;
            Invulnerable = 0f;
            faceTimer = 0f;
            body.sprite = runSprite;
            body.color = Color.white;
            visual.localRotation = Quaternion.identity;
        }

        public void Tick(float dt, float worldSpeed)
        {
            if (TapInput.JumpPressed)
            {
                if (Grounded) { vy = JumpVelocity; Grounded = false; airJumps = 1; GameManager.Instance.Sfx("jump"); }
                else if (airJumps > 0) { vy = FlapVelocity; airJumps--; GameManager.Instance.Sfx("flap"); FxBurst.Spawn("effects_vfx/smoke", transform.position + Vector3.up * 0.3f, 0.7f, 0.35f, -0.5f); }
            }

            Gliding = !Grounded && vy < 0f && TapInput.JumpHeld;
            vy += Gravity * dt * (Gliding ? 0.25f : 1f);
            if (Gliding && vy < GlideFallSpeed) vy = GlideFallSpeed;

            var p = transform.position;
            p.y += vy * dt;
            if (p.y <= GroundY)
            {
                if (!Grounded) { Squash(1.25f, 0.8f); FxBurst.Spawn("effects_vfx/smoke", new Vector3(p.x - 0.4f, GroundY, 0f), 0.5f, 0.3f, 0.2f); }
                p.y = GroundY; vy = 0f; Grounded = true;
            }
            transform.position = p;

            // Sombra: se queda en el suelo y encoge con la altura.
            var shadow = transform.Find("Shadow");
            if (shadow != null)
            {
                float h = Mathf.Clamp01((p.y - GroundY) / 4f);
                shadow.position = new Vector3(p.x, GroundY + 0.02f, 0f);
                shadow.localScale = new Vector3(1.1f * (1f - 0.6f * h), 0.18f * (1f - 0.6f * h), 1f);
            }

            Animate(dt, worldSpeed);

            if (Invulnerable > 0f)
            {
                Invulnerable -= dt;
                body.color = (Mathf.Repeat(Invulnerable, 0.16f) < 0.08f) ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
                if (Invulnerable <= 0f) body.color = Color.white;
            }
        }

        void Animate(float dt, float worldSpeed)
        {
            if (faceTimer > 0f) faceTimer -= dt;
            else body.sprite = Grounded ? runSprite : (Gliding ? glideSprite : jumpSprite);

            // Escala que vuelve suavemente a la normal tras un "squash".
            var s = visual.localScale;
            s = Vector3.Lerp(s, Vector3.one * baseScale, 1f - Mathf.Exp(-14f * dt));

            if (Grounded)
            {
                // "Carrera" con rebote: no hay frames de correr, así que botamos al dragón.
                runPhase += dt * (6f + worldSpeed * 0.6f);
                float bounce = Mathf.Abs(Mathf.Sin(runPhase));
                visual.localPosition = new Vector3(0f, bounce * 0.18f, 0f);
                visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(runPhase) * 4f);
            }
            else
            {
                visual.localPosition = Vector3.zero;
                float tilt = Gliding ? -6f : Mathf.Clamp(vy * 1.2f, -20f, 15f);
                visual.localRotation = Quaternion.Slerp(visual.localRotation, Quaternion.Euler(0f, 0f, tilt), 1f - Mathf.Exp(-10f * dt));
            }
            visual.localScale = s;
        }

        public void Squash(float x, float y) => visual.localScale = new Vector3(baseScale * x, baseScale * y, 1f);

        public void Bounce()
        {
            vy = JumpVelocity * 0.8f;
            Grounded = false;
            airJumps = 1;
            Squash(0.8f, 1.25f);
        }

        public void Hurt()
        {
            Invulnerable = 1.6f;
            ShowFace(hurtSprite, 0.6f);
            Squash(1.3f, 0.75f);
        }

        public void Happy() => ShowFace(happySprite, 0.35f);

        public void Die()
        {
            body.sprite = sadSprite;
            body.color = Color.white;
            faceTimer = 999f;
            visual.localRotation = Quaternion.identity;
            visual.localPosition = Vector3.zero;
        }

        void ShowFace(Sprite s, float seconds)
        {
            body.sprite = s;
            faceTimer = seconds;
        }
    }
}
