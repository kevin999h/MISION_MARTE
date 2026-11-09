using System;
using UnityEngine;
using UnityEngine.UI;

namespace MisionMarte
{
    /// <summary>
    /// Panel de diálogo compartido por todos los NPC (parte inferior de la pantalla).
    /// Efecto máquina de escribir; E / Espacio / Enter avanza. Solo puede haber UN diálogo abierto (prueba P5).
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Text nameText;
        [SerializeField] private Text contentText;
        [SerializeField] private Text hintText;
        [SerializeField] private float charsPerSecond = 50f;

        private DialogueLine[] lines;
        private string defaultName;
        private Action onFinished;
        private int index;
        private int openedFrame;
        private string fullText = "";
        private float shown;
        private bool typing;

        public bool IsOpen => GameState.DialogueOpen;

        private void Awake()
        {
            Instance = this;
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Open(DialogueLine[] newLines, string fallbackName, Action finished)
        {
            if (GameState.DialogueOpen || newLines == null || newLines.Length == 0) return; // nunca se duplica
            lines = newLines; defaultName = fallbackName; onFinished = finished;
            index = 0; openedFrame = Time.frameCount;
            GameState.DialogueOpen = true;
            dialoguePanel.SetActive(true);
            ShowLine();
        }

        private void ShowLine()
        {
            var l = lines[index];
            string speaker = string.IsNullOrEmpty(l.speakerName) ? defaultName : l.speakerName;
            nameText.text = GameSession.Format(speaker);
            nameText.color = l.nameColor.a > 0.01f ? l.nameColor : Color.white;
            fullText = GameSession.Format(l.text);
            if (GameHUDManager.Instance != null) fullText = fullText.Replace("{FALTAN}", GameHUDManager.Instance.RemainingResiduos.ToString())
                                                                 .Replace("{FUGAS}", GameHUDManager.Instance.RemainingLeaks.ToString());
            shown = 0f; typing = true;
            contentText.text = "";
            if (hintText != null) hintText.text = (index < lines.Length - 1) ? "E / Espacio  >>  continuar" : "E / Espacio  >>  cerrar";
        }

        private void Update()
        {
            if (!GameState.DialogueOpen || GameState.Paused) return;

            if (typing)
            {
                shown += Time.unscaledDeltaTime * charsPerSecond;
                int n = Mathf.Min(fullText.Length, Mathf.FloorToInt(shown));
                contentText.text = fullText.Substring(0, n);
                if (n >= fullText.Length) typing = false;
            }

            if (Time.frameCount == openedFrame || !GameInput.AdvanceDown()) return;

            if (typing) { typing = false; contentText.text = fullText; return; } // completa la línea
            index++;
            if (index < lines.Length) { ShowLine(); Sfx.Play(SfxType.Blip); }
            else Close();
        }

        private void Close()
        {
            dialoguePanel.SetActive(false);
            GameState.DialogueOpen = false;
            GameState.MarkClosed();
            var cb = onFinished; onFinished = null;
            cb?.Invoke();
        }
    }
}
