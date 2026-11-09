using UnityEngine;
using UnityEngine.SceneManagement;

namespace MisionMarte
{
    /// <summary>
    /// Música de fondo persistente (no se corta al cambiar de escena). Genera por código dos pistas
    /// en bucle (una calmada para el menú y otra con más pulso para el nivel), así no hace falta
    /// importar archivos de audio. El volumen sale de AccessibilitySettings (slider de música / silenciar).
    /// Se crea sola al iniciar el juego, no hay que agregarla a ninguna escena.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        const int Rate = 16000;
        const float Loop = 16f;        // duración del bucle en segundos (4 acordes de 4 s)
        const float BaseVolume = 0.45f;

        AudioSource music;
        AudioClip menuClip, levelClip, wanted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("[AudioManager]");
            DontDestroyOnLoad(go);
            go.AddComponent<AudioManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.ignoreListenerPause = true;
            music.volume = 0f;

            menuClip = BuildTrack("musica_menu", false);
            levelClip = BuildTrack("musica_nivel", true);
            SceneManager.sceneLoaded += OnSceneLoaded;
            Choose(SceneManager.GetActiveScene().name);
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this) Instance = null;
        }

        void OnSceneLoaded(Scene s, LoadSceneMode m) { Choose(s.name); }

        void Choose(string sceneName)
        {
            wanted = sceneName != null && sceneName.Contains("Nivel") ? levelClip : menuClip;
        }

        void Update()
        {
            float target = AccessibilitySettings.MusicEffective * BaseVolume;
            if (wanted != music.clip)
            {
                // fundido: baja el volumen y, cuando llega a 0, cambia de pista
                target = 0f;
                if (music.volume <= 0.01f) { music.clip = wanted; music.Play(); }
            }
            else if (wanted != null && !music.isPlaying) music.Play();
            music.volume = Mathf.MoveTowards(music.volume, target, Time.unscaledDeltaTime * 1.2f);
        }

        // ---------------- síntesis de las pistas ----------------
        // Progresión Am - F - C - G. Las frecuencias se ajustan para que quepan ciclos completos
        // en el bucle y no se escuche un corte al repetirse.
        static readonly float[][] Chords =
        {
            new[] { 110.00f, 130.81f, 164.81f, 220.00f },
            new[] {  87.31f, 130.81f, 174.61f, 220.00f },
            new[] { 130.81f, 164.81f, 196.00f, 261.63f },
            new[] {  98.00f, 146.83f, 196.00f, 246.94f },
        };
        static readonly int[] ArpPattern = { 0, 2, 1, 3, 2, 1, 3, 2 };

        static float Snap(float f) { return Mathf.Round(f * Loop) / Loop; }

        static AudioClip BuildTrack(string name, bool driving)
        {
            int n = Mathf.RoundToInt(Rate * Loop);
            var data = new float[n];
            float twoPi = Mathf.PI * 2f;
            float step = driving ? 0.25f : 0.5f;
            float arpAmp = driving ? 0.14f : 0.10f;

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float s = 0f;

                // pad suave (acordes que se funden entre sí)
                for (int k = 0; k < 4; k++)
                {
                    float d = Mathf.Abs(t - (k * 4f + 2f));
                    d = Mathf.Min(d, Loop - d);
                    float env = Mathf.Clamp01(1f - d / 2.6f);
                    if (env <= 0f) continue;
                    var ch = Chords[k];
                    for (int j = 0; j < ch.Length; j++)
                        s += Mathf.Sin(twoPi * Snap(ch[j]) * t) * env * 0.07f;
                }

                // arpegio de triángulo
                int idx = Mathf.FloorToInt(t / step);
                int chord = Mathf.Min(3, Mathf.FloorToInt(t / 4f));
                float u = (t - idx * step) / step;
                float f = Snap(Chords[chord][ArpPattern[idx % ArpPattern.Length]] * 2f);
                float tri = Mathf.Asin(Mathf.Sin(twoPi * f * t)) * (2f / Mathf.PI);
                float aenv = Mathf.Min(1f, u / 0.03f) * Mathf.Exp(-u * 3.5f);
                s += tri * aenv * arpAmp;

                // bajo con pulso (solo la pista del nivel)
                if (driving)
                {
                    float bu = (t % 0.5f) / 0.5f;
                    float benv = Mathf.Min(1f, bu / 0.02f) * Mathf.Exp(-bu * 4f);
                    s += Mathf.Sin(twoPi * Snap(Chords[chord][0]) * t) * benv * 0.16f;
                }

                data[i] = Mathf.Clamp(s, -0.9f, 0.9f);
            }

            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
