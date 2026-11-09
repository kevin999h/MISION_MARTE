using UnityEngine;
using UnityEngine.SceneManagement;

namespace MisionMarte
{
    /// <summary>
    /// Parámetros de dificultad de un nivel guardados en un ScriptableObject.
    /// Se editan desde el Inspector (carpeta Assets/MisionMarte/Resources/LevelConfigs)
    /// sin tocar código. Si no existe el asset, se usan estos valores por defecto.
    /// </summary>
    [CreateAssetMenu(menuName = "Misión Marte/LevelConfig", fileName = "Nivel_Config")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Identificación")]
        public string levelName = "Base limpia, agua segura";
        public int levelIndex = 1;

        [Header("Misión")]
        [Min(1)] public int residuosRequeridos = 5;

        [Header("Agua")]
        public float aguaInicial = 100f;
        [Tooltip("Porcentaje de agua que pierde la base por segundo por cada fuga abierta")]
        public float perdidaPorFugaPorSegundo = 0.3f;
        public float aguaPorGota = 5f;

        [Header("Jugador")]
        [Tooltip("Multiplica la velocidad de caminar/correr")]
        public float multiplicadorVelocidad = 1f;
        public int tuercasParaTurbo = 10;

        [Header("Ayudas")]
        [Tooltip("Pistas disponibles con la tecla H")]
        public int pistasDisponibles = 3;
        [Tooltip("Agua (%) que cuesta usar una pista. 0 = sin penalización")]
        public float costoPistaAgua = 0f;
        public bool mostrarConsejos = true;

        [Header("Calificación (agua final %)")]
        public float aguaTresEstrellas = 60f;
        public float aguaDosEstrellas = 30f;

        /// <summary>Número de nivel según el nombre de la escena activa ("Nivel1" -> 1).</summary>
        public static int CurrentLevelIndex()
        {
            string n = SceneManager.GetActiveScene().name;
            for (int i = 0; i < n.Length; i++)
                if (char.IsDigit(n[i])) return n[i] - '0';
            return 1;
        }

        /// <summary>Busca el LevelConfig del nivel; si no existe, crea uno con valores por defecto.</summary>
        public static LevelConfig ForLevel(int index)
        {
            var all = Resources.LoadAll<LevelConfig>("LevelConfigs");
            foreach (var c in all) if (c != null && c.levelIndex == index) return c;
            var d = CreateInstance<LevelConfig>();
            d.levelIndex = index;
            return d;
        }
    }
}
