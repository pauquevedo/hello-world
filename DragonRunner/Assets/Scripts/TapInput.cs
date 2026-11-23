using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DragonRunner
{
    /// <summary>
    /// Entrada unificada: espacio / flecha arriba / W / clic / toque.
    /// Funciona con el Input System nuevo (por defecto en Unity 6) y con el antiguo.
    /// </summary>
    public static class TapInput
    {
        /// <summary>Zona de la pantalla (arriba a la derecha) reservada al botón de pausa.</summary>
        static bool InPauseZone(Vector2 screen) =>
            screen.x > Screen.width * 0.85f && screen.y > Screen.height * 0.8f;

        public static bool JumpPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame))
                    return true;
                var ts = Touchscreen.current;
                if (ts != null)
                    foreach (var touch in ts.touches)
                        if (touch.press.wasPressedThisFrame && !InPauseZone(touch.position.ReadValue())) return true;
                var m = Mouse.current;
                if (m != null && m.leftButton.wasPressedThisFrame && !InPauseZone(m.position.ReadValue())) return true;
                return false;
#else
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) return true;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Began && !InPauseZone(t.position)) return true;
                }
                return Input.touchCount == 0 && Input.GetMouseButtonDown(0) && !InPauseZone(Input.mousePosition);
#endif
            }
        }

        public static bool JumpHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null && (kb.spaceKey.isPressed || kb.upArrowKey.isPressed || kb.wKey.isPressed)) return true;
                var ts = Touchscreen.current;
                if (ts != null && ts.primaryTouch.press.isPressed) return true;
                var m = Mouse.current;
                return m != null && m.leftButton.isPressed;
#else
                return Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)
                       || Input.touchCount > 0 || Input.GetMouseButton(0);
#endif
            }
        }

        public static bool PausePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                return kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame);
#else
                return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
#endif
            }
        }
    }
}
