using UnityEngine;
using UnityEngine.UI;

namespace MisionMarte
{
    /// <summary>Panel central que muestra la información educativa de los objetos interactivos.</summary>
    public class InfoPanelUI : MonoBehaviour
    {
        public static InfoPanelUI Instance { get; private set; }

        [SerializeField] private GameObject panel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;

        private InteractableObject owner;

        private void Awake()
        {
            Instance = this;
            if (panel != null) panel.SetActive(false);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public bool IsOpenFor(InteractableObject obj) => GameState.InfoOpen && owner == obj;

        public void Open(InteractableObject obj, string title, string body)
        {
            owner = obj;
            titleText.text = title;
            bodyText.text = body;
            panel.SetActive(true);
            GameState.InfoOpen = true;
        }

        public void Close()
        {
            owner = null;
            panel.SetActive(false);
            GameState.InfoOpen = false;
            GameState.MarkClosed();
        }
    }
}
