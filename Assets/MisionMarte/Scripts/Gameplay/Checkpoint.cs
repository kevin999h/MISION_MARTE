using UnityEngine;

namespace MisionMarte
{
    /// <summary>Baliza de punto de control: si el jugador cae, reaparece aquí.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer beaconRenderer;
        [SerializeField] private Sprite offSprite;
        [SerializeField] private Sprite onSprite;
        private bool active;

        private void Start() { GetComponent<Collider2D>().isTrigger = true; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (active || !other.CompareTag("Player") || PlayerController.Instance == null) return;
            active = true;
            PlayerController.Instance.SetCheckpoint(transform.position + Vector3.up * 0.2f);
            if (beaconRenderer && onSprite) beaconRenderer.sprite = onSprite;
            Sfx.Play(SfxType.Checkpoint);
            if (GameHUDManager.Instance != null) GameHUDManager.Instance.Toast("Punto de control activado", 3f, ToastKind.Success);
        }
    }
}
