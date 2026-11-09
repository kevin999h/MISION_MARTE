using UnityEngine;

namespace MisionMarte
{
    /// <summary>
    /// Objeto interactivo educativo (cartel, terminal...). Al acercarse muestra la tecla "E" flotante;
    /// con E abre un panel con información y puede activar mecanismos (ej. el puente de energía).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class InteractableObject : MonoBehaviour
    {
        [Header("Mensaje flotante")]
        [SerializeField] private GameObject promptFloating;

        [Header("Contenido educativo")]
        [SerializeField] private string title = "Dato";
        [TextArea(3, 8)]
        [SerializeField] private string educationalContent = "";

        [Header("Mecanismo (opcional)")]
        [SerializeField] private GameObject[] activateOnUse;
        [SerializeField] private SpriteRenderer stateRenderer;
        [SerializeField] private Sprite usedSprite;
        [SerializeField] private string activationMessage = "";

        private bool isPlayerNearby;
        private bool used;

        private void Start()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (promptFloating != null) promptFloating.SetActive(false);
            if (activateOnUse != null)
                foreach (var go in activateOnUse) if (go != null) go.SetActive(false);
        }

        private void Update()
        {
            if (!isPlayerNearby || GameState.Paused || GameState.ModalOpen || GameState.DialogueOpen) return;
            if (!GameInput.InteractDown()) return;

            var panel = InfoPanelUI.Instance;
            if (panel == null) return;

            if (panel.IsOpenFor(this))
            {
                panel.Close();
                if (promptFloating != null) promptFloating.SetActive(true);
                Activate();
            }
            else if (!GameState.Locked)
            {
                panel.Open(this, title, GameSession.Format(educationalContent));
                if (promptFloating != null) promptFloating.SetActive(false);
                Sfx.Play(SfxType.Blip);
            }
        }

        /// <summary>Activa el mecanismo la primera vez que se lee el objeto.</summary>
        private void Activate()
        {
            if (used) return;
            used = true;
            bool hasMechanism = false;
            if (activateOnUse != null)
                foreach (var go in activateOnUse)
                    if (go != null)
                    {
                        go.SetActive(true); hasMechanism = true;
                        if (GameHUDManager.Instance != null) GameHUDManager.Instance.SpawnBurst(go.transform.position, new Color(0.3f, 0.9f, 1f));
                    }
            if (stateRenderer != null && usedSprite != null) stateRenderer.sprite = usedSprite;
            if (hasMechanism) Sfx.Play(SfxType.Activate);
            if (!string.IsNullOrEmpty(activationMessage) && GameHUDManager.Instance != null)
                GameHUDManager.Instance.Toast(activationMessage);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            isPlayerNearby = true;
            if (promptFloating != null && (InfoPanelUI.Instance == null || !InfoPanelUI.Instance.IsOpenFor(this)))
                promptFloating.SetActive(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            isPlayerNearby = false;
            if (promptFloating != null) promptFloating.SetActive(false);
            if (InfoPanelUI.Instance != null && InfoPanelUI.Instance.IsOpenFor(this)) { InfoPanelUI.Instance.Close(); Activate(); }
        }
    }
}
