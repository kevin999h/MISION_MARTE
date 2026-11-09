using UnityEngine;

namespace MisionMarte
{
    public enum Heroine { Ligi = 0, Mariale = 1 }

    /// <summary>Guarda qué personaje eligió el jugador (persiste entre escenas y partidas).</summary>
    public static class GameSession
    {
        const string KeyHero = "MM_Personaje";

        public static Heroine Selected
        {
            get => (Heroine)PlayerPrefs.GetInt(KeyHero, 0);
            set { PlayerPrefs.SetInt(KeyHero, (int)value); PlayerPrefs.Save(); }
        }

        public static string HeroName => Selected == Heroine.Ligi ? "Ligi" : "Mariale";
        public static string CompanionName => Selected == Heroine.Ligi ? "Mariale" : "Ligi";
        public static string HeroRole => Selected == Heroine.Ligi ? "Exploradora y líder" : "Ingeniera y científica";

        // Bonos de cada personaje (para que elegir tenga sentido en la jugabilidad)
        public static float JumpMultiplier => Selected == Heroine.Ligi ? 1.08f : 1f;   // Ligi salta un poco más
        /// <summary>Multiplicador de velocidad del nivel (viene del LevelConfig).</summary>
        public static float LevelSpeed = 1f;
        public static float SpeedMultiplier => (Selected == Heroine.Mariale ? 1.08f : 1f) * LevelSpeed; // Mariale corre un poco más

        /// <summary>Reemplaza {HEROE} y {COMPANERA} en los textos de diálogo.</summary>
        public static string Format(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("{HEROE}", HeroName).Replace("{COMPANERA}", CompanionName);
        }
    }

    /// <summary>Estado global: bloquea el movimiento cuando hay diálogos, paneles o pausa.</summary>
    public static class GameState
    {
        public static bool DialogueOpen;
        public static bool InfoOpen;
        public static bool Paused;
        public static bool ModalOpen;
        public static int LastUIClosedFrame = -10;

        /// <summary>True si el jugador NO debe moverse ni abrir otra interfaz (evita solapamientos - prueba P5).</summary>
        public static bool Locked => DialogueOpen || InfoOpen || Paused || ModalOpen || Time.frameCount == LastUIClosedFrame;

        public static void MarkClosed() => LastUIClosedFrame = Time.frameCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetAll()
        {
            DialogueOpen = InfoOpen = Paused = ModalOpen = false;
            LastUIClosedFrame = -10;
            Time.timeScale = 1f;
        }
    }
}
