using UnityEngine;

namespace MisionMarte
{
    /// <summary>Muestra un consejo en pantalla la primera vez que el jugador pasa por aquí.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class HintTrigger : MonoBehaviour
    {
        [TextArea(1, 3)] [SerializeField] private string hint = "";
        [SerializeField] private bool onlyOnce = true;
        [SerializeField] private float duration = 4f;
        private bool shown;

        private void Start() { GetComponent<Collider2D>().isTrigger = true; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || (onlyOnce && shown)) return;
            if (GameHUDManager.Instance != null && !GameHUDManager.Instance.ShowTips) return;
            shown = true;
            if (GameHUDManager.Instance != null) GameHUDManager.Instance.Toast(GameSession.Format(hint), duration);
        }
    }
}
