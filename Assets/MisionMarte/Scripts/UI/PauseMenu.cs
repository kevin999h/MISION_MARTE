using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MisionMarte
{
    /// <summary>Menú de pausa (Esc o P): Reanudar, Reiniciar nivel, Menú principal y Salir.</summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private string menuSceneName = "Menu";

        private void Start()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            if (menuButton != null) menuButton.onClick.AddListener(GoMenu);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
        }

        private void Update()
        {
            if (SettingsPanel.IsOpen) return; // Esc cierra los ajustes, no pausa
            if (!GameInput.PauseDown()) return;
            if (GameState.Paused) Resume();
            else if (!GameState.ModalOpen) Pause();
        }

        public void Pause()
        {
            GameState.Paused = true;
            Time.timeScale = 0f;
            pausePanel.SetActive(true);
            if (resumeButton != null) resumeButton.Select();
        }

        public void Resume()
        {
            GameState.Paused = false;
            Time.timeScale = 1f;
            pausePanel.SetActive(false);
            GameState.MarkClosed();
        }

        public void Restart()
        {
            SceneLoader.ReloadCurrent();
        }

        public void GoMenu()
        {
            SceneLoader.Load(menuSceneName);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
