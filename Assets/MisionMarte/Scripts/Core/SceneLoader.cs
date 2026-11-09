using UnityEngine;
using UnityEngine.SceneManagement;

namespace MisionMarte
{
    /// <summary>
    /// Carga escenas por nombre. Si la escena no está en la lista del Build Profile,
    /// dentro del editor la carga igual desde Assets/MisionMarte/Scenes (así nunca sale el error rojo).
    /// </summary>
    public static class SceneLoader
    {
        const string Folder = "Assets/MisionMarte/Scenes/";

        public static void Load(string sceneName)
        {
            GameState.ResetAll();
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
                return;
            }
#if UNITY_EDITOR
            string path = Folder + sceneName + ".unity";
            if (!System.IO.File.Exists(path))
            {
                Debug.LogError("[Misión Marte] No existe " + path + ". Sal de Play y usa el menú superior: Misión Marte > Construir solo el Nivel 1.");
                return;
            }
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Debug.LogError("[Misión Marte] La escena " + sceneName + " no está en el Build Profile.");
#endif
        }

        public static void ReloadCurrent() => Load(SceneManager.GetActiveScene().name);
    }
}
