using System;
using UnityEngine;

namespace MisionMarte
{
    /// <summary>
    /// Opciones de volumen y accesibilidad. Se guardan con PlayerPrefs, así se conservan
    /// al cerrar y volver a abrir el juego (Windows y WebGL).
    /// </summary>
    public static class AccessibilitySettings
    {
        const string KMusic = "MM_VolMusica", KSfx = "MM_VolSfx", KMute = "MM_Silencio",
                     KText = "MM_TamTexto", KContrast = "MM_AltoContraste", KIcons = "MM_Iconos", KShake = "MM_SinVibracion";

        public static event Action OnChanged;

        public static float MusicVolume { get => PlayerPrefs.GetFloat(KMusic, 0.7f); set { PlayerPrefs.SetFloat(KMusic, Mathf.Clamp01(value)); Raise(); } }
        public static float SfxVolume   { get => PlayerPrefs.GetFloat(KSfx, 0.85f);  set { PlayerPrefs.SetFloat(KSfx, Mathf.Clamp01(value)); Raise(); } }
        public static bool Muted        { get => PlayerPrefs.GetInt(KMute, 0) == 1;  set { PlayerPrefs.SetInt(KMute, value ? 1 : 0); Raise(); } }
        /// <summary>0 = Normal, 1 = Grande, 2 = Muy grande</summary>
        public static int TextSize      { get => Mathf.Clamp(PlayerPrefs.GetInt(KText, 0), 0, 2); set { PlayerPrefs.SetInt(KText, Mathf.Clamp(value, 0, 2)); Raise(); } }
        public static bool HighContrast { get => PlayerPrefs.GetInt(KContrast, 0) == 1; set { PlayerPrefs.SetInt(KContrast, value ? 1 : 0); Raise(); } }
        /// <summary>Muestra etiquetas de texto ([OK], [ERROR]...) además del color (para daltonismo).</summary>
        public static bool IconMode     { get => PlayerPrefs.GetInt(KIcons, 0) == 1; set { PlayerPrefs.SetInt(KIcons, value ? 1 : 0); Raise(); } }
        public static bool ReduceShake  { get => PlayerPrefs.GetInt(KShake, 0) == 1; set { PlayerPrefs.SetInt(KShake, value ? 1 : 0); Raise(); } }

        public static float TextScale => TextSize == 2 ? 1.3f : TextSize == 1 ? 1.15f : 1f;
        public static float MusicEffective => Muted ? 0f : MusicVolume;
        public static float SfxEffective => Muted ? 0f : SfxVolume;

        public static void Save() { PlayerPrefs.Save(); }
        static void Raise() { OnChanged?.Invoke(); }
    }
}
