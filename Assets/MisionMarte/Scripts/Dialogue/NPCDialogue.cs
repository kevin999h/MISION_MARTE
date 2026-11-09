using UnityEngine;

namespace MisionMarte
{
    /// <summary>
    /// NPC con conversación (tecla E). Cambia lo que dice según el avance de la misión:
    /// - antes de completar el objetivo usa "introLines" (la 2ª vez "reminderLines")
    /// - con los 5 residuos recogidos usa "completeLines" y, si se indica, finaliza el nivel.
    /// Si "isCompanion" está activo, el NPC es la compañera que NO eligió el jugador (Ligi o Mariale).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class NPCDialogue : MonoBehaviour
    {
        [Header("Configuración del NPC")]
        [SerializeField] private string npcName = "Comandante Orión";
        [SerializeField] private bool isCompanion = false;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite ligiSprite;
        [SerializeField] private Sprite mariSprite;
        [SerializeField] private float visualHeight = 1.5f;

        [Header("Conversaciones")]
        [SerializeField] private DialogueLine[] introLines;
        [SerializeField] private DialogueLine[] reminderLines;
        [SerializeField] private DialogueLine[] completeLines;
        [SerializeField] private bool completesLevel = false;

        [Header("Interfaz")]
        [SerializeField] private GameObject interactionPrompt;

        private bool isPlayerInRange;
        private bool talkedOnce;
        private bool levelDone;

        private void Start()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
            if (isCompanion)
            {
                npcName = GameSession.CompanionName;
                var s = GameSession.Selected == Heroine.Ligi ? mariSprite : ligiSprite;
                if (spriteRenderer != null && s != null)
                {
                    spriteRenderer.sprite = s;
                    float k = visualHeight / Mathf.Max(0.01f, s.bounds.size.y);
                    spriteRenderer.transform.localScale = new Vector3(k, k, 1f);
                }
            }
        }

        private void Update()
        {
            // Mira hacia el jugador
            if (spriteRenderer != null && PlayerController.Instance != null)
                spriteRenderer.flipX = PlayerController.Instance.transform.position.x < transform.position.x;

            if (!isPlayerInRange || GameState.Locked || DialogueUI.Instance == null) return;
            if (GameInput.InteractDown()) StartDialogue();
        }

        private void StartDialogue()
        {
            var hud = GameHUDManager.Instance;
            bool complete = hud != null && hud.ObjectiveComplete;
            DialogueLine[] lines;
            if (complete && completeLines != null && completeLines.Length > 0) lines = completeLines;
            else if (talkedOnce && reminderLines != null && reminderLines.Length > 0) lines = reminderLines;
            else lines = introLines;
            if (lines == null || lines.Length == 0) return;

            if (interactionPrompt != null) interactionPrompt.SetActive(false);
            Sfx.Play(SfxType.Blip);
            DialogueUI.Instance.Open(lines, npcName, () =>
            {
                talkedOnce = true;
                if (isPlayerInRange && interactionPrompt != null) interactionPrompt.SetActive(true);
                if (complete && completesLevel && !levelDone && hud != null)
                {
                    levelDone = true;
                    hud.ShowLevelComplete();
                }
                else if (hud != null && !complete) hud.OnTalkedToNPC();
            });
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player")) return;
            isPlayerInRange = true;
            if (!GameState.DialogueOpen && interactionPrompt != null) interactionPrompt.SetActive(true);
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player")) return;
            isPlayerInRange = false;
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
        }
    }
}
