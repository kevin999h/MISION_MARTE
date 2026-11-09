using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MisionMarte
{
    /// <summary>
    /// Capa única de entrada. Funciona tanto con el Input System nuevo (por defecto en Unity 6)
    /// como con el Input Manager clásico, así el proyecto compila sin importar la configuración.
    /// Controles: A/D o flechas = mover | Espacio/W = saltar | S/Ctrl = deslizar/agacharse
    ///            Shift = correr | E = interactuar | Esc/P = pausa
    /// </summary>
    public static class GameInput
    {
#if ENABLE_INPUT_SYSTEM
        static bool Down(params Key[] keys)
        {
            var k = Keyboard.current; if (k == null) return false;
            foreach (var key in keys) if (k[key].wasPressedThisFrame) return true;
            return false;
        }
        static bool Held(params Key[] keys)
        {
            var k = Keyboard.current; if (k == null) return false;
            foreach (var key in keys) if (k[key].isPressed) return true;
            return false;
        }
        static Gamepad Pad => Gamepad.current;

        public static float Horizontal()
        {
            float h = 0f;
            if (Held(Key.A, Key.LeftArrow)) h -= 1f;
            if (Held(Key.D, Key.RightArrow)) h += 1f;
            if (Pad != null)
            {
                float s = Pad.leftStick.x.ReadValue();
                if (Mathf.Abs(s) > 0.25f) h = s;
                if (Pad.dpad.left.isPressed) h = -1f;
                if (Pad.dpad.right.isPressed) h = 1f;
            }
            return Mathf.Clamp(h, -1f, 1f);
        }
        public static bool JumpDown() => Down(Key.Space, Key.W, Key.UpArrow) || (Pad != null && Pad.buttonSouth.wasPressedThisFrame);
        public static bool JumpHeld() => Held(Key.Space, Key.W, Key.UpArrow) || (Pad != null && Pad.buttonSouth.isPressed);
        public static bool SlideDown() => Down(Key.S, Key.DownArrow, Key.LeftCtrl, Key.RightCtrl) || (Pad != null && Pad.buttonEast.wasPressedThisFrame);
        public static bool SlideHeld() => Held(Key.S, Key.DownArrow, Key.LeftCtrl, Key.RightCtrl) || (Pad != null && Pad.buttonEast.isPressed);
        public static bool RunHeld() => Held(Key.LeftShift, Key.RightShift) || (Pad != null && Pad.rightTrigger.isPressed);
        public static bool InteractDown() => Down(Key.E) || (Pad != null && Pad.buttonWest.wasPressedThisFrame);
        public static bool AdvanceDown() => Down(Key.E, Key.Space, Key.Enter, Key.NumpadEnter) || (Pad != null && Pad.buttonSouth.wasPressedThisFrame);
        public static bool ConfirmDown() => Down(Key.Enter, Key.NumpadEnter, Key.E) || (Pad != null && Pad.buttonSouth.wasPressedThisFrame);
        public static bool PauseDown() => Down(Key.Escape, Key.P) || (Pad != null && Pad.startButton.wasPressedThisFrame);
        public static bool LeftDown() => Down(Key.A, Key.LeftArrow);
        public static bool RightDown() => Down(Key.D, Key.RightArrow);
        public static bool HintDown() => Down(Key.H);
        public static bool SettingsDown() => Down(Key.F1);
#else
        static bool Down(params KeyCode[] keys) { foreach (var k in keys) if (Input.GetKeyDown(k)) return true; return false; }
        static bool Held(params KeyCode[] keys) { foreach (var k in keys) if (Input.GetKey(k)) return true; return false; }

        public static float Horizontal()
        {
            float h = 0f;
            if (Held(KeyCode.A, KeyCode.LeftArrow)) h -= 1f;
            if (Held(KeyCode.D, KeyCode.RightArrow)) h += 1f;
            return h;
        }
        public static bool JumpDown() => Down(KeyCode.Space, KeyCode.W, KeyCode.UpArrow);
        public static bool JumpHeld() => Held(KeyCode.Space, KeyCode.W, KeyCode.UpArrow);
        public static bool SlideDown() => Down(KeyCode.S, KeyCode.DownArrow, KeyCode.LeftControl, KeyCode.RightControl);
        public static bool SlideHeld() => Held(KeyCode.S, KeyCode.DownArrow, KeyCode.LeftControl, KeyCode.RightControl);
        public static bool RunHeld() => Held(KeyCode.LeftShift, KeyCode.RightShift);
        public static bool InteractDown() => Down(KeyCode.E);
        public static bool AdvanceDown() => Down(KeyCode.E, KeyCode.Space, KeyCode.Return, KeyCode.KeypadEnter);
        public static bool ConfirmDown() => Down(KeyCode.Return, KeyCode.KeypadEnter, KeyCode.E);
        public static bool PauseDown() => Down(KeyCode.Escape, KeyCode.P);
        public static bool LeftDown() => Down(KeyCode.A, KeyCode.LeftArrow);
        public static bool RightDown() => Down(KeyCode.D, KeyCode.RightArrow);
        public static bool HintDown() => Down(KeyCode.H);
        public static bool SettingsDown() => Down(KeyCode.F1);
#endif
    }
}
