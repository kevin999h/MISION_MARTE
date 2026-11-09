using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MisionMarte
{
    /// <summary>
    /// Menú principal + selección de personaje (Ligi o Mariale).
    /// Se usa con el mouse o con el teclado (flechas/WASD para moverse, Enter para aceptar, Esc para volver).
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("Paneles")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject selectPanel;
        [SerializeField] private GameObject controlsPanel;
        [SerializeField] private GameObject creditsPanel;

        [Header("Menú principal")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        [Header("Selección de personaje")]
        [SerializeField] private Button ligiCard;
        [SerializeField] private Button mariCard;
        [SerializeField] private Image ligiFrame;
        [SerializeField] private Image mariFrame;
        [SerializeField] private Text selectInfoText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button selectBackButton;

        [Header("Otros")]
        [SerializeField] private Button controlsBackButton;
        [SerializeField] private Button creditsBackButton;
        [SerializeField] private string levelSceneName = "Nivel1";

        private Heroine chosen;

        private void Start()
        {
            GameState.ResetAll();
            chosen = GameSession.Selected;

            playButton.onClick.AddListener(() => Show(selectPanel, ligiCard));
            controlsButton.onClick.AddListener(() => Show(controlsPanel, controlsBackButton));
            creditsButton.onClick.AddListener(() => Show(creditsPanel, creditsBackButton));
            quitButton.onClick.AddListener(Quit);

            ligiCard.onClick.AddListener(() => Choose(Heroine.Ligi));
            mariCard.onClick.AddListener(() => Choose(Heroine.Mariale));
            startButton.onClick.AddListener(StartGame);
            selectBackButton.onClick.AddListener(() => Show(mainPanel, playButton));
            controlsBackButton.onClick.AddListener(() => Show(mainPanel, controlsButton));
            creditsBackButton.onClick.AddListener(() => Show(mainPanel, creditsButton));

            Show(mainPanel, playButton);
            Choose(chosen, false);
        }

        private void Update()
        {
            if (SettingsPanel.IsOpen) return; // los ajustes tienen su propio manejo de Esc

            // Esc vuelve al menú principal desde cualquier sub-panel
            if (GameInput.PauseDown() && !mainPanel.activeSelf) { Show(mainPanel, playButton); return; }

            if (selectPanel.activeSelf)
            {
                // Al mover la selección con el teclado sobre una tarjeta, se elige ese personaje
                var sel = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
                if (sel == ligiCard.gameObject && chosen != Heroine.Ligi) Choose(Heroine.Ligi);
                else if (sel == mariCard.gameObject && chosen != Heroine.Mariale) Choose(Heroine.Mariale);
            }

            // Si se perdió el foco (clic en el vacío), lo recupera para poder usar el teclado
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            {
                if (mainPanel.activeSelf) playButton.Select();
                else if (selectPanel.activeSelf) (chosen == Heroine.Ligi ? ligiCard : mariCard).Select();
            }
        }

        private void Show(GameObject panel, Button focus)
        {
            mainPanel.SetActive(panel == mainPanel);
            selectPanel.SetActive(panel == selectPanel);
            controlsPanel.SetActive(panel == controlsPanel);
            creditsPanel.SetActive(panel == creditsPanel);
            if (panel == selectPanel) focus = chosen == Heroine.Ligi ? ligiCard : mariCard;
            if (focus != null) focus.Select();
            Sfx.Play(SfxType.Blip);
        }

        private void Choose(Heroine h, bool sound = true)
        {
            chosen = h;
            GameSession.Selected = h;
            var on = new Color(1f, 0.78f, 0.25f, 1f);
            var off = new Color(1f, 1f, 1f, 0.15f);
            if (ligiFrame != null) ligiFrame.color = h == Heroine.Ligi ? on : off;
            if (mariFrame != null) mariFrame.color = h == Heroine.Mariale ? on : off;
            if (selectInfoText != null)
                selectInfoText.text = h == Heroine.Ligi
                    ? "<b>LIGI</b> · Exploradora y líder\nValiente, decidida y curiosa.\n<color=#7CF0FF>Habilidad: salta un 8% más alto.</color>\n<size=22>Mariale te acompañará como guía científica.</size>"
                    : "<b>MARIALE</b> · Ingeniera y científica\nInteligente, tranquila y analítica.\n<color=#7CF0FF>Habilidad: corre un 8% más rápido.</color>\n<size=22>Ligi te acompañará como líder de la misión.</size>";
            if (sound) Sfx.Play(SfxType.Blip);
        }

        private void StartGame()
        {
            GameSession.Selected = chosen;
            Sfx.Play(SfxType.Success);
            SceneLoader.Load(levelSceneName);
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
