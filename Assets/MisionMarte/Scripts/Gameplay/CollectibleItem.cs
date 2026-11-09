using System.Collections.Generic;
using UnityEngine;

namespace MisionMarte
{
    public enum CollectibleType { Residuo, Tuerca, Gota }

    /// <summary>
    /// Objeto coleccionable. Residuo = objetivo de la misión (5). Tuerca = pieza de circuito (puntaje y turbo).
    /// Gota = agua recuperada (sube la reserva de agua de la base).
    /// Se recoge al tocarlo, desaparece y solo cuenta UNA vez (prueba P3).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CollectibleItem : MonoBehaviour
    {
        [Header("Identificación")]
        [SerializeField] private CollectibleType type = CollectibleType.Residuo;
        [SerializeField] private string itemName = "Residuo";
        [SerializeField] private int itemValue = 1;
        [TextArea(2, 4)]
        [SerializeField] private string educationalFact = "";

        [Header("Animación")]
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float bobSpeed = 3f;
        [SerializeField] private bool spin = false;

        /// <summary>Coleccionables activos en la escena (lo usa el sistema de pistas, sin usar Find).</summary>
        public static readonly List<CollectibleItem> Active = new List<CollectibleItem>();
        public CollectibleType Type => type;

        private void OnEnable() { Active.Add(this); }
        private void OnDisable() { Active.Remove(this); }

        private bool isCollected;
        private Vector3 basePos;
        private float phase;

        private void Start()
        {
            basePos = transform.position;
            phase = Random.value * 6.28f;
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Update()
        {
            float t = Time.time * bobSpeed + phase;
            transform.position = basePos + Vector3.up * Mathf.Sin(t) * bobAmplitude;
            if (spin && transform.childCount > 0) transform.GetChild(0).localScale = new Vector3(Mathf.Cos(t * 0.8f), 1f, 1f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Validación: solo el Player y solo una vez (Prueba P3)
            if (isCollected || !other.CompareTag("Player")) return;
            isCollected = true;

            if (GameHUDManager.Instance != null)
            {
                var sr = GetComponentInChildren<SpriteRenderer>();
                GameHUDManager.Instance.AddCollectible(type, itemValue, itemName, educationalFact, sr != null ? sr.sprite : null);
                Color c = type == CollectibleType.Tuerca ? new Color(0.85f, 0.88f, 0.95f)
                        : type == CollectibleType.Gota ? new Color(0.45f, 0.8f, 1f) : new Color(0.4f, 1f, 0.6f);
                GameHUDManager.Instance.SpawnBurst(transform.position, c);
            }
            Destroy(gameObject);
        }
    }
}
