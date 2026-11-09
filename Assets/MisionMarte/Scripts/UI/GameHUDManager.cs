using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MisionMarte
{
    /// <summary>Tipo de mensaje emergente: cambia el color del recuadro (y agrega una etiqueta si está activada la opción de Ajustes).</summary>
    public enum ToastKind { Info, Success, Error, Warning, Hint }

    /// <summary>
    /// HUD (Head-Up Display) de la Misión 1 "Base limpia, agua segura".
    /// Muestra: misión y objetivo actual, contador de residuos (x/5) con mini-inventario de iconos,
    /// fugas cerradas (x/3), barra de reserva de agua, tuercas, personaje elegido y mensajes.
    /// También controla el panel de "objetivo cumplido" y el panel final del nivel.
    /// </summary>
    public class GameHUDManager : MonoBehaviour
    {
        public static GameHUDManager Instance { get; private set; }

        [Header("Textos del HUD")]
        [SerializeField] private Text missionTitleText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text residuosText;
        [SerializeField] private Text fugasText;
        [SerializeField] private Text tuercasText;
        [SerializeField] private Text aguaText;
        [SerializeField] private Text heroNameText;

        [Header("Barra de agua y mini-inventario")]
        [SerializeField] private RectTransform aguaFill;
        [SerializeField] private Image aguaFillImage;
        [SerializeField] private Image[] residuoSlots;
        [SerializeField] private Image heroPortrait;

        [Header("Mensajes")]
        [SerializeField] private GameObject toastPanel;
        [SerializeField] private Text toastText;

        [Header("Panel de objetivo cumplido (5/5)")]
        [SerializeField] private GameObject modalPanel;
        [SerializeField] private Text modalTitle;
        [SerializeField] private Text modalBody;
        [SerializeField] private Button modalButton;

        [Header("Panel de nivel completado")]
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private Text levelStatsText;
        [SerializeField] private Text starsText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;

        [Header("Configuración de la misión")]
        [SerializeField] private int totalResiduos = 5;
        [SerializeField] private float aguaInicial = 100f;
        [SerializeField] private float drainPerLeakPerSecond = 0.3f;
        [SerializeField] private float aguaPorGota = 5f;
        [SerializeField] private int tuercasParaTurbo = 10;
        [SerializeField] private string menuSceneName = "Menu";

        [Header("Sprites")]
        [SerializeField] private Sprite pixelSprite;
        [SerializeField] private Sprite ligiSprite;
        [SerializeField] private Sprite mariSprite;

        // ---- estado ----
        private int residuos, tuercas, leaksClosed, totalLeaks, gotas;
        private float agua;
        private bool talkedToCommander, residuosModalShown, levelDone, waterEmptyWarned;
        private float toastTimer, elapsed;
        private int modalOpenedFrame;
        private WaterLeak[] leaks;

        // ---- LevelConfig, pistas y retroalimentación ----
        private LevelConfig cfg;
        private int hintsLeft;
        private float hintCost, starsThree = 60f, starsTwo = 30f;
        private bool waterLowWarned;
        private Image toastBorder, toastInner;
        private Color toastBorderBase, toastInnerBase;
        private RectTransform toastRt;
        private Vector2 toastBasePos;
        private float toastPunch, toastShake;

        /// <summary>False si el LevelConfig desactiva los consejos contextuales.</summary>
        public bool ShowTips => cfg == null || cfg.mostrarConsejos;

        public bool ObjectiveComplete => residuos >= totalResiduos && leaksClosed >= totalLeaks;
        public int RemainingResiduos => Mathf.Max(0, totalResiduos - residuos);
        public int RemainingLeaks => Mathf.Max(0, totalLeaks - leaksClosed);
        public float Agua => agua;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start()
        {
            // Parámetros de dificultad del nivel (LevelConfig); si no existe el asset se usan los de la escena
            cfg = LevelConfig.ForLevel(LevelConfig.CurrentLevelIndex());
            int slots = residuoSlots != null && residuoSlots.Length > 0 ? residuoSlots.Length : totalResiduos;
            totalResiduos = Mathf.Clamp(cfg.residuosRequeridos, 1, slots);
            aguaInicial = cfg.aguaInicial;
            drainPerLeakPerSecond = cfg.perdidaPorFugaPorSegundo;
            aguaPorGota = cfg.aguaPorGota;
            tuercasParaTurbo = cfg.tuercasParaTurbo;
            hintsLeft = cfg.pistasDisponibles;
            hintCost = cfg.costoPistaAgua;
            starsThree = cfg.aguaTresEstrellas;
            starsTwo = cfg.aguaDosEstrellas;
            GameSession.LevelSpeed = cfg.multiplicadorVelocidad;

            if (toastPanel != null)
            {
                toastRt = toastPanel.GetComponent<RectTransform>();
                toastBasePos = toastRt.anchoredPosition;
                toastBorder = toastPanel.GetComponent<Image>();
                if (toastBorder != null) toastBorderBase = toastBorder.color;
                if (toastPanel.transform.childCount > 0)
                {
                    toastInner = toastPanel.transform.GetChild(0).GetComponent<Image>();
                    if (toastInner != null) toastInnerBase = toastInner.color;
                }
            }

            agua = aguaInicial;
            leaks = FindObjectsByType<WaterLeak>(FindObjectsSortMode.None);
            totalLeaks = leaks.Length;

            if (toastPanel != null) toastPanel.SetActive(false);
            if (modalPanel != null) modalPanel.SetActive(false);
            if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
            if (residuoSlots != null) foreach (var s in residuoSlots) if (s != null) { s.color = new Color(1, 1, 1, 0.15f); }

            if (heroNameText != null) heroNameText.text = GameSession.HeroName.ToUpper() + "\n<size=18>" + GameSession.HeroRole + "</size>";
            if (heroPortrait != null)
            {
                heroPortrait.sprite = GameSession.Selected == Heroine.Ligi ? ligiSprite : mariSprite;
                heroPortrait.preserveAspect = true;
            }
            if (missionTitleText != null) missionTitleText.text = "MISIÓN 1 · BASE LIMPIA, AGUA SEGURA";

            if (modalButton != null) modalButton.onClick.AddListener(CloseModal);
            if (retryButton != null) retryButton.onClick.AddListener(Retry);
            if (menuButton != null) menuButton.onClick.AddListener(GoMenu);

            Refresh();
            Toast("Bienvenida, " + GameSession.HeroName + ". Habla con el Comandante Orión (tecla E)." +
                  (hintsLeft > 0 ? "\n<size=22>Si te pierdes, pulsa H para pedir una pista (" + hintsLeft + " disponibles).</size>" : ""), 5f);
        }

        private void Update()
        {
            if (!levelDone && !GameState.Paused) elapsed += Time.deltaTime;

            // La reserva de agua baja mientras haya fugas abiertas (se pausa durante diálogos y menús)
            if (!levelDone && !GameState.Locked)
            {
                int open = 0;
                foreach (var l in leaks) if (l != null && l.IsOpen) open++;
                if (open > 0)
                {
                    agua = Mathf.Max(0f, agua - open * drainPerLeakPerSecond * Time.deltaTime);
                    if (agua <= 25f && agua > 0f && !waterLowWarned)
                    {
                        waterLowWarned = true;
                        Toast("Reserva de agua baja (" + Mathf.RoundToInt(agua) + " %). Cierra las fugas con la tecla E.", 4f, ToastKind.Warning);
                    }
                    if (agua <= 0f && !waterEmptyWarned)
                    {
                        waterEmptyWarned = true;
                        Toast("¡La reserva de agua se agotó! Cierra las fugas para no perder más.", 5f, ToastKind.Error);
                        Sfx.Play(SfxType.Error);
                        CameraFollow2D.Shake(0.25f, 0.12f);
                    }
                }
            }
            UpdateWaterBar();

            if (!levelDone && !GameState.Locked && GameInput.HintDown()) UseHint();

            AnimateToast();

            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f && toastPanel != null) toastPanel.SetActive(false);
            }

            // Cerrar el panel modal con E / Enter / Espacio
            if (modalPanel != null && modalPanel.activeSelf && Time.frameCount > modalOpenedFrame + 1 && GameInput.AdvanceDown())
                CloseModal();

            if (levelCompletePanel != null && levelCompletePanel.activeSelf && Time.frameCount > modalOpenedFrame + 1)
            {
#if ENABLE_INPUT_SYSTEM
                var k = UnityEngine.InputSystem.Keyboard.current;
                if (k != null && k.rKey.wasPressedThisFrame) Retry();
                if (k != null && k.mKey.wasPressedThisFrame) GoMenu();
#else
                if (Input.GetKeyDown(KeyCode.R)) Retry();
                if (Input.GetKeyDown(KeyCode.M)) GoMenu();
#endif
            }
        }

        // ---------------- API usada por los demás scripts ----------------
        public void AddCollectible(CollectibleType type, int value, string itemName, string fact, Sprite icon = null)
        {
            switch (type)
            {
                case CollectibleType.Residuo:
                    if (residuoSlots != null && residuos < residuoSlots.Length && residuoSlots[residuos] != null)
                    {
                        if (icon != null) residuoSlots[residuos].sprite = icon;
                        residuoSlots[residuos].color = Color.white;
                        residuoSlots[residuos].preserveAspect = true;
                    }
                    residuos = Mathf.Min(totalResiduos, residuos + value);
                    Sfx.Play(SfxType.Collect);
                    Toast("+1 " + itemName + (string.IsNullOrEmpty(fact) ? "" : "\n<size=22>" + fact + "</size>"), 5f, ToastKind.Success);
                    if (residuos >= totalResiduos && !residuosModalShown) { residuosModalShown = true; ShowResiduosComplete(); }
                    break;

                case CollectibleType.Tuerca:
                    tuercas += value;
                    Sfx.Play(SfxType.Nut);
                    if (tuercasParaTurbo > 0 && tuercas % tuercasParaTurbo == 0 && PlayerController.Instance != null)
                    {
                        PlayerController.Instance.ApplyBoost(4f, 1.45f);
                        Toast("¡" + tuercas + " tuercas! Turbo de reciclaje activado", 3f, ToastKind.Success);
                    }
                    break;

                case CollectibleType.Gota:
                    gotas += value;
                    agua = Mathf.Min(100f, agua + aguaPorGota * value);
                    Sfx.Play(SfxType.Collect);
                    if (gotas == 1) Toast("+Agua recuperada. Las gotas suben la reserva de agua de la base.", 3.5f, ToastKind.Success);
                    break;
            }
            Refresh();
        }

        public void LeakClosed(string fact)
        {
            leaksClosed++;
            Toast("¡Fuga cerrada! (" + leaksClosed + "/" + totalLeaks + ")\n<size=22>" + fact + "</size>", 5.5f, ToastKind.Success);
            if (leaksClosed >= totalLeaks)
                Toast("¡Cerraste todas las fugas! La reserva de agua ya no baja.\n<size=22>" + fact + "</size>", 5.5f, ToastKind.Success);
            if (ObjectiveComplete && residuosModalShown) Toast("¡Objetivo cumplido! Ve con " + GameSession.CompanionName + " a la Estación de Reciclaje.", 6f, ToastKind.Success);
            Refresh();
        }

        public void OnTalkedToNPC()
        {
            if (talkedToCommander) return;
            talkedToCommander = true;
            Refresh();
        }

        public void Toast(string msg, float duration = 3f, ToastKind kind = ToastKind.Info)
        {
            if (toastText == null || toastPanel == null) return;
            string tag = "";
            if (AccessibilitySettings.IconMode)
            {
                // Etiqueta de texto además del color (para no depender solo del color)
                switch (kind)
                {
                    case ToastKind.Success: tag = "[OK] "; break;
                    case ToastKind.Error: tag = "[ERROR] "; break;
                    case ToastKind.Warning: tag = "[AVISO] "; break;
                    case ToastKind.Hint: tag = "[PISTA] "; break;
                }
            }
            toastText.text = tag + msg;
            ApplyToastColors(kind);
            toastPunch = 1f;
            toastShake = kind == ToastKind.Error && !AccessibilitySettings.ReduceShake ? 0.35f : 0f;
            toastPanel.SetActive(true);
            toastTimer = duration;
        }

        private void ApplyToastColors(ToastKind kind)
        {
            Color border = toastBorderBase, fill = toastInnerBase;
            switch (kind)
            {
                case ToastKind.Success: border = new Color(0.35f, 0.95f, 0.5f, 0.95f); fill = new Color(0.03f, 0.14f, 0.07f, toastInnerBase.a); break;
                case ToastKind.Error:   border = new Color(1f, 0.4f, 0.4f, 0.95f);     fill = new Color(0.2f, 0.04f, 0.05f, toastInnerBase.a); break;
                case ToastKind.Warning: border = new Color(1f, 0.65f, 0.2f, 0.95f);    fill = new Color(0.2f, 0.12f, 0.02f, toastInnerBase.a); break;
                case ToastKind.Hint:    border = new Color(0.48f, 0.94f, 1f, 0.95f);   fill = new Color(0.03f, 0.1f, 0.16f, toastInnerBase.a); break;
            }
            if (toastBorder != null) toastBorder.color = border;
            if (toastInner != null) toastInner.color = fill;
        }

        private void AnimateToast()
        {
            if (toastRt == null || toastPanel == null || !toastPanel.activeSelf) return;
            toastPunch = Mathf.MoveTowards(toastPunch, 0f, Time.unscaledDeltaTime * 4f);
            toastRt.localScale = Vector3.one * (1f + 0.06f * toastPunch);
            Vector2 off = Vector2.zero;
            if (toastShake > 0f)
            {
                toastShake -= Time.unscaledDeltaTime;
                off = new Vector2(Mathf.Sin(Time.unscaledTime * 80f) * 12f * Mathf.Clamp01(toastShake / 0.35f), 0f);
            }
            toastRt.anchoredPosition = toastBasePos + off;
        }

        // ---------------- sistema de pistas (tecla H) ----------------
        private void UseHint()
        {
            if (PlayerController.Instance == null) return;
            if (hintsLeft <= 0)
            {
                Toast("Ya no quedan pistas en este nivel. ¡Tú puedes!", 3f, ToastKind.Warning);
                Sfx.Play(SfxType.Error);
                return;
            }
            hintsLeft--;
            if (hintCost > 0f) agua = Mathf.Max(0f, agua - hintCost);

            Vector3 p = PlayerController.Instance.transform.position;
            string msg;
            if (!talkedToCommander)
                msg = "habla con el Comandante Orión: acércate a él y pulsa E.";
            else if (residuos < totalResiduos)
            {
                CollectibleItem best = null; float bd = float.MaxValue;
                foreach (var c in CollectibleItem.Active)
                {
                    if (c == null || c.Type != CollectibleType.Residuo) continue;
                    float d = Vector2.Distance(c.transform.position, p);
                    if (d < bd) { bd = d; best = c; }
                }
                msg = best != null ? "te falta un residuo " + Direction(p, best.transform.position) + "."
                                   : "revisa el escenario, aún faltan " + RemainingResiduos + " residuos.";
            }
            else if (RemainingLeaks > 0)
            {
                WaterLeak best = null; float bd = float.MaxValue;
                foreach (var l in leaks)
                {
                    if (l == null || !l.IsOpen) continue;
                    float d = Vector2.Distance(l.transform.position, p);
                    if (d < bd) { bd = d; best = l; }
                }
                msg = best != null ? "hay una fuga abierta " + Direction(p, best.transform.position) + ". Ciérrala con E."
                                   : "cierra las fugas de agua que quedan.";
            }
            else
                msg = "ve con " + GameSession.CompanionName + " a la Estación de Reciclaje, al final del nivel.";

            Toast("Pista: " + msg + "\n<size=22>Pistas restantes: " + hintsLeft + (hintCost > 0f ? " · costó " + hintCost + " % de agua" : "") + "</size>", 5f, ToastKind.Hint);
            Sfx.Play(SfxType.Hint);
        }

        private static string Direction(Vector3 from, Vector3 to)
        {
            float dx = to.x - from.x, dy = to.y - from.y;
            string s = Mathf.Abs(dx) < 2f ? "muy cerca de ti" : (dx > 0 ? "hacia la derecha" : "hacia la izquierda");
            if (dy > 2.5f) s += " y más arriba"; else if (dy < -2.5f) s += " y más abajo";
            return s + " (a unos " + Mathf.RoundToInt(Mathf.Abs(dx)) + " m)";
        }

        public void SpawnBurst(Vector3 pos, Color color) => Burst.Spawn(pos, color, pixelSprite);

        public void ShowLevelComplete()
        {
            if (levelDone) return;
            levelDone = true;
            GameState.ModalOpen = true;
            modalOpenedFrame = Time.frameCount;
            Sfx.Play(SfxType.Success);

            int stars = agua >= starsThree ? 3 : agua >= starsTwo ? 2 : 1;
            if (starsText != null) starsText.text = "Calificación de agua: " + stars + " de 3 estrellas  " + new string('*', stars);
            int min = Mathf.FloorToInt(elapsed / 60f), sec = Mathf.FloorToInt(elapsed % 60f);
            if (levelStatsText != null)
                levelStatsText.text =
                    "Personaje: " + GameSession.HeroName + "\n" +
                    "Residuos recolectados: " + residuos + " / " + totalResiduos + "\n" +
                    "Fugas de agua cerradas: " + leaksClosed + " / " + totalLeaks + "\n" +
                    "Reserva de agua final: " + Mathf.RoundToInt(agua) + " %\n" +
                    "Tuercas recicladas: " + tuercas + "\n" +
                    "Tiempo: " + min.ToString("00") + ":" + sec.ToString("00") + "\n\n" +
                    "<size=22>Aprendiste: separar residuos (caneca blanca, verde y negra), que pilas y\n" +
                    "electrónicos van a puntos especiales, y que cerrar fugas ahorra agua.</size>";
            if (levelCompletePanel != null) levelCompletePanel.SetActive(true);
            if (retryButton != null) retryButton.Select();
        }

        // ---------------- interno ----------------
        private void ShowResiduosComplete()
        {
            if (modalPanel == null) return;
            GameState.ModalOpen = true;
            modalOpenedFrame = Time.frameCount;
            Sfx.Play(SfxType.Success);
            if (modalTitle != null) modalTitle.text = "¡RECOLECTASTE LOS " + totalResiduos + " RESIDUOS!";
            if (modalBody != null)
                modalBody.text = RemainingLeaks > 0
                    ? "¡Felicidades! Cumpliste el objetivo de recolección.\nAún quedan " + RemainingLeaks + " fugas de agua abiertas: ciérralas con E.\nLuego habla con " + GameSession.CompanionName + " en la Estación de Reciclaje para finalizar."
                    : "¡Felicidades! Cumpliste el objetivo pedagógico.\nHabla con " + GameSession.CompanionName + " en la Estación de Reciclaje para finalizar la misión.";
            modalPanel.SetActive(true);
            if (modalButton != null) modalButton.Select();
        }

        private void CloseModal()
        {
            if (modalPanel == null || !modalPanel.activeSelf) return;
            modalPanel.SetActive(false);
            GameState.ModalOpen = false;
            GameState.MarkClosed();
        }

        private void Retry()
        {
            SceneLoader.ReloadCurrent();
        }

        private void GoMenu()
        {
            SceneLoader.Load(menuSceneName);
        }

        private void Refresh()
        {
            if (residuosText != null) residuosText.text = "Residuos: " + residuos + " / " + totalResiduos;
            if (fugasText != null) fugasText.text = "Fugas cerradas: " + leaksClosed + " / " + totalLeaks;
            if (tuercasText != null) tuercasText.text = "Tuercas: " + tuercas;
            if (objectiveText != null)
            {
                string o;
                if (!talkedToCommander) o = "Objetivo: habla con el Comandante Orión (E)";
                else if (!ObjectiveComplete)
                {
                    o = "Objetivo: recolecta los residuos y cierra las fugas de agua";
                    if (residuos >= totalResiduos) o = "Objetivo: cierra las fugas de agua (" + RemainingLeaks + " restantes)";
                    else if (leaksClosed >= totalLeaks) o = "Objetivo: recolecta los residuos (" + RemainingResiduos + " restantes)";
                }
                else o = "Objetivo: lleva los residuos a " + GameSession.CompanionName + " en la Estación de Reciclaje >>";
                objectiveText.text = o;
            }
            UpdateWaterBar();
        }

        private void UpdateWaterBar()
        {
            float t = Mathf.Clamp01(agua / 100f);
            if (aguaFill != null) aguaFill.anchorMax = new Vector2(t, 1f);
            if (aguaFillImage != null)
                aguaFillImage.color = t > 0.5f ? new Color(0.25f, 0.65f, 1f) : t > 0.25f ? new Color(1f, 0.75f, 0.25f) : new Color(1f, 0.3f, 0.3f);
            if (aguaText != null) aguaText.text = "Reserva de agua: " + Mathf.RoundToInt(agua) + " %";
        }
    }
}
