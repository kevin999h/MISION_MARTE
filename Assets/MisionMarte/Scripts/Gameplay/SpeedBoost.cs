using System.Collections;
using UnityEngine;

namespace MisionMarte
{
    /// <summary>Celda de energía limpia: da turbo de velocidad unos segundos y reaparece después.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class SpeedBoost : MonoBehaviour
    {
        [SerializeField] private float duration = 5f;
        [SerializeField] private float speedMultiplier = 1.6f;
        [SerializeField] private float respawnTime = 8f;

        private SpriteRenderer sr;
        private Collider2D col;
        private Vector3 basePos;

        private void Start()
        {
            sr = GetComponentInChildren<SpriteRenderer>();
            col = GetComponent<Collider2D>();
            col.isTrigger = true;
            basePos = transform.position;
        }

        private void Update()
        {
            transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 4f) * 0.1f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || PlayerController.Instance == null) return;
            PlayerController.Instance.ApplyBoost(duration, speedMultiplier);
            if (GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.Toast("¡TURBO! Celda de energía solar: +60% velocidad", 3f, ToastKind.Success);
                GameHUDManager.Instance.SpawnBurst(transform.position, new Color(0.3f, 0.9f, 1f));
            }
            StartCoroutine(Respawn());
        }

        private IEnumerator Respawn()
        {
            col.enabled = false; if (sr) sr.enabled = false;
            yield return new WaitForSeconds(respawnTime);
            col.enabled = true; if (sr) sr.enabled = true;
        }
    }
}
