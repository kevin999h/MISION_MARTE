using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MisionMarte
{
    /// <summary>
    /// PARCHE DE PANTALLA NEGRA (proyectos URP / Universal 2D).
    /// En URP los sprites usan un material "iluminado" y, si la luz 2D de la escena no los alcanza, se ven NEGROS.
    /// Este script se activa solo al cargar cualquier escena (no hay que ponerlo en ningún objeto) y cambia todos
    /// los sprites a un material "sin iluminación", así siempre se ven con su color real.
    /// En proyectos sin URP no hace nada.
    /// </summary>
    public static class SpriteRenderFix
    {
        static Material unlit;
        static bool tried;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            FixAll(SceneManager.GetActiveScene().name);
        }

        static void OnLoaded(Scene s, LoadSceneMode m) => FixAll(s.name);

        public static Material Unlit
        {
            get
            {
                if (!tried)
                {
                    tried = true;
                    if (GraphicsSettings.currentRenderPipeline != null)
                    {
                        var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                        if (sh != null) unlit = new Material(sh) { name = "MM_SpriteUnlit" };
                        else Debug.LogWarning("[Misión Marte] No se encontró el shader Sprite-Unlit-Default de URP.");
                    }
                }
                return unlit;
            }
        }

        public static void Apply(SpriteRenderer sr)
        {
            if (sr != null && Unlit != null) sr.sharedMaterial = Unlit;
        }

        static void FixAll(string sceneName)
        {
            var srs = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int empty = 0;
            foreach (var sr in srs)
            {
                Apply(sr);
                if (sr.sprite == null) empty++;
            }
            string pipe = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "Built-in";
            Debug.Log("[Misión Marte] Escena " + sceneName + ": " + srs.Length + " sprites, render = " + pipe +
                      (Unlit != null ? " (parche sin iluminación aplicado)" : ""));
            if (empty > 3)
                Debug.LogWarning("[Misión Marte] Hay " + empty + " objetos sin imagen (Sprite = None). Sal de Play y usa: Misión Marte > Reconstruir escenas.");
        }
    }
}
