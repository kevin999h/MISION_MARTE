using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MisionMarte
{
    /// <summary>Marca una interfaz que NO debe cambiar con las opciones de texto/contraste.</summary>
    public class AccessibilityIgnore : MonoBehaviour { }

    /// <summary>
    /// Aplica a toda la interfaz las opciones de accesibilidad: tamaño de texto y alto contraste
    /// (contorno más grueso en los textos y paneles más opacos). Se crea sola al iniciar el juego.
    /// </summary>
    public class AccessibilityApplier : MonoBehaviour
    {
        struct TextBase { public int size; public bool hasOutline; public Color outline; public Vector2 dist; }

        readonly Dictionary<Text, TextBase> texts = new Dictionary<Text, TextBase>();
        readonly Dictionary<Image, float> alphas = new Dictionary<Image, float>();
        float rescanTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<AccessibilityApplier>() != null) return;
            var go = new GameObject("[Accesibilidad]");
            DontDestroyOnLoad(go);
            go.AddComponent<AccessibilityApplier>();
        }

        void OnEnable()
        {
            AccessibilitySettings.OnChanged += Apply;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            AccessibilitySettings.OnChanged -= Apply;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene s, LoadSceneMode m)
        {
            texts.Clear();
            alphas.Clear();
            StartCoroutine(ApplyDelayed());
        }

        IEnumerator ApplyDelayed()
        {
            yield return null;
            yield return null;
            Apply();
        }

        void Update()
        {
            // Revisa de vez en cuando por si apareció algún texto nuevo
            rescanTimer += Time.unscaledDeltaTime;
            if (rescanTimer < 2f) return;
            rescanTimer = 0f;
            if (AccessibilitySettings.TextSize != 0 || AccessibilitySettings.HighContrast) Apply();
        }

        static bool Ignored(Component c) { return c.GetComponentInParent<AccessibilityIgnore>() != null; }

        void Apply()
        {
            float scale = AccessibilitySettings.TextScale;
            bool hc = AccessibilitySettings.HighContrast;

            foreach (var t in FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t == null || Ignored(t)) continue;
                var outline = t.GetComponent<Outline>();
                if (!texts.TryGetValue(t, out var b))
                {
                    b = new TextBase
                    {
                        size = t.fontSize,
                        hasOutline = outline != null,
                        outline = outline != null ? outline.effectColor : default(Color),
                        dist = outline != null ? outline.effectDistance : default(Vector2)
                    };
                    texts[t] = b;
                }
                int target = Mathf.RoundToInt(b.size * scale);
                if (t.fontSize != target) t.fontSize = target;
                if (outline != null && b.hasOutline)
                {
                    outline.effectColor = hc ? new Color(0f, 0f, 0f, 1f) : b.outline;
                    outline.effectDistance = hc ? b.dist * 1.5f : b.dist;
                }
            }

            foreach (var im in FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (im == null || Ignored(im)) continue;
                Color c = im.color;
                if (!alphas.TryGetValue(im, out float a0)) { a0 = c.a; alphas[im] = a0; }
                bool dark = (c.r * 0.3f + c.g * 0.59f + c.b * 0.11f) < 0.4f;
                if (hc && dark && a0 > 0.3f && a0 < 0.97f && Mathf.Abs(c.a - a0) < 0.001f) { c.a = 0.97f; im.color = c; }
                else if (!hc && Mathf.Abs(c.a - 0.97f) < 0.001f && a0 < 0.97f) { c.a = a0; im.color = c; }
            }
        }
    }
}
