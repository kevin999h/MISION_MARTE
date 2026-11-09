using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MisionMarte.EditorTools
{
    /// <summary>
    /// CONSTRUCTOR AUTOMÁTICO de "Misión Marte: Sobrevive".
    /// Al importar el paquete arma TODO solo: configura los sprites, crea las capas de orden,
    /// construye las escenas "Menu" (menú + selección Ligi/Mariale) y "Nivel1" (nivel completo),
    /// y las agrega al Build Settings. No hay que arrastrar ni configurar nada a mano.
    /// Si quieres regenerarlas: menú superior  Misión Marte > Reconstruir escenas.
    /// </summary>
    [InitializeOnLoad]
    public static class MisionMarteBuilder
    {
        const string Root = "Assets/MisionMarte";
        const string ArtDir = Root + "/Arte/";
        const string SceneDir = Root + "/Scenes";
        const string MenuScene = SceneDir + "/Menu.unity";
        const string LevelScene = SceneDir + "/Nivel1.unity";
        const string VersionFile = SceneDir + "/version_constructor.txt";
        const string BuilderVersion = "5";

        static MisionMarteBuilder()
        {
            EditorApplication.delayCall += TryAutoBuild;
        }

        static void TryAutoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            bool upToDate = File.Exists(VersionFile) && File.ReadAllText(VersionFile).Trim() == BuilderVersion;
            if (File.Exists(LevelScene) && File.Exists(MenuScene) && upToDate)
            {
                // Las escenas ya existen: solo asegura que estén en el Build Profile / Build Settings.
                try { SetBuildSettings(); } catch (Exception e) { Debug.LogWarning("[Misión Marte] " + e.Message); }
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryAutoBuild;
                return;
            }
            bool outdated = File.Exists(LevelScene) && File.Exists(MenuScene) && !upToDate;
            if (outdated) BuildAll(false, true, true); // escenas de una versión anterior con errores: se rehacen
            else BuildAll(false);
        }

        [MenuItem("Misión Marte/Reconstruir escenas (Menú + Nivel 1)", priority = 0)]
        public static void RebuildMenu() => BuildAll(true);

        [MenuItem("Misión Marte/Abrir Menú principal", priority = 20)]
        public static void OpenMenu() { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(MenuScene); }

        [MenuItem("Misión Marte/Abrir Nivel 1", priority = 21)]
        public static void OpenLevel() { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(LevelScene); }

        [MenuItem("Misión Marte/Construir solo el Nivel 1", priority = 1)]
        public static void BuildOnlyLevel() => BuildAll(false, false, true);

        public static void BuildAll(bool ask) => BuildAll(ask, ask || !File.Exists(MenuScene), ask || !File.Exists(LevelScene));

        public static void BuildAll(bool ask, bool doMenu, bool doLevel)
        {
            if (ask && !EditorUtility.DisplayDialog("Misión Marte", "Esto vuelve a crear las escenas Menu y Nivel1 desde cero. ¿Continuar?", "Sí, reconstruir", "Cancelar")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                EditorUtility.DisplayProgressBar("Misión Marte", "Configurando sprites...", 0.1f);
                Step("Configurar sprites", ConfigureTextures);
                EditorUtility.DisplayProgressBar("Misión Marte", "Creando capas...", 0.2f);
                Step("Capas de orden", EnsureSortingLayers);
                Step("Capa Ground", () => EnsurePhysicsLayer("Ground"));
                if (!AssetDatabase.IsValidFolder(SceneDir)) AssetDatabase.CreateFolder(Root, "Scenes");
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                spriteCache.Clear();

                if (doMenu)
                {
                    EditorUtility.DisplayProgressBar("Misión Marte", "Construyendo menú...", 0.4f);
                    Step("Escena Menu", BuildMenuScene);
                }
                if (doLevel)
                {
                    EditorUtility.DisplayProgressBar("Misión Marte", "Construyendo Nivel 1...", 0.7f);
                    Step("Escena Nivel1", BuildLevelScene);
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Step("Build Profile", SetBuildSettings);
            }
            finally { EditorUtility.ClearProgressBar(); }

            if (File.Exists(MenuScene)) EditorSceneManager.OpenScene(MenuScene);
            if (File.Exists(LevelScene) && File.Exists(MenuScene))
            {
                try { File.WriteAllText(VersionFile, BuilderVersion); AssetDatabase.ImportAsset(VersionFile); } catch { }
            }
            if (File.Exists(LevelScene) && File.Exists(MenuScene))
                Debug.Log("<color=#7CF0FF><b>[Misión Marte]</b></color> ¡Listo! Escenas Menu y Nivel1 creadas en " + SceneDir + ". Presiona PLAY en la escena Menu.");
            else
                Debug.LogError("[Misión Marte] No se pudo crear alguna escena. Revisa el error anterior que empieza con [Misión Marte] y envíaselo a Claude.");
        }

        /// <summary>Ejecuta un paso y, si falla, muestra el error con su nombre sin detener lo demás.</summary>
        static void Step(string name, Action action)
        {
            try { action(); }
            catch (Exception e) { Debug.LogError("[Misión Marte] Falló el paso '" + name + "': " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
        }

        // =====================================================================================
        //  CONFIGURACIÓN DEL PROYECTO
        // =====================================================================================
        static void ConfigureTextures()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/Arte" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                bool isLogo = path.EndsWith("Logo.png");
                if (imp.textureType == TextureImporterType.Sprite && Mathf.Approximately(imp.spritePixelsPerUnit, isLogo ? 100 : 16)
                    && imp.filterMode == (isLogo ? FilterMode.Bilinear : FilterMode.Point)) continue;
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = isLogo ? 100 : 16;
                imp.filterMode = isLogo ? FilterMode.Bilinear : FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                var s = new TextureImporterSettings();
                imp.ReadTextureSettings(s);
                s.spriteMeshType = SpriteMeshType.FullRect;
                s.spriteAlignment = (int)SpriteAlignment.Center;
                imp.SetTextureSettings(s);
                imp.SaveAndReimport();
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        static readonly string[] ExtraSortingLayers = { "Ground", "Items", "Characters", "FX" };

        static void EnsureSortingLayers()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;
            var tm = new SerializedObject(assets[0]);
            var layers = tm.FindProperty("m_SortingLayers");
            if (layers == null) return;

            bool Has(string n)
            {
                for (int i = 0; i < layers.arraySize; i++)
                    if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == n) return true;
                return false;
            }
            void Add(string n, int index)
            {
                layers.InsertArrayElementAtIndex(index);
                var e = layers.GetArrayElementAtIndex(index);
                e.FindPropertyRelative("name").stringValue = n;
                e.FindPropertyRelative("uniqueID").intValue = StableId(n);
                var locked = e.FindPropertyRelative("locked");
                if (locked != null) locked.boolValue = false;
            }
            if (!Has("Background")) Add("Background", 0); // detrás de Default
            foreach (var n in ExtraSortingLayers) if (!Has(n)) Add(n, layers.arraySize);
            tm.ApplyModifiedPropertiesWithoutUndo();
        }

        static int StableId(string s)
        {
            unchecked
            {
                int h = 23;
                foreach (char c in "MM_" + s) h = h * 31 + c;
                h &= 0x7fffffff;
                return h == 0 ? 1 : h;
            }
        }

        static int groundLayer;
        static void EnsurePhysicsLayer(string name)
        {
            groundLayer = LayerMask.NameToLayer(name);
            if (groundLayer >= 0) return;
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) { groundLayer = 0; return; }
            var tm = new SerializedObject(assets[0]);
            var layers = tm.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var p = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(p.stringValue)) { p.stringValue = name; groundLayer = i; break; }
            }
            tm.ApplyModifiedPropertiesWithoutUndo();
            if (groundLayer < 0) groundLayer = 0;
        }

        static void SetBuildSettings()
        {
            var list = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MenuScene, true),
                new EditorBuildSettingsScene(LevelScene, true)
            };
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path != MenuScene && s.path != LevelScene && File.Exists(s.path)) list.Add(s);
            var arr = list.ToArray();
            bool changed = EditorBuildSettings.scenes.Length < 2 || EditorBuildSettings.scenes[0].path != MenuScene || EditorBuildSettings.scenes[1].path != LevelScene;
            if (changed) EditorBuildSettings.scenes = arr;
            SetActiveBuildProfileScenes(arr);
        }

        /// <summary>Unity 6: si el Build Profile activo tiene su propia lista de escenas, también las agrega ahí.</summary>
        static void SetActiveBuildProfileScenes(EditorBuildSettingsScene[] arr)
        {
            try
            {
                Type t = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    t = asm.GetType("UnityEditor.Build.Profile.BuildProfile");
                    if (t != null) break;
                }
                if (t == null) return;
                var getActive = t.GetMethod("GetActiveBuildProfile", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var profile = getActive != null ? getActive.Invoke(null, null) as Object : null;
                if (profile == null) return;
                var prop = t.GetProperty("scenes");
                if (prop == null || !prop.CanWrite) return;
                var current = prop.GetValue(profile) as EditorBuildSettingsScene[];
                if (current != null && current.Length >= 2 && current[0].path == MenuScene && current[1].path == LevelScene) return;
                var merged = new List<EditorBuildSettingsScene>(arr);
                if (current != null) foreach (var s in current) if (s.path != MenuScene && s.path != LevelScene && File.Exists(s.path)) merged.Add(s);
                prop.SetValue(profile, merged.ToArray());
                EditorUtility.SetDirty(profile);
            }
            catch (Exception e) { Debug.LogWarning("[Misión Marte] No se pudo actualizar el Build Profile: " + e.Message); }
        }

        // =====================================================================================
        //  UTILIDADES
        // =====================================================================================
        static Font font;
        static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        static Sprite Spr(string name)
        {
            if (spriteCache.TryGetValue(name, out var s) && s != null) return s;
            s = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + name + ".png");
            if (s == null)
            {
                s = AssetDatabase.LoadAllAssetsAtPath(ArtDir + name + ".png").OfType<Sprite>().FirstOrDefault();
                if (s == null) Debug.LogWarning("[Misión Marte] No se encontró el sprite " + name);
            }
            spriteCache[name] = s;
            return s;
        }

        static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning("[Misión Marte] Campo no encontrado: " + target.GetType().Name + "." + field); return; }
            switch (value)
            {
                case null: p.objectReferenceValue = null; break;
                case Object o: p.objectReferenceValue = o; break;
                case string str: p.stringValue = str; break;
                case bool b: p.boolValue = b; break;
                case int i: if (p.propertyType == SerializedPropertyType.Enum) p.enumValueIndex = i; else p.intValue = i; break;
                case float f: p.floatValue = f; break;
                case Color c: p.colorValue = c; break;
                case Vector2 v2: p.vector2Value = v2; break;
                case Vector3 v3: p.vector3Value = v3; break;
                case Enum en: p.enumValueIndex = Convert.ToInt32(en); break;
                default: Debug.LogWarning("[Misión Marte] Tipo no soportado " + value.GetType()); break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning("[Misión Marte] Campo no encontrado: " + field); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        struct L { public string who; public Color col; public string text; public L(string w, Color c, string t) { who = w; col = c; text = t; } }

        static void SetLines(Object target, string field, params L[] lines)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                var e = p.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("speakerName").stringValue = lines[i].who;
                e.FindPropertyRelative("nameColor").colorValue = lines[i].col;
                e.FindPropertyRelative("text").stringValue = lines[i].text;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject GO(string name, Transform parent = null, Vector3? pos = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            if (pos.HasValue) go.transform.position = pos.Value;
            return go;
        }

        static SpriteRenderer SR(GameObject go, Sprite s, string layer, int order, Color? color = null)
        {
            // OJO: en Unity no se puede usar '??' con componentes (GetComponent devuelve un "nulo falso" en el editor).
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingLayerName = layer;
            sr.sortingOrder = order;
            if (color.HasValue) sr.color = color.Value;
            return sr;
        }

        static SpriteRenderer Tiled(GameObject go, Sprite s, Vector2 size, string layer, int order, Color? color = null)
        {
            var sr = SR(go, s, layer, order, color);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = size;
            return sr;
        }

        static GameObject Visual(Transform parent, string spriteName, Vector3 localPos, float scale, string layer, int order)
        {
            var v = GO("Visual", parent);
            v.transform.localPosition = localPos;
            v.transform.localScale = Vector3.one * scale;
            SR(v, Spr(spriteName), layer, order);
            return v;
        }

        static GameObject Prompt(Transform parent, float height)
        {
            var p = GO("Indicador_E", parent);
            p.transform.localPosition = new Vector3(0f, height, 0f);
            var inner = GO("Tecla", p.transform);
            inner.transform.localScale = Vector3.one * 1.1f;
            SR(inner, Spr("TeclaE"), "FX", 50);
            inner.AddComponent<Bobber>();
            return p;
        }

        // ------------------------------- UI -------------------------------
        static GameObject UI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return go;
        }

        static RectTransform Place(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        static RectTransform Stretch(GameObject go, float l = 0, float r = 0, float b = 0, float t = 0)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        static Image Img(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var go = UI(name, parent);
            var img = go.AddComponent<Image>();
            img.color = color; img.sprite = sprite;
            if (sprite != null) img.preserveAspect = true;
            return img;
        }

        static Image Panel(Transform parent, string name, Color fill, Color border, float borderSize = 4f)
        {
            var outer = Img(parent, name, border);
            var inner = Img(outer.transform, "Fondo", fill);
            Stretch(inner.gameObject, borderSize, borderSize, borderSize, borderSize);
            inner.raycastTarget = false;
            return outer;
        }

        static Text Txt(Transform parent, string name, string text, int size, Color color, TextAnchor align, FontStyle style = FontStyle.Normal)
        {
            var go = UI(name, parent);
            var t = go.AddComponent<Text>();
            t.font = font; t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = style;
            t.supportRichText = true; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            var o = go.AddComponent<Outline>();
            o.effectColor = new Color(0, 0, 0, 0.85f); o.effectDistance = new Vector2(2, -2);
            return t;
        }

        static readonly Color Gold = new Color(1f, 0.8f, 0.3f);
        static readonly Color Cyan = new Color(0.48f, 0.94f, 1f);
        static readonly Color PanelFill = new Color(0.06f, 0.05f, 0.14f, 0.88f);
        static readonly Color PanelBorder = new Color(0.48f, 0.94f, 1f, 0.9f);

        static Button Btn(Transform parent, string name, string label, int size = 34)
        {
            var img = Img(parent, name, Color.white);
            var b = img.gameObject.AddComponent<Button>();
            var cb = b.colors;
            cb.normalColor = new Color(0.16f, 0.12f, 0.32f, 0.95f);
            cb.highlightedColor = new Color(0.36f, 0.24f, 0.6f, 1f);
            cb.selectedColor = new Color(0.95f, 0.5f, 0.18f, 1f);
            cb.pressedColor = new Color(1f, 0.75f, 0.3f, 1f);
            cb.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
            cb.colorMultiplier = 1f; cb.fadeDuration = 0.08f;
            b.colors = cb;
            b.targetGraphic = img;
            var t = Txt(img.transform, "Texto", label, size, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(t.gameObject);
            return b;
        }

        static Canvas MakeCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 10;
            var cs = go.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        static Camera MakeCamera(Color bg)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0, 0, -10);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
            go.AddComponent<AudioListener>();
            return cam;
        }

        /// <summary>En proyectos URP 2D agrega una luz global para que los sprites se vean con su color real.</summary>
        static void AddGlobalLightIfURP()
        {
            try
            {
                var t = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
                if (t == null || UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline == null) return;
                var go = new GameObject("Luz Global 2D");
                var light = go.AddComponent(t);
                var so = new SerializedObject(light);
                var lt = so.FindProperty("m_LightType");
                if (lt != null) lt.intValue = 4; // 4 = Global
                var layers = so.FindProperty("m_ApplyToSortingLayers"); // que ilumine TODAS las capas de orden
                if (layers != null)
                {
                    var all = SortingLayer.layers;
                    layers.arraySize = all.Length;
                    for (int i = 0; i < all.Length; i++) layers.GetArrayElementAtIndex(i).intValue = all[i].id;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            catch (Exception e) { Debug.LogWarning("[Misión Marte] No se pudo crear la luz 2D global: " + e.Message); }
        }

        static void EventSystemFix() => new GameObject("EventSystem (auto)").AddComponent<UIEventSystemFix>();

        // =====================================================================================
        //  ESCENA: MENÚ PRINCIPAL + SELECCIÓN DE PERSONAJE
        // =====================================================================================
        static void BuildMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MakeCamera(new Color(0.03f, 0.04f, 0.12f));
            AddGlobalLightIfURP();
            EventSystemFix();

            var canvas = MakeCanvas("Canvas_Menu");
            var root = canvas.transform;

            var bg = Img(root, "Fondo", Color.white, Spr("FondoCielo"));
            bg.preserveAspect = false;
            Stretch(bg.gameObject);
            var shade = Img(root, "Oscurecer", new Color(0.02f, 0.01f, 0.08f, 0.45f));
            Stretch(shade.gameObject);

            // ---------- Panel principal ----------
            var main = UI("PanelPrincipal", root); Stretch(main);
            var logo = Img(main.transform, "Logo", Color.white, Spr("Logo"));
            Place(logo.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420, 20), new Vector2(760, 760));

            var bJugar = Btn(main.transform, "BotonJugar", "JUGAR", 40);
            Place(bJugar.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(470, 170), new Vector2(480, 96));
            var bControles = Btn(main.transform, "BotonControles", "CONTROLES");
            Place(bControles.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(470, 40), new Vector2(480, 96));
            var bCreditos = Btn(main.transform, "BotonCreditos", "CRÉDITOS");
            Place(bCreditos.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(470, -90), new Vector2(480, 96));
            var bSalir = Btn(main.transform, "BotonSalir", "SALIR");
            Place(bSalir.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(470, -220), new Vector2(480, 96));
            var sub = Txt(main.transform, "Subtitulo", "Serious Game educativo · Misión 1: Base limpia, agua segura", 28, Cyan, TextAnchor.MiddleCenter);
            Place(sub.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(1600, 50));
            var nav = Txt(main.transform, "AyudaTeclado", "Flechas / W-S para moverte por el menú · Enter para aceptar", 22, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleCenter);
            Place(nav.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(470, -320), new Vector2(700, 40));

            // ---------- Selección de personaje ----------
            var sel = UI("PanelSeleccionPersonaje", root); Stretch(sel);
            var selTitle = Txt(sel.transform, "Titulo", "ELIGE TU PERSONAJE", 64, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(selTitle.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(1400, 90));

            Button Card(string n, string spriteName, string label, float x, out Image frame)
            {
                frame = Img(sel.transform, "Marco" + n, new Color(1, 1, 1, 0.15f));
                Place(frame.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 70), new Vector2(392, 512));
                var card = Btn(sel.transform, "Tarjeta" + n, "", 30);
                Place(card.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 70), new Vector2(370, 490));
                var cb = card.colors;
                cb.normalColor = new Color(0.08f, 0.07f, 0.2f, 0.95f);
                cb.selectedColor = new Color(0.25f, 0.18f, 0.45f, 1f);
                cb.highlightedColor = new Color(0.2f, 0.15f, 0.38f, 1f);
                card.colors = cb;
                var portrait = Img(card.transform, "Retrato", Color.white, Spr(spriteName));
                Place(portrait.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(256, 384));
                portrait.raycastTarget = false;
                var name = Txt(card.transform, "Nombre", label, 44, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                Place(name.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(360, 60));
                return card;
            }
            var ligiCard = Card("Ligi", "Ligi", "LIGI", -260, out var ligiFrame);
            var mariCard = Card("Mariale", "Mariale", "MARIALE", 260, out var mariFrame);

            var info = Txt(sel.transform, "InfoPersonaje", "", 30, Color.white, TextAnchor.UpperCenter);
            Place(info.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0, -205), new Vector2(1200, 170));
            var bStart = Btn(sel.transform, "BotonComenzar", "COMENZAR MISIÓN 1", 34);
            Place(bStart.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(190, -440), new Vector2(520, 88));
            var bBack = Btn(sel.transform, "BotonVolver", "VOLVER", 30);
            Place(bBack.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280, -440), new Vector2(300, 88));

            // ---------- Controles ----------
            var ctr = UI("PanelControles", root); Stretch(ctr);
            var ctrBox = Panel(ctr.transform, "Caja", PanelFill, PanelBorder);
            Place(ctrBox.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1150, 760));
            var ctrT = Txt(ctrBox.transform, "Titulo", "CONTROLES", 56, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(ctrT.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(1000, 80));
            var ctrBody = Txt(ctrBox.transform, "Lista",
                "<color=#7CF0FF>A / D</color>  o  flechas  ·  Moverse\n" +
                "<color=#7CF0FF>SHIFT</color>  ·  Correr\n" +
                "<color=#7CF0FF>ESPACIO / W</color>  ·  Saltar (mantén para saltar más alto)\n" +
                "<color=#7CF0FF>S / CTRL</color>  ·  Agacharse  ·  corriendo: DESLIZARSE\n" +
                "<color=#7CF0FF>E</color>  ·  Hablar / interactuar / cerrar válvulas\n" +
                "<color=#7CF0FF>E / ESPACIO</color>  ·  Avanzar diálogos\n" +
                "<color=#7CF0FF>ESC / P</color>  ·  Pausa\n\n" +
                "<size=26>Objetivo: recoge los 5 residuos, cierra las 3 fugas de agua y\nllévalos a la Estación de Reciclaje.</size>",
                34, Color.white, TextAnchor.UpperCenter);
            Place(ctrBody.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -130), new Vector2(1050, 500));
            var bCtrBack = Btn(ctr.transform, "BotonVolver", "VOLVER", 30);
            Place(bCtrBack.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -420), new Vector2(320, 84));

            // ---------- Créditos ----------
            var cre = UI("PanelCreditos", root); Stretch(cre);
            var creBox = Panel(cre.transform, "Caja", PanelFill, PanelBorder);
            Place(creBox.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1150, 700));
            var creT = Txt(creBox.transform, "Titulo", "CRÉDITOS", 56, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(creT.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(1000, 80));
            var creBody = Txt(creBox.transform, "Texto",
                "<b>Misión Marte: Sobrevive</b>\nSerious Game sobre sostenibilidad y recuperación ambiental\n\n" +
                "<color=#7CF0FF>Desarrollo y diseño</color>\nLigia Lisanny Marcano Mayorga\nMaria Alejandra Matos Bohorquez\n\n" +
                "<color=#7CF0FF>Curso</color>\nDesarrollo de Contenidos Digitales y Multimediales · Previo 2\n\n" +
                "<size=24>Hecho en Unity 6 · Arte pixel y sonidos generados por código</size>",
                32, Color.white, TextAnchor.UpperCenter);
            Place(creBody.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -130), new Vector2(1050, 520));
            var bCreBack = Btn(cre.transform, "BotonVolver", "VOLVER", 30);
            Place(bCreBack.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -400), new Vector2(320, 84));

            // ---------- Script del menú ----------
            var mm = canvas.gameObject.AddComponent<MainMenu>();
            Set(mm, "mainPanel", main); Set(mm, "selectPanel", sel); Set(mm, "controlsPanel", ctr); Set(mm, "creditsPanel", cre);
            Set(mm, "playButton", bJugar); Set(mm, "controlsButton", bControles); Set(mm, "creditsButton", bCreditos); Set(mm, "quitButton", bSalir);
            Set(mm, "ligiCard", ligiCard); Set(mm, "mariCard", mariCard); Set(mm, "ligiFrame", ligiFrame); Set(mm, "mariFrame", mariFrame);
            Set(mm, "selectInfoText", info); Set(mm, "startButton", bStart); Set(mm, "selectBackButton", bBack);
            Set(mm, "controlsBackButton", bCtrBack); Set(mm, "creditsBackButton", bCreBack);
            Set(mm, "levelSceneName", "Nivel1");

            sel.SetActive(false); ctr.SetActive(false); cre.SetActive(false);
            EditorSceneManager.SaveScene(scene, MenuScene);
        }

        // =====================================================================================
        //  ESCENA: NIVEL 1 — "BASE LIMPIA, AGUA SEGURA"
        // =====================================================================================
        static Transform world, groundRoot, itemsRoot, decoRoot, interRoot, gameplayRoot;
        const float LevelStart = -3f, LevelEnd = 142f;

        static void BuildLevelScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = MakeCamera(new Color(0.03f, 0.04f, 0.12f));
            AddGlobalLightIfURP();
            EventSystemFix();

            world = GO("Nivel1_Mundo").transform;
            groundRoot = GO("Suelo_y_Plataformas", world).transform;
            decoRoot = GO("Decoracion", world).transform;
            itemsRoot = GO("Coleccionables", world).transform;
            interRoot = GO("NPCs_e_Interactivos", world).transform;
            gameplayRoot = GO("Gameplay (checkpoints, peligros, pistas)", world).transform;

            // Cada parte se construye por separado: si una falla, el resto del nivel igual se crea y se guarda.
            GameObject player = null;
            HudRefs refs = null;
            Step("Nivel1 - Fondo", () => BuildBackground(cam));
            Step("Nivel1 - Suelo y plataformas", BuildGeometry);
            Step("Nivel1 - Decoración", BuildDecoration);
            Step("Nivel1 - Jugador", () => player = BuildPlayer(new Vector3(1f, 1.2f, 0f)));
            Step("Nivel1 - Coleccionables", BuildCollectibles);
            Step("Nivel1 - HUD", () => refs = BuildHUD());
            Step("Nivel1 - NPCs e interactivos", () => BuildInteractables(refs));
            Step("Nivel1 - Checkpoints y peligros", BuildGameplay);
            Step("Nivel1 - Cámara", () =>
            {
                var follow = cam.gameObject.AddComponent<CameraFollow2D>();
                if (player != null) Set(follow, "target", player.transform);
                Set(follow, "minX", LevelStart - 0.5f); Set(follow, "maxX", LevelEnd + 0.5f);
                Set(follow, "minY", -3f); Set(follow, "maxY", 16f);
                cam.transform.position = new Vector3(8f, 3f, -10f);
            });

            bool ok = EditorSceneManager.SaveScene(scene, LevelScene);
            if (!ok) Debug.LogError("[Misión Marte] Unity no pudo guardar " + LevelScene);
        }

        // ---------------- Fondo con parallax ----------------
        static void BuildBackground(Camera cam)
        {
            var bgRoot = GO("Fondo (Parallax)", world).transform;

            var sky = GO("Cielo_Marte", bgRoot);
            sky.transform.position = new Vector3(0.25f, 0f, 10f);
            sky.transform.localScale = Vector3.one * 1.65f;
            SR(sky, Spr("FondoCielo"), "Background", -100);
            var p1 = sky.AddComponent<Parallax>(); Set(p1, "factorX", 0.97f); Set(p1, "factorY", 1f);

            void Hills(string name, float factor, float y, float scale, Color tint, int order)
            {
                var h = GO(name, bgRoot);
                h.transform.position = new Vector3(70f * (1f - factor), y, 9f);
                var tiles = GO("Colinas", h.transform);
                tiles.transform.localScale = Vector3.one * scale;
                Tiled(tiles, Spr("Colinas"), new Vector2(260f / scale, 4f), "Background", order, tint);
                var fill = GO("Relleno", h.transform);
                fill.transform.localPosition = new Vector3(0f, -2f * scale - 10f, 0f);
                fill.transform.localScale = new Vector3(260f / 0.25f, 20f / 0.25f, 1f); // Pixel = 4px = 0.25 unidades
                SR(fill, Spr("Pixel"), "Background", order, new Color(0.478f, 0.18f, 0.165f, 1f) * tint);
                var px = h.AddComponent<Parallax>(); Set(px, "factorX", factor); Set(px, "factorY", factor * 0.9f);
            }
            Hills("Colinas_Lejanas", 0.45f, 1.0f, 1.6f, new Color(0.75f, 0.6f, 0.75f), -90);
            Hills("Colinas_Cercanas", 0.7f, -0.6f, 1.3f, Color.white, -80);
        }

        // ---------------- Suelo y plataformas ----------------
        static void Ground(string name, float x0, float x1, float top, float bottom = -8f)
        {
            float w = x1 - x0, h = top - bottom;
            var g = GO(name, groundRoot, new Vector3((x0 + x1) * 0.5f, (top + bottom) * 0.5f, 0f));
            g.layer = groundLayer;
            var col = g.AddComponent<BoxCollider2D>();
            col.size = new Vector2(w, h);
            var body = GO("Roca", g.transform);
            body.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            Tiled(body, Spr("RocaMarte"), new Vector2(w, h - 1f), "Ground", 0);
            var surf = GO("Superficie", g.transform);
            surf.transform.localPosition = new Vector3(0f, h * 0.5f - 0.5f, 0f);
            Tiled(surf, Spr("SueloMarte"), new Vector2(w, 1f), "Ground", 1);
        }

        static GameObject Platform(string name, float x0, float x1, float top, bool oneWay = true, string sprite = "PlataformaMetal")
        {
            float w = x1 - x0;
            var p = GO(name, groundRoot, new Vector3((x0 + x1) * 0.5f, top - 0.25f, 0f));
            p.layer = groundLayer;
            var col = p.AddComponent<BoxCollider2D>();
            col.size = new Vector2(w, 0.5f);
            Tiled(p, Spr(sprite), new Vector2(w, 0.5f), "Ground", 2);
            if (oneWay)
            {
                col.usedByEffector = true;
                var eff = p.AddComponent<PlatformEffector2D>();
                eff.useOneWay = true; eff.surfaceArc = 160f; eff.useSideFriction = false;
            }
            return p;
        }

        static void Block(string name, float x0, float x1, float y0, float y1)
        {
            float w = x1 - x0, h = y1 - y0;
            var b = GO(name, groundRoot, new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f));
            b.layer = groundLayer;
            b.AddComponent<BoxCollider2D>().size = new Vector2(w, h);
            Tiled(b, Spr("RocaMarte"), new Vector2(w, h), "Ground", 0, new Color(0.85f, 0.8f, 0.8f));
            // tubería que marca el "techo" del túnel
            var pipe = GO("Tuberia_Baja", b.transform);
            pipe.transform.localPosition = new Vector3(0f, -h * 0.5f + 0.25f, 0f);
            Tiled(pipe, Spr("PlataformaMetal"), new Vector2(w, 0.5f), "Ground", 3);
        }

        static void BuildGeometry()
        {
            // Zona A — Base marciana (inicio) y tutorial de salto
            Ground("Suelo_A_Base", LevelStart, 40f, 0f);
            Platform("Plataforma_Salto_1", 26.5f, 30.5f, 2.0f);
            Ground("Escalon_Roca", 33f, 36f, 1.2f, 0f);
            // vacío 40-44
            // Zona B — Fuga 1 y túnel (deslizarse)
            Ground("Suelo_B_Tunel", 44f, 64f, 0f);
            Block("Tunel_Techo (deslizate)", 56f, 62f, 0.95f, 4.6f);
            // Zona C — Charco tóxico y plataforma móvil
            Ground("Suelo_C_Toxico", 64f, 74f, 0f);
            Platform("Plataforma_Sobre_Toxico", 67.8f, 70.2f, 1.8f);
            Platform("Plataforma_Alta_Bolsa", 70.6f, 72.6f, 3.4f);
            // vacío 74-82 con plataforma móvil
            var mp = Platform("Plataforma_Movil", 74.3f, 76.8f, 0f, false);
            var rb = mp.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var mov = mp.AddComponent<MovingPlatform>();
            Set(mov, "travel", new Vector2(4.9f, 0f)); Set(mov, "speed", 2.4f); Set(mov, "waitTime", 0.6f);
            // Zona D — Terminal y puente de energía
            Ground("Suelo_D_Terminal", 82f, 92f, 0f);
            // vacío 92-100 (puente que activa la terminal)
            Ground("Suelo_E", 100f, 104f, 0f);
            // Zona E — Subida a la meseta
            Ground("Escalon_1", 104f, 108f, 1.5f);
            Ground("Escalon_2", 108f, 112f, 3f);
            Ground("Meseta", 112f, 124f, 4f);
            Platform("Plataforma_Chip", 118.8f, 122.2f, 6.2f);
            // Zona F — Estación de reciclaje (final)
            Ground("Suelo_F_Estacion", 124f, LevelEnd, 0f);

            // Paredes invisibles (no se sale del nivel)
            void Wall(string n, float x)
            {
                var w = GO(n, groundRoot, new Vector3(x, 10f, 0f));
                w.AddComponent<BoxCollider2D>().size = new Vector2(1f, 40f);
            }
            Wall("Pared_Invisible_Izq", LevelStart - 0.5f);
            Wall("Pared_Invisible_Der", LevelEnd + 0.5f);
        }

        static void Deco(string sprite, float x, float groundY, float scale, int order = -10, Color? tint = null)
        {
            var s = Spr(sprite);
            if (s == null) return;
            var d = GO("Deco_" + sprite, decoRoot);
            float h = s.bounds.size.y * scale;
            d.transform.position = new Vector3(x, groundY + h * 0.5f - 0.05f, 0f);
            d.transform.localScale = Vector3.one * scale;
            SR(d, s, "Default", order, tint);
        }

        static void BuildDecoration()
        {
            Deco("Domo", 3.5f, 0f, 1.6f, -12);
            Deco("Antena", -1.2f, 0f, 1.2f);
            Deco("TanqueAgua", 9.5f, 0f, 1.5f);
            Deco("Rocas", 18f, 0f, 1.5f);
            Deco("Rocas", 37.5f, 1.2f, 1.2f);
            Deco("TanqueAgua", 46.5f, 0f, 1.3f);
            Deco("Rocas", 64.8f, 0f, 1.3f);
            Deco("Antena", 84f, 0f, 1.1f);
            Deco("Rocas", 102.5f, 0f, 1.2f);
            Deco("Rocas", 113.5f, 4f, 1.4f);
            Deco("TanqueAgua", 123f, 4f, 1.3f);
            Deco("Domo", 139.6f, 0f, 1.6f, -12);
            Deco("Antena", 126f, 0f, 1.2f);
        }

        // ---------------- Jugador ----------------
        static GameObject BuildPlayer(Vector3 pos)
        {
            var p = GO("Player", null, pos);
            p.tag = "Player";
            var rb = p.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.gravityScale = 3f;
            p.AddComponent<BoxCollider2D>();
            var vis = GO("Visual", p.transform);
            var sr = SR(vis, Spr("Ligi"), "Characters", 10);
            var pc = p.AddComponent<PlayerController>();
            Set(pc, "visual", vis.transform);
            Set(pc, "spriteRenderer", sr);
            Set(pc, "ligiSprite", Spr("Ligi"));
            Set(pc, "mariSprite", Spr("Mariale"));
            return p;
        }

        // ---------------- Coleccionables ----------------
        static void Item(CollectibleType type, string itemName, string sprite, Vector2 pos, float scale, string fact = "", bool spin = false)
        {
            var go = GO((type == CollectibleType.Residuo ? "Residuo_" : type == CollectibleType.Gota ? "Gota_" : "Tuerca_") + itemName.Replace(" ", "_"), itemsRoot, pos);
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true; col.radius = type == CollectibleType.Residuo ? 0.55f : 0.4f;
            Visual(go.transform, sprite, Vector3.zero, scale, "Items", type == CollectibleType.Residuo ? 6 : 5);
            var ci = go.AddComponent<CollectibleItem>();
            Set(ci, "type", (int)type);
            Set(ci, "itemName", itemName);
            Set(ci, "educationalFact", fact);
            Set(ci, "spin", spin);
            if (type == CollectibleType.Residuo)
            {
                // brillo para que se note que es importante
                var glow = GO("Brillo", go.transform);
                glow.transform.localScale = Vector3.one * 2.6f;
                SR(glow, Spr("Pixel"), "Items", 4, new Color(0.4f, 1f, 0.6f, 0.18f));
            }
        }

        static void Nuts(params Vector2[] pts) { foreach (var p in pts) Item(CollectibleType.Tuerca, "Tuerca", "Tuerca", p, 0.85f, "", true); }
        static Vector2[] Row(float x0, float y, int n, float dx = 1f) { var r = new Vector2[n]; for (int i = 0; i < n; i++) r[i] = new Vector2(x0 + i * dx, y); return r; }
        static Vector2[] Arc(float x0, float y0, int n, float dx, float hgt) { var r = new Vector2[n]; for (int i = 0; i < n; i++) { float t = n == 1 ? 0.5f : i / (float)(n - 1); r[i] = new Vector2(x0 + i * dx, y0 + Mathf.Sin(t * Mathf.PI) * hgt); } return r; }

        static void BuildCollectibles()
        {
            // 5 RESIDUOS (objetivo de la misión) — cada uno enseña algo
            Item(CollectibleType.Residuo, "Botella de plástico", "Botella", new Vector2(28.5f, 3.0f), 1.4f,
                "El plástico puede tardar cientos de años en degradarse. Va en la caneca BLANCA (aprovechables).");
            Item(CollectibleType.Residuo, "Lata de aluminio", "Lata", new Vector2(59f, 0.55f), 1.4f,
                "El aluminio se puede reciclar una y otra vez sin perder calidad. Caneca BLANCA.");
            Item(CollectibleType.Residuo, "Bolsa plástica", "Bolsa", new Vector2(71.6f, 4.6f), 1.3f,
                "Las bolsas que llegan a ríos y mares se rompen en microplásticos que contaminan el agua.");
            Item(CollectibleType.Residuo, "Pila usada", "Bateria", new Vector2(102f, 0.8f), 1.4f,
                "Las pilas tienen metales pesados: NUNCA a la basura común, van a puntos de recolección especial.");
            Item(CollectibleType.Residuo, "Chip electrónico", "Chip", new Vector2(120.5f, 7.2f), 1.3f,
                "Los residuos electrónicos (RAEE) tienen metales valiosos que se recuperan si se reciclan bien.");

            // GOTAS de agua (suben la reserva de agua)
            foreach (var p in new[] { new Vector2(47.5f, 1f), new Vector2(48.5f, 1f), new Vector2(68.9f, 2.6f), new Vector2(87.5f, 1f),
                                      new Vector2(96f, 1.8f), new Vector2(114f, 5f), new Vector2(115f, 5f), new Vector2(130f, 1f) })
                Item(CollectibleType.Gota, "Gota de agua", "Gota", p, 0.9f);

            // TUERCAS (puntaje; cada 10 dan turbo)
            Nuts(Row(13f, 1f, 4));
            Nuts(Arc(22f, 1f, 5, 1f, 1.6f));
            Nuts(Row(33.5f, 2.2f, 3));
            Nuts(Arc(40.2f, 1.5f, 4, 1.15f, 1.5f));
            Nuts(Row(51.5f, 1f, 3));
            Nuts(Row(57.2f, 0.5f, 2, 3.4f));
            Nuts(Arc(65.5f, 1.2f, 3, 1f, 1f));
            Nuts(Arc(76f, 1.6f, 5, 1.1f, 1.2f));
            Nuts(Arc(93f, 1f, 6, 1.2f, 1.5f));
            Nuts(Row(105f, 2.5f, 2)); Nuts(Row(109f, 4f, 2));
            Nuts(Row(116.5f, 5f, 3));
            Nuts(Arc(124.5f, 4.5f, 4, 0.9f, 0.6f));
        }

        // ---------------- HUD / Canvas ----------------
        class HudRefs { public Canvas canvas; public GameHUDManager hud; }

        static HudRefs BuildHUD()
        {
            var canvas = MakeCanvas("Canvas_HUD");
            var root = canvas.transform;

            // --- Panel de misión (arriba izquierda) ---
            var mision = Panel(root, "Panel_Mision", PanelFill, PanelBorder, 3f);
            Place(mision.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(660, 250));
            var mt = Txt(mision.transform, "MisionTitulo", "MISIÓN 1", 26, Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            Place(mt.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -14), new Vector2(620, 36));
            var obj = Txt(mision.transform, "ObjectiveText", "Objetivo:", 24, Color.white, TextAnchor.UpperLeft);
            Place(obj.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -52), new Vector2(620, 64));
            var resT = Txt(mision.transform, "CounterText_Residuos", "Residuos: 0 / 5", 26, new Color(0.55f, 1f, 0.65f), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(resT.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -122), new Vector2(260, 50));
            var slots = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                var bg = Img(mision.transform, "Slot_" + (i + 1), new Color(0, 0, 0, 0.55f));
                Place(bg.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(290 + i * 70, -118), new Vector2(60, 60));
                var icon = Img(bg.transform, "Icono", new Color(1, 1, 1, 0.15f), Spr("Botella"));
                Stretch(icon.gameObject, 8, 8, 8, 8);
                slots[i] = icon;
            }
            var fugT = Txt(mision.transform, "Fugas", "Fugas cerradas: 0 / 3", 24, new Color(0.55f, 0.85f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(fugT.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -188), new Vector2(330, 44));
            var tueIcon = Img(mision.transform, "IconoTuerca", Color.white, Spr("Tuerca"));
            Place(tueIcon.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(380, -192), new Vector2(36, 36));
            var tueT = Txt(mision.transform, "Tuercas", "Tuercas: 0", 24, new Color(0.85f, 0.88f, 0.95f), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(tueT.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(424, -188), new Vector2(220, 44));

            // --- Barra de agua (arriba centro) ---
            var agua = Panel(root, "Panel_Agua", PanelFill, new Color(0.3f, 0.65f, 1f, 0.9f), 3f);
            Place(agua.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(60, -20), new Vector2(560, 92));
            var gIcon = Img(agua.transform, "IconoGota", Color.white, Spr("Gota"));
            Place(gIcon.gameObject, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(44, 60));
            var aguaT = Txt(agua.transform, "AguaTexto", "Reserva de agua: 100 %", 24, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            Place(aguaT.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(72, -10), new Vector2(470, 34));
            var barBg = Img(agua.transform, "BarraFondo", new Color(0, 0, 0, 0.7f));
            Place(barBg.gameObject, new Vector2(0, 0), new Vector2(0, 0), new Vector2(72, 14), new Vector2(468, 26));
            var barFill = Img(barBg.transform, "BarraRelleno", new Color(0.25f, 0.65f, 1f));
            var fillRt = barFill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one; fillRt.pivot = new Vector2(0, 0.5f);
            fillRt.offsetMin = new Vector2(3, 3); fillRt.offsetMax = new Vector2(-3, -3);

            // --- Personaje (arriba derecha) ---
            var hero = Panel(root, "Panel_Personaje", PanelFill, PanelBorder, 3f);
            Place(hero.gameObject, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(330, 120));
            var portrait = Img(hero.transform, "Retrato", Color.white, Spr("Ligi"));
            Place(portrait.gameObject, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(64, 96));
            var heroT = Txt(hero.transform, "NombrePersonaje", "LIGI", 30, Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(heroT.gameObject, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(92, 0), new Vector2(230, 100));

            // --- Ayuda de controles (abajo) ---
            var help = Txt(root, "AyudaControles", "A/D mover · SHIFT correr · ESPACIO saltar · S deslizar · E interactuar · ESC pausa", 22, new Color(1, 1, 1, 0.75f), TextAnchor.MiddleCenter);
            Place(help.gameObject, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(1600, 36));

            // --- Mensajes emergentes (toast) ---
            var toast = Panel(root, "Panel_Mensaje", new Color(0.04f, 0.03f, 0.1f, 0.85f), new Color(1f, 0.8f, 0.3f, 0.8f), 3f);
            Place(toast.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -290), new Vector2(1150, 130));
            var toastT = Txt(toast.transform, "MensajeTexto", "", 28, Color.white, TextAnchor.MiddleCenter);
            Stretch(toastT.gameObject, 20, 20, 8, 8);

            // --- Panel de diálogo (abajo) — nombres como en la guía de la profe ---
            var dlg = Panel(root, "DialoguePanel", new Color(0.05f, 0.04f, 0.13f, 0.95f), PanelBorder, 4f);
            var dRt = dlg.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0, 0); dRt.anchorMax = new Vector2(1, 0); dRt.pivot = new Vector2(0.5f, 0);
            dRt.offsetMin = new Vector2(160, 50); dRt.offsetMax = new Vector2(-160, 300);
            var nameBox = Img(dlg.transform, "CajaNombre", new Color(0.12f, 0.08f, 0.28f, 1f));
            Place(nameBox.gameObject, new Vector2(0, 1), new Vector2(0, 0), new Vector2(30, -6), new Vector2(420, 56));
            var nameT = Txt(nameBox.transform, "NPCNameText", "Nombre", 32, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(nameT.gameObject);
            var contT = Txt(dlg.transform, "DialogueContentText", "", 32, Color.white, TextAnchor.UpperLeft);
            Stretch(contT.gameObject, 40, 40, 50, 30);
            var hintT = Txt(dlg.transform, "IndicadorContinuar", "E / Espacio  >>  continuar", 22, Cyan, TextAnchor.LowerRight);
            Stretch(hintT.gameObject, 30, 30, 14, 14);

            // --- Panel de información (objetos interactivos) ---
            var infoP = Panel(root, "InfoDisplayPanel", new Color(0.05f, 0.04f, 0.13f, 0.96f), new Color(0.55f, 1f, 0.65f, 0.95f), 4f);
            Place(infoP.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(1100, 600));
            var infoTitle = Txt(infoP.transform, "InfoTitulo", "Título", 40, new Color(0.55f, 1f, 0.65f), TextAnchor.UpperCenter, FontStyle.Bold);
            Place(infoTitle.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(1000, 60));
            var infoBody = Txt(infoP.transform, "InfoTextComponent", "", 29, Color.white, TextAnchor.UpperLeft);
            Place(infoBody.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -105), new Vector2(1000, 420));
            var infoHint = Txt(infoP.transform, "InfoCerrar", "Presiona E para cerrar", 22, Cyan, TextAnchor.LowerCenter);
            Stretch(infoHint.gameObject, 20, 20, 18, 18);

            // --- Panel "objetivo cumplido" (5/5) ---
            var modal = Panel(root, "MissionCompletePanel", new Color(0.05f, 0.1f, 0.08f, 0.97f), new Color(0.55f, 1f, 0.65f, 1f), 5f);
            Place(modal.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1150, 440));
            var modT = Txt(modal.transform, "Titulo", "", 46, new Color(0.55f, 1f, 0.65f), TextAnchor.UpperCenter, FontStyle.Bold);
            Place(modT.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(1080, 70));
            var modB = Txt(modal.transform, "Texto", "", 30, Color.white, TextAnchor.UpperCenter);
            Place(modB.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(1060, 200));
            var modBtn = Btn(modal.transform, "BotonContinuar", "CONTINUAR (E)", 30);
            Place(modBtn.gameObject, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(420, 80));

            // --- Panel de nivel completado ---
            var done = Img(root, "LevelCompletePanel", new Color(0.01f, 0.01f, 0.05f, 0.82f));
            Stretch(done.gameObject);
            var doneBox = Panel(done.transform, "Caja", new Color(0.05f, 0.04f, 0.13f, 0.98f), Gold, 5f);
            Place(doneBox.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1150, 860));
            var doneT = Txt(doneBox.transform, "Titulo", "¡MISIÓN 1 COMPLETADA!", 60, Gold, TextAnchor.UpperCenter, FontStyle.Bold);
            Place(doneT.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(1080, 80));
            var stars = Txt(doneBox.transform, "Estrellas", "", 32, Cyan, TextAnchor.UpperCenter, FontStyle.Bold);
            Place(stars.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -115), new Vector2(1080, 50));
            var stats = Txt(doneBox.transform, "Estadisticas", "", 30, Color.white, TextAnchor.UpperCenter);
            Place(stats.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -180), new Vector2(1080, 440));
            var next = Txt(doneBox.transform, "Proximamente", "Próximamente: Misión 2 — Restaurar ecosistemas", 26, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleCenter, FontStyle.Italic);
            Place(next.gameObject, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 140), new Vector2(1080, 40));
            var bRetry = Btn(doneBox.transform, "BotonReintentar", "JUGAR DE NUEVO (R)", 28);
            Place(bRetry.gameObject, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-250, 40), new Vector2(440, 84));
            var bMenu = Btn(doneBox.transform, "BotonMenu", "MENÚ PRINCIPAL (M)", 28);
            Place(bMenu.gameObject, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(250, 40), new Vector2(440, 84));

            // --- Menú de pausa ---
            var pause = Img(root, "PausePanel", new Color(0.01f, 0.01f, 0.05f, 0.8f));
            Stretch(pause.gameObject);
            var pBox = Panel(pause.transform, "Caja", new Color(0.05f, 0.04f, 0.13f, 0.98f), PanelBorder, 5f);
            Place(pBox.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 780));
            var pT = Txt(pBox.transform, "Titulo", "PAUSA", 64, Gold, TextAnchor.UpperCenter, FontStyle.Bold);
            Place(pT.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(700, 90));
            Button PB(string n, string l, float y)
            {
                var b = Btn(pBox.transform, n, l, 32);
                Place(b.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(480, 84));
                return b;
            }
            var bRes = PB("BotonReanudar", "REANUDAR", -150);
            var bRst = PB("BotonReiniciar", "REINICIAR NIVEL", -250);
            var bMen = PB("BotonMenuPrincipal", "MENÚ PRINCIPAL", -350);
            var bQuit = PB("BotonSalir", "SALIR DEL JUEGO", -450);
            var pHelp = Txt(pBox.transform, "Controles", "A/D mover · SHIFT correr · ESPACIO saltar\nS deslizar/agacharse · E interactuar · ESC reanudar", 22, Cyan, TextAnchor.LowerCenter);
            Stretch(pHelp.gameObject, 20, 20, 30, 30);

            // ---------- Scripts ----------
            var hudGo = canvas.gameObject;
            var hud = hudGo.AddComponent<GameHUDManager>();
            Set(hud, "missionTitleText", mt); Set(hud, "objectiveText", obj); Set(hud, "residuosText", resT);
            Set(hud, "fugasText", fugT); Set(hud, "tuercasText", tueT); Set(hud, "aguaText", aguaT); Set(hud, "heroNameText", heroT);
            Set(hud, "aguaFill", fillRt); Set(hud, "aguaFillImage", barFill);
            SetArray(hud, "residuoSlots", slots.Cast<Object>().ToArray());
            Set(hud, "heroPortrait", portrait);
            Set(hud, "toastPanel", toast.gameObject); Set(hud, "toastText", toastT);
            Set(hud, "modalPanel", modal.gameObject); Set(hud, "modalTitle", modT); Set(hud, "modalBody", modB); Set(hud, "modalButton", modBtn);
            Set(hud, "levelCompletePanel", done.gameObject); Set(hud, "levelStatsText", stats); Set(hud, "starsText", stars);
            Set(hud, "retryButton", bRetry); Set(hud, "menuButton", bMenu);
            Set(hud, "pixelSprite", Spr("Pixel")); Set(hud, "ligiSprite", Spr("Ligi")); Set(hud, "mariSprite", Spr("Mariale"));
            Set(hud, "menuSceneName", "Menu");

            var dui = hudGo.AddComponent<DialogueUI>();
            Set(dui, "dialoguePanel", dlg.gameObject); Set(dui, "nameText", nameT); Set(dui, "contentText", contT); Set(dui, "hintText", hintT);

            var iui = hudGo.AddComponent<InfoPanelUI>();
            Set(iui, "panel", infoP.gameObject); Set(iui, "titleText", infoTitle); Set(iui, "bodyText", infoBody);

            var pm = hudGo.AddComponent<PauseMenu>();
            Set(pm, "pausePanel", pause.gameObject); Set(pm, "resumeButton", bRes); Set(pm, "restartButton", bRst);
            Set(pm, "menuButton", bMen); Set(pm, "quitButton", bQuit); Set(pm, "menuSceneName", "Menu");

            // Paneles ocultos al iniciar
            toast.gameObject.SetActive(false); dlg.gameObject.SetActive(false); infoP.gameObject.SetActive(false);
            modal.gameObject.SetActive(false); done.gameObject.SetActive(false); pause.gameObject.SetActive(false);

            return new HudRefs { canvas = canvas, hud = hud };
        }

        // ---------------- NPCs, objetos interactivos y fugas ----------------
        static readonly Color CmdColor = new Color(0.55f, 1f, 0.65f);
        static readonly Color HeroColor = new Color(1f, 0.8f, 0.3f);
        static readonly Color CompColor = new Color(0.8f, 0.6f, 1f);

        static NPCDialogue NPC(string goName, string displayName, string sprite, float x, float groundY, bool companion)
        {
            var go = GO(goName, interRoot, new Vector3(x, groundY + 0.75f, 0f));
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true; col.size = new Vector2(3.6f, 2.6f);
            var vis = GO("Visual", go.transform);
            var sr = SR(vis, Spr(sprite), "Characters", 8);
            var prompt = Prompt(go.transform, 1.45f);
            var npc = go.AddComponent<NPCDialogue>();
            Set(npc, "npcName", displayName);
            Set(npc, "isCompanion", companion);
            Set(npc, "spriteRenderer", sr);
            Set(npc, "ligiSprite", Spr("Ligi"));
            Set(npc, "mariSprite", Spr("Mariale"));
            Set(npc, "interactionPrompt", prompt);
            return npc;
        }

        static InteractableObject Interactive(string goName, string sprite, float x, float groundY, float scale, string title, string content,
                                              GameObject[] activate = null, string usedSprite = null, string activationMsg = "")
        {
            var s = Spr(sprite);
            float h = s != null ? s.bounds.size.y * scale : 1.5f;
            var go = GO(goName, interRoot, new Vector3(x, groundY + h * 0.5f, 0f));
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true; col.size = new Vector2(Mathf.Max(2.6f, h), Mathf.Max(2.4f, h + 0.6f));
            var vis = Visual(go.transform, sprite, Vector3.zero, scale, "Items", 3);
            var prompt = Prompt(go.transform, h * 0.5f + 0.6f);
            var io = go.AddComponent<InteractableObject>();
            Set(io, "promptFloating", prompt);
            Set(io, "title", title);
            Set(io, "educationalContent", content);
            if (activate != null) SetArray(io, "activateOnUse", activate.Cast<Object>().ToArray());
            Set(io, "stateRenderer", vis.GetComponent<SpriteRenderer>());
            if (usedSprite != null) Set(io, "usedSprite", Spr(usedSprite));
            Set(io, "activationMessage", activationMsg);
            return io;
        }

        static void Leak(int n, float x, float groundY, string fact)
        {
            var go = GO("Fuga_de_Agua_" + n, interRoot, new Vector3(x, groundY + 0.7f, 0f));
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true; col.size = new Vector2(3f, 2.4f);
            var vis = Visual(go.transform, "TuberiaFuga", Vector3.zero, 1.4f, "Items", 3);
            var sprayGo = GO("Chorro", go.transform);
            sprayGo.transform.localPosition = new Vector3(0.68f, -0.02f, 0f);
            var spray = sprayGo.AddComponent<WaterSpray>();
            Set(spray, "dropSprite", Spr("Pixel"));
            var prompt = Prompt(go.transform, 1.5f);
            var wl = go.AddComponent<WaterLeak>();
            Set(wl, "promptFloating", prompt);
            Set(wl, "valveRenderer", vis.GetComponent<SpriteRenderer>());
            Set(wl, "closedSprite", Spr("TuberiaOk"));
            Set(wl, "spray", spray);
            Set(wl, "fact", fact);
        }

        static void BuildInteractables(HudRefs refs)
        {
            // --- NPC 1: Comandante Orión (inicio) ---
            var cmd = NPC("NPC_Guia_Comandante_Orion", "Comandante Orión", "Comandante", 7.5f, 0f, false);
            SetLines(cmd, "introLines",
                new L("Comandante Orión", CmdColor, "¡{HEROE}! Qué bueno que llegas. La tormenta de polvo dejó la base marciana hecha un desastre."),
                new L("{HEROE} (Tú)", HeroColor, "Vi basura regada por todo el cráter... y escuché agua goteando."),
                new L("Comandante Orión", CmdColor, "Exacto. Hay 5 residuos tirados y 3 tuberías con fugas. Cada gota que perdemos es agua que no podremos llevar a la Tierra."),
                new L("Comandante Orión", CmdColor, "Tu misión: recoge los 5 residuos y cierra las 3 fugas con la tecla E. Cuando termines, lleva todo a {COMPANERA} en la Estación de Reciclaje, al final del cráter."),
                new L("{HEROE} (Tú)", HeroColor, "¡Entendido! Base limpia, agua segura."),
                new L("Comandante Orión", CmdColor, "Usa ESPACIO para saltar y S mientras corres para deslizarte bajo las tuberías bajas. Lee el cartel de al lado antes de salir. ¡Suerte!"));
            SetLines(cmd, "reminderLines",
                new L("Comandante Orión", CmdColor, "Todavía faltan {FALTAN} residuos y {FUGAS} fugas por cerrar."),
                new L("Comandante Orión", CmdColor, "Pista: busca arriba de las plataformas, dentro del túnel y en la meseta alta."));
            SetLines(cmd, "completeLines",
                new L("Comandante Orión", CmdColor, "¡Excelente trabajo, {HEROE}! La base ya no pierde agua."),
                new L("Comandante Orión", CmdColor, "Ve con {COMPANERA} a la Estación de Reciclaje para clasificar los residuos y cerrar la misión."));

            // --- NPC 2: la compañera (Ligi o Mariale, la que NO eligió el jugador) ---
            var comp = NPC("NPC_Companera (Ligi o Mariale)", "Mariale", "Mariale", 131f, 0f, true);
            Set(comp, "completesLevel", true);
            SetLines(comp, "introLines",
                new L("{COMPANERA}", CompColor, "¡{HEROE}! Ya tengo lista la Estación de Reciclaje."),
                new L("{COMPANERA}", CompColor, "Pero aún te faltan {FALTAN} residuos y {FUGAS} fugas por cerrar. ¡Vuelve cuando tengas todo!"));
            SetLines(comp, "completeLines",
                new L("{COMPANERA}", CompColor, "¡Lo lograste, {HEROE}! Clasifiquemos: la botella, la lata y la bolsa van a la caneca BLANCA de aprovechables."),
                new L("{HEROE} (Tú)", HeroColor, "¿Y la pila y el chip? ¿También a la blanca?"),
                new L("{COMPANERA}", CompColor, "No. Son residuos peligrosos y electrónicos: van a un punto de recolección especial. Nunca a la basura común."),
                new L("{COMPANERA}", CompColor, "Y como cerraste las fugas, la reserva de agua está a salvo. Ahora la base puede empezar a purificar agua para la Tierra."),
                new L("{HEROE} (Tú)", HeroColor, "¡Misión 1 completada! Siguiente parada: restaurar los ecosistemas."));

            // --- Objeto interactivo 1: Cartel educativo ---
            Interactive("Objeto_Interactivo_Cartel", "Cartel", 12.5f, 0f, 1.6f,
                "¿POR QUÉ RECOGER Y SEPARAR LA BASURA?",
                "Los residuos mal dispuestos contaminan el suelo y el AGUA que necesitamos para vivir.\n\n" +
                "En Colombia se separa en 3 canecas:\n" +
                "<color=#FFFFFF><b>BLANCA</b></color>  · aprovechables: plástico, vidrio, metal, papel y cartón limpios.\n" +
                "<color=#6CF07A><b>VERDE</b></color>  · orgánicos aprovechables: restos de comida y jardín.\n" +
                "<color=#AAAAAA><b>NEGRA</b></color>  · no aprovechables: papel higiénico, servilletas, empaques sucios.\n\n" +
                "Las pilas y los aparatos electrónicos NO van en ninguna caneca: se llevan a puntos de recolección especial.");

            // --- Objeto interactivo 2: Terminal que activa el puente de energía ---
            var bridge = Platform("Puente_de_Energia (lo activa la terminal)", 91.85f, 100.15f, 0f, false, "PuenteEnergia");
            bridge.SetActive(false);
            Interactive("Objeto_Interactivo_Terminal", "TerminalOff", 88.8f, 0f, 1.5f,
                "TERMINAL DE ENERGÍA SOLAR",
                "La base funciona con paneles solares: energía limpia que no contamina el aire ni el agua.\n\n" +
                "Bombear y purificar agua gasta mucha energía. Por eso, ahorrar agua también es ahorrar energía.\n\n" +
                "<color=#7CF0FF>Al cerrar esta ventana se activará el PUENTE DE ENERGÍA para cruzar el cráter.</color>",
                new[] { bridge }, "TerminalOn", "¡Puente de energía activado! Ya puedes cruzar.");

            // --- Objeto interactivo 3: Estación de reciclaje (final) ---
            Interactive("Objeto_Interactivo_Estacion_Reciclaje", "EstacionReciclaje", 135f, 0f, 1.25f,
                "ESTACIÓN DE RECICLAJE",
                "Reciclar convierte residuos en materia prima nueva y evita que terminen en ríos y mares.\n\n" +
                "Las 3 R:\n<b>Reducir</b>: consumir menos y evitar desechables.\n<b>Reutilizar</b>: darle otra vida a los objetos.\n<b>Reciclar</b>: separar bien para que se aprovechen.\n\n" +
                "Habla con {COMPANERA} para entregar los residuos.");

            // --- Fugas de agua (mecánica de ahorro de agua) ---
            Leak(1, 50.5f, 0f, "Una llave que gotea puede desperdiciar decenas de litros de agua al día. ¡Repara las fugas!");
            Leak(2, 85.6f, 0f, "Cerrar la llave mientras te cepillas o te enjabonas ahorra muchos litros de agua cada día.");
            Leak(3, 117.2f, 4f, "Solo una pequeña parte del agua del planeta es dulce y accesible. ¡Cada gota cuenta!");
        }

        // ---------------- Checkpoints, peligros y pistas ----------------
        static void Checkpoint(int n, float x, float groundY)
        {
            var go = GO("Checkpoint_" + n, gameplayRoot, new Vector3(x, groundY + 0.9f, 0f));
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true; col.size = new Vector2(1.2f, 3f);
            var vis = Visual(go.transform, "BalizaOff", Vector3.zero, 1.2f, "Items", 2);
            var cp = go.AddComponent<global::MisionMarte.Checkpoint>();
            Set(cp, "beaconRenderer", vis.GetComponent<SpriteRenderer>());
            Set(cp, "offSprite", Spr("BalizaOff"));
            Set(cp, "onSprite", Spr("BalizaOn"));
        }

        static void HazardZone(string name, float x0, float x1, float y0, float y1, string msg, bool toxicVisual)
        {
            var go = GO(name, gameplayRoot, new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f));
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true; col.size = new Vector2(x1 - x0, y1 - y0);
            if (toxicVisual) Tiled(go, Spr("Toxico"), new Vector2(x1 - x0, y1 - y0), "Ground", 4);
            var hz = go.AddComponent<Hazard>();
            Set(hz, "message", msg);
        }

        static void Hint(float x, string text)
        {
            var go = GO("Pista_" + Mathf.RoundToInt(x), gameplayRoot, new Vector3(x, 6f, 0f));
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true; col.size = new Vector2(1f, 24f);
            var h = go.AddComponent<HintTrigger>();
            Set(h, "hint", text);
            Set(h, "duration", 4.5f);
        }

        static void BuildGameplay()
        {
            Checkpoint(1, 0.2f, 0f);
            Checkpoint(2, 45.3f, 0f);
            Checkpoint(3, 83.2f, 0f);
            Checkpoint(4, 105f, 1.5f);
            Checkpoint(5, 125.2f, 0f);

            // Zona de caída (PARCHE ANTI-CAÍDAS): cubre todo el nivel por debajo; si cae, reaparece en el último punto de control.
            HazardZone("Zona_de_Caida (respawn)", LevelStart - 10f, LevelEnd + 10f, -26f, -4.5f, "¡Cuidado! Volviste al último punto de control.", false);
            HazardZone("Charco_Toxico", 66f, 72.4f, 0f, 0.45f, "¡Charco tóxico! Salta por las plataformas.", true);

            // Celda de energía (turbo)
            var cell = GO("Celda_Energia_Turbo", gameplayRoot, new Vector3(31.5f, 1f, 0f));
            var cc = cell.AddComponent<CircleCollider2D>(); cc.isTrigger = true; cc.radius = 0.5f;
            Visual(cell.transform, "CeldaEnergia", Vector3.zero, 1.2f, "Items", 6);
            cell.AddComponent<SpeedBoost>();
            var cell2 = GO("Celda_Energia_Turbo_2", gameplayRoot, new Vector3(126.5f, 1f, 0f));
            var cc2 = cell2.AddComponent<CircleCollider2D>(); cc2.isTrigger = true; cc2.radius = 0.5f;
            Visual(cell2.transform, "CeldaEnergia", Vector3.zero, 1.2f, "Items", 6);
            cell2.AddComponent<SpeedBoost>();

            Hint(3f, "Usa A / D para moverte y SHIFT para correr. Habla con el Comandante con E.");
            Hint(21f, "ESPACIO o W para SALTAR. Mantén presionado para saltar más alto.");
            Hint(38.5f, "¡Vacío adelante! Si caes, vuelves al último punto de control (baliza).");
            Hint(48f, "Una FUGA de agua: acércate y presiona E para cerrar la válvula.");
            Hint(53.5f, "Tubería baja: corre y presiona S (o CTRL) para DESLIZARTE por debajo.");
            Hint(64.5f, "Charco tóxico: no lo pises. Salta por las plataformas.");
            Hint(73.5f, "Espera la plataforma móvil y súbete para cruzar.");
            Hint(87f, "No hay paso... Lee la TERMINAL (E) para activar el puente de energía.");
            Hint(104f, "Sube a la meseta: hay otra fuga y un residuo electrónico allá arriba.");
            Hint(127f, "¡La Estación de Reciclaje! Habla con {COMPANERA} para terminar la misión.");
        }
    }
}
