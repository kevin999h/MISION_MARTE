using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MisionMarte
{
    /// <summary>
    /// Panel de AJUSTES creado por código (no depende de ninguna escena): volumen de música y efectos,
    /// silenciar, tamaño de texto, alto contraste, etiquetas junto al color, reducir vibración de cámara
    /// y mapa de controles. Se abre con el botón "AJUSTES [F1]" (esquina inferior derecha) o con F1,
    /// y se cierra con Esc, F1 o el botón VOLVER. Funciona con mouse y teclado.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        static SettingsPanel inst;
        static bool open;
        static int closedFrame = -10;

        /// <summary>True mientras está abierto (o se cerró en este mismo frame, para que Esc no pause el juego).</summary>
        public static bool IsOpen => open || Time.frameCount == closedFrame;

        public static void Open() { if (inst != null) inst.DoOpen(); }
        public static void Close() { if (inst != null) inst.DoClose(); }

        // ---- referencias de UI ----
        GameObject panelRoot, gearRoot;
        Slider musicSlider, sfxSlider;
        Text musicVal, sfxVal, muteLabel, contrastLabel, iconsLabel, shakeLabel;
        Button muteBtn, contrastBtn, iconsBtn, shakeBtn, backBtn;
        Button[] sizeBtns = new Button[3];
        Font font;

        // ---- estado al abrir ----
        bool prevModal; float prevScale; GameObject prevSelected; float lastBlip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var go = new GameObject("[Ajustes]");
            DontDestroyOnLoad(go);
            go.AddComponent<SettingsPanel>();
        }

        void Awake()
        {
            if (inst != null && inst != this) { Destroy(gameObject); return; }
            inst = this;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildGear();
            BuildPanel();
            panelRoot.SetActive(false);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (inst == this) inst = null;
        }

        void OnSceneLoaded(Scene s, LoadSceneMode m)
        {
            open = false;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        void Update()
        {
            bool blocked = GameState.DialogueOpen || GameState.InfoOpen;
            if (gearRoot != null)
            {
                bool show = !open && !blocked;
                if (gearRoot.activeSelf != show) gearRoot.SetActive(show);
            }
            if (open)
            {
                if (GameInput.PauseDown() || GameInput.SettingsDown()) DoClose();
            }
            else if (GameInput.SettingsDown() && !blocked) DoOpen();
        }

        // ---------------- abrir / cerrar ----------------
        void DoOpen()
        {
            if (open) return;
            open = true;
            prevModal = GameState.ModalOpen;
            prevScale = Time.timeScale;
            GameState.ModalOpen = true;
            Time.timeScale = 0f;
            prevSelected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            Refresh();
            panelRoot.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(musicSlider.gameObject);
            Sfx.Play(SfxType.Blip);
        }

        void DoClose()
        {
            if (!open) return;
            open = false;
            closedFrame = Time.frameCount;
            panelRoot.SetActive(false);
            GameState.ModalOpen = prevModal;
            Time.timeScale = prevScale;
            GameState.MarkClosed();
            AccessibilitySettings.Save();
            if (EventSystem.current != null && prevSelected != null && prevSelected.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(prevSelected);
            Sfx.Play(SfxType.Blip);
        }

        // ---------------- construcción de la interfaz ----------------
        static readonly Color Gold = new Color(1f, 0.8f, 0.3f);
        static readonly Color Cyan = new Color(0.48f, 0.94f, 1f);
        static readonly Color Orange = new Color(0.95f, 0.5f, 0.18f);

        Canvas NewCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(transform, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var cs = go.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<AccessibilityIgnore>();
            return c;
        }

        static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return go;
        }

        static Image NewImg(Transform parent, string name, Color color)
        {
            var im = NewUI(name, parent).AddComponent<Image>();
            im.color = color;
            return im;
        }

        static RectTransform Place(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        static void Stretch(GameObject go, float l = 0, float r = 0, float b = 0, float t = 0)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        Text NewTxt(Transform parent, string name, string text, int size, Color color, TextAnchor align, FontStyle style = FontStyle.Normal)
        {
            var t = NewUI(name, parent).AddComponent<Text>();
            t.font = font; t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = style;
            t.supportRichText = true; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0, 0, 0, 0.85f); o.effectDistance = new Vector2(2, -2);
            return t;
        }

        Button NewBtn(Transform parent, string name, string label, int size, out Text labelText)
        {
            var img = NewImg(parent, name, Color.white);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            SetBtnColors(b, false);
            labelText = NewTxt(img.transform, "Texto", label, size, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(labelText.gameObject);
            return b;
        }

        static void SetBtnColors(Button b, bool on)
        {
            var cb = b.colors;
            cb.normalColor = on ? new Color(0.78f, 0.4f, 0.12f, 1f) : new Color(0.16f, 0.12f, 0.32f, 0.95f);
            cb.highlightedColor = new Color(0.36f, 0.24f, 0.6f, 1f);
            cb.selectedColor = Orange;
            cb.pressedColor = new Color(1f, 0.75f, 0.3f, 1f);
            cb.colorMultiplier = 1f; cb.fadeDuration = 0.08f;
            b.colors = cb;
        }

        void BuildGear()
        {
            var c = NewCanvas("Canvas_BotonAjustes", 590);
            gearRoot = c.gameObject;
            var b = NewBtn(c.transform, "Boton_Ajustes", "AJUSTES  [F1]", 26, out _);
            Place(b.gameObject, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 30), new Vector2(300, 64));
            b.onClick.AddListener(DoOpen);
        }

        // y negativo = distancia desde el borde superior del panel
        Text Label(Transform p, string text, float y)
        {
            var t = NewTxt(p, "Etiqueta", text, 26, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(t.gameObject, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(50, y), new Vector2(400, 60));
            return t;
        }

        Slider NewSlider(Transform p, float y, out Text valueText)
        {
            var root = NewUI("Slider", p);
            Place(root, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(470, y), new Vector2(380, 50));
            var sl = root.AddComponent<Slider>();

            var bg = NewImg(root.transform, "Fondo", new Color(0.2f, 0.16f, 0.4f, 1f));
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.3f); bgRt.anchorMax = new Vector2(1, 0.7f);
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            var fillArea = NewUI("Fill Area", root.transform);
            var faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0, 0.3f); faRt.anchorMax = new Vector2(1, 0.7f);
            faRt.offsetMin = new Vector2(8, 0); faRt.offsetMax = new Vector2(-8, 0);
            var fill = NewImg(fillArea.transform, "Fill", Orange);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(0, 1);
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

            var handleArea = NewUI("Handle Slide Area", root.transform);
            var haRt = handleArea.GetComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(14, 0); haRt.offsetMax = new Vector2(-14, 0);
            var handle = NewImg(handleArea.transform, "Handle", Color.white);
            var hRt = handle.GetComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(28, 0);
            hRt.anchorMin = new Vector2(0, 0); hRt.anchorMax = new Vector2(0, 1);

            sl.fillRect = fillRt;
            sl.handleRect = hRt;
            sl.targetGraphic = handle;
            sl.direction = Slider.Direction.LeftToRight;
            sl.minValue = 0f; sl.maxValue = 1f; sl.wholeNumbers = false;
            var cb = sl.colors;
            cb.normalColor = Color.white; cb.highlightedColor = new Color(0.8f, 0.9f, 1f);
            cb.selectedColor = Orange; cb.pressedColor = Gold;
            sl.colors = cb;

            valueText = NewTxt(p, "Valor", "", 24, Cyan, TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(valueText.gameObject, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(870, y), new Vector2(100, 50));
            return sl;
        }

        Button NewToggle(Transform p, string name, float y, out Text labelText)
        {
            var b = NewBtn(p, name, "NO", 26, out labelText);
            Place(b.gameObject, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(470, y), new Vector2(240, 56));
            return b;
        }

        void BuildPanel()
        {
            var c = NewCanvas("Canvas_Ajustes", 600);
            panelRoot = c.gameObject; // el propio canvas es la raíz
            var dim = NewImg(c.transform, "Atenuado", new Color(0, 0, 0, 0.65f));
            Stretch(dim.gameObject);

            var border = NewImg(c.transform, "Panel", new Color(0.48f, 0.94f, 1f, 0.95f));
            Place(border.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 900));
            var inner = NewImg(border.transform, "Fondo", new Color(0.06f, 0.05f, 0.14f, 0.97f));
            Stretch(inner.gameObject, 4, 4, 4, 4);
            inner.raycastTarget = false;
            var p = border.transform;

            var title = NewTxt(p, "Titulo", "AJUSTES", 52, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(title.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(800, 80));

            Label(p, "Música", -150);
            musicSlider = NewSlider(p, -150, out musicVal);
            Label(p, "Efectos de sonido", -225);
            sfxSlider = NewSlider(p, -225, out sfxVal);

            Label(p, "Silenciar todo", -300);
            muteBtn = NewToggle(p, "Boton_Silenciar", -300, out muteLabel);

            Label(p, "Tamaño del texto", -375);
            string[] names = { "Normal", "Grande", "Muy grande" };
            for (int i = 0; i < 3; i++)
            {
                var b = NewBtn(p, "Boton_Texto_" + i, names[i], 22, out _);
                Place(b.gameObject, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(470 + i * 170, -375), new Vector2(160, 56));
                int idx = i;
                b.onClick.AddListener(() => { AccessibilitySettings.TextSize = idx; Refresh(); Sfx.Play(SfxType.Blip); });
                sizeBtns[i] = b;
            }

            Label(p, "Alto contraste", -450);
            contrastBtn = NewToggle(p, "Boton_Contraste", -450, out contrastLabel);
            Label(p, "Etiquetas además del color", -525);
            iconsBtn = NewToggle(p, "Boton_Etiquetas", -525, out iconsLabel);
            Label(p, "Reducir vibración de cámara", -600);
            shakeBtn = NewToggle(p, "Boton_Vibracion", -600, out shakeLabel);

            var controls = NewTxt(p, "Controles",
                "<color=#7CF0FF>Controles:</color> A/D o flechas mover · Shift correr · Espacio saltar\n" +
                "S/Ctrl agacharse (corriendo: deslizarse) · E interactuar · H pista\n" +
                "Esc pausa · F1 ajustes", 22, Color.white, TextAnchor.UpperLeft);
            Place(controls.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(50, -655), new Vector2(900, 110));

            backBtn = NewBtn(p, "Boton_Volver", "VOLVER", 30, out _);
            Place(backBtn.gameObject, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(300, 66));

            // ---- eventos ----
            musicSlider.onValueChanged.AddListener(v => { AccessibilitySettings.MusicVolume = v; musicVal.text = Mathf.RoundToInt(v * 100) + "%"; });
            sfxSlider.onValueChanged.AddListener(v =>
            {
                AccessibilitySettings.SfxVolume = v; sfxVal.text = Mathf.RoundToInt(v * 100) + "%";
                if (Time.unscaledTime - lastBlip > 0.15f) { lastBlip = Time.unscaledTime; Sfx.Play(SfxType.Blip); }
            });
            muteBtn.onClick.AddListener(() => { AccessibilitySettings.Muted = !AccessibilitySettings.Muted; Refresh(); });
            contrastBtn.onClick.AddListener(() => { AccessibilitySettings.HighContrast = !AccessibilitySettings.HighContrast; Refresh(); Sfx.Play(SfxType.Blip); });
            iconsBtn.onClick.AddListener(() => { AccessibilitySettings.IconMode = !AccessibilitySettings.IconMode; Refresh(); Sfx.Play(SfxType.Blip); });
            shakeBtn.onClick.AddListener(() => { AccessibilitySettings.ReduceShake = !AccessibilitySettings.ReduceShake; Refresh(); Sfx.Play(SfxType.Blip); });
            backBtn.onClick.AddListener(DoClose);
        }

        void Refresh()
        {
            musicSlider.SetValueWithoutNotify(AccessibilitySettings.MusicVolume);
            sfxSlider.SetValueWithoutNotify(AccessibilitySettings.SfxVolume);
            musicVal.text = Mathf.RoundToInt(AccessibilitySettings.MusicVolume * 100) + "%";
            sfxVal.text = Mathf.RoundToInt(AccessibilitySettings.SfxVolume * 100) + "%";
            SetToggle(muteBtn, muteLabel, AccessibilitySettings.Muted);
            SetToggle(contrastBtn, contrastLabel, AccessibilitySettings.HighContrast);
            SetToggle(iconsBtn, iconsLabel, AccessibilitySettings.IconMode);
            SetToggle(shakeBtn, shakeLabel, AccessibilitySettings.ReduceShake);
            for (int i = 0; i < 3; i++) SetBtnColors(sizeBtns[i], AccessibilitySettings.TextSize == i);
        }

        static void SetToggle(Button b, Text label, bool on)
        {
            label.text = on ? "SÍ" : "NO";
            SetBtnColors(b, on);
        }
    }
}
