using UnityEngine;

namespace MisionMarte
{
    /// <summary>
    /// Zona peligrosa (vacío bajo el nivel o charco tóxico). El jugador vuelve al último punto de control.
    /// Este es el "parche" para que el personaje nunca se pierda cayendo de la plataforma.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hazard : MonoBehaviour
    {
        [SerializeField] private string message = "";

        private void Start() { GetComponent<Collider2D>().isTrigger = true; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || PlayerController.Instance == null) return;
            PlayerController.Instance.Respawn();
            CameraFollow2D.Shake(0.3f, 0.18f);
            if (!string.IsNullOrEmpty(message) && GameHUDManager.Instance != null) GameHUDManager.Instance.Toast(message, 3f, ToastKind.Error);
        }
    }
}
