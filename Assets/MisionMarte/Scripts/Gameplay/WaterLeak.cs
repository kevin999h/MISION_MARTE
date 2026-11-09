using UnityEngine;

namespace MisionMarte
{
    /// <summary>
    /// Fuga de agua (tubería rota). Mientras esté abierta, la reserva de agua de la base baja.
    /// El jugador se acerca, aparece la tecla "E" y al presionarla cierra la válvula (ahorro de agua).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class WaterLeak : MonoBehaviour
    {
        [SerializeField] private GameObject promptFloating;
        [SerializeField] private SpriteRenderer valveRenderer;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private WaterSpray spray;
        [TextArea(2, 4)]
        [SerializeField] private string fact = "Una llave que gotea puede desperdiciar decenas de litros de agua al día.";

        public bool IsOpen { get; private set; } = true;
        private bool playerNear;

        private void Start()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (promptFloating != null) promptFloating.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen || !playerNear || GameState.Locked) return;
            if (GameInput.InteractDown()) Close();
        }

        private void Close()
        {
            IsOpen = false;
            if (valveRenderer != null && closedSprite != null) valveRenderer.sprite = closedSprite;
            if (spray != null) spray.enabled = false;
            if (promptFloating != null) promptFloating.SetActive(false);
            Sfx.Play(SfxType.Activate);
            if (GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.LeakClosed(fact);
                GameHUDManager.Instance.SpawnBurst(transform.position + Vector3.up * 0.5f, new Color(0.45f, 0.8f, 1f));
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            playerNear = true;
            if (IsOpen && promptFloating != null) promptFloating.SetActive(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            playerNear = false;
            if (promptFloating != null) promptFloating.SetActive(false);
        }
    }
}
