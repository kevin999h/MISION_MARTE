using UnityEditor;
using UnityEngine;
using System.IO;

namespace MisionMarte.EditorTools
{
    /// <summary>
    /// Crea automáticamente los 3 LevelConfig (Nivel 1 tutorial, 2 intermedio, 3 final)
    /// en Assets/MisionMarte/Resources/LevelConfigs si todavía no existen.
    /// </summary>
    [InitializeOnLoad]
    public static class LevelConfigCreator
    {
        const string Dir = "Assets/MisionMarte/Resources/LevelConfigs";

        static LevelConfigCreator() { EditorApplication.delayCall += Ensure; }

        [MenuItem("Misión Marte/Crear LevelConfig (niveles 1-3)")]
        public static void Ensure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Ensure; return; }

            if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
            Make(1, "Nivel1_Config", "Base limpia, agua segura", 0.15f, 3, 0f, 1f, 60f, 30f);
            Make(2, "Nivel2_Config", "Restaurar ecosistemas", 0.25f, 2, 2f, 1f, 65f, 35f);
            Make(3, "Nivel3_Config", "Energía y regreso a la Tierra", 0.40f, 1, 3f, 1.05f, 70f, 40f);
            AssetDatabase.SaveAssets();
        }

        static void Make(int idx, string file, string title, float loss, int hints, float hintCost, float speed, float star3, float star2)
        {
            string path = Dir + "/" + file + ".asset";
            if (File.Exists(path)) return;
            var c = ScriptableObject.CreateInstance<LevelConfig>();
            c.levelIndex = idx; c.levelName = title;
            c.perdidaPorFugaPorSegundo = loss; c.pistasDisponibles = hints; c.costoPistaAgua = hintCost;
            c.multiplicadorVelocidad = speed; c.aguaTresEstrellas = star3; c.aguaDosEstrellas = star2;
            AssetDatabase.CreateAsset(c, path);
        }
    }
}
