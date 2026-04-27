// Assets/Scripts/UI/CategoryFolderButton.cs
//
// Hides the prefab's FilterBar entirely and builds its own bottom-screen
// "Categories" toggle plus a panel of 10 custom buttons in code. No reliance
// on prefab Button onClick wiring or Inspector hookups — every button is
// constructed at runtime with an explicit color, an explicit click listener,
// and an explicit label.
//
// Behavior:
//   • Tap the bottom "≡ Categories" button → panel of 10 buttons fades in.
//   • Tap one (e.g. "Halogens")            → button turns BRIGHT RED, the
//     6 halogen cubes in the placed table tint + pop forward, others dim.
//   • Tap the same button again            → it returns to dark red, table
//     resets to normal.
//   • Tap a different button while one is active → previous goes dark, new
//     goes bright. Only one category active at a time.
//   • Tap "≡ Close"                        → panel hides, active state kept.
//
// Self-bootstrapping. Delete this file to remove.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PeriodicAR.AR;

namespace PeriodicAR.UI
{
    public class CategoryFolderButton : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<CategoryFolderButton>() != null) return;

            var go = new GameObject("[CategoryFolderButton]");
            DontDestroyOnLoad(go);
            go.AddComponent<CategoryFolderButton>();
        }

        // ---------- Categories ----------------------------------------------------
        // (highlighter key, on-screen label)
        private static readonly (string key, string label)[] s_Categories =
        {
            ("alkali",         "Alkali Metals"      ),
            ("alkaline",       "Alkaline Earth"     ),
            ("transition",     "Transition Metals"  ),
            ("posttransition", "Post-Trans Metals"  ),
            ("lanthanoid",     "Lanthanoids"        ),
            ("actinoid",       "Actinoids"          ),
            ("metalloid",      "Metalloids"         ),
            ("halogen",        "Halogens"           ),
            ("noblegas",       "Noble Gases"        ),
            ("othernonmetal",  "Other Nonmetals"    ),
        };

        // Vivid cyan default, vivid red active — unmistakably distinct from the
        // old dark-red prefab buttons so it's obvious the new UI is running.
        private static readonly Color NormalColor = new Color(0.10f, 0.55f, 0.85f, 0.95f); // cyan
        private static readonly Color ActiveColor = new Color(0.95f, 0.20f, 0.20f, 1.00f); // bright red

        // ---------- State ---------------------------------------------------------
        private GameObject _categoryPanel;
        private GameObject _toggleButtonGo;
        private Text       _toggleLabel;
        private Image      _toggleBg;
        private bool       _panelOpen;
        private string     _activeKey;
        private readonly Dictionary<string, (Button btn, Image img)> _categoryButtons = new();

        private TapToPlace _placer;

        // ---------- Bootstrap -----------------------------------------------------
        private const string Version = "v4";

        private GameObject _bannerGo;
        private float      _bannerHideAt;

        private void Start()
        {
            BuildToggleAndPanel();
            SetPanelOpen(false);
            ShowStartupBanner($"NEW UI {Version} ACTIVE");
            Debug.Log($"[CategoryFolderButton] {Version} ACTIVE — custom category panel ready.");
        }

        private void Update()
        {
            // Force-hide the prefab FilterBar continuously. Cheap because we cache
            // the controller after first find.
            HidePrefabFilterBar();

            // Lazy-resolve placer.
            if (_placer == null) _placer = FindAnyObjectByType<TapToPlace>();

            // Auto-hide the startup banner after a few seconds.
            if (_bannerGo != null && Time.unscaledTime >= _bannerHideAt)
            {
                Destroy(_bannerGo);
                _bannerGo = null;
            }
        }

        private void HidePrefabFilterBar()
        {
            // Most reliable hide: find the actual CategoryFilterController component
            // (it lives on the FilterBar GameObject in ARHudCanvas.prefab) and disable
            // that GameObject directly. Catches inactive ones too.
            var ctrls = FindObjectsByType<CategoryFilterController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var ctrl in ctrls)
            {
                if (ctrl == null) continue;
                if (ctrl.gameObject.activeSelf)
                {
                    ctrl.gameObject.SetActive(false);
                    Debug.Log($"[CategoryFolderButton] Hid prefab FilterBar at '{ctrl.gameObject.name}'.");
                }
            }
            // Belt-and-suspenders: also search by name in case the controller is missing.
            var rts = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var rt in rts)
            {
                if (rt.name == "FilterBar" && rt.gameObject.activeSelf)
                {
                    rt.gameObject.SetActive(false);
                    Debug.Log("[CategoryFolderButton] Hid FilterBar by name match.");
                }
            }
        }

        // ---------- UI build ------------------------------------------------------
        private void BuildToggleAndPanel()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000; // above HUD

            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            gameObject.AddComponent<GraphicRaycaster>();

            BuildToggleButton();
            BuildCategoryPanel();
        }

        private void BuildToggleButton()
        {
            _toggleButtonGo = new GameObject("CategoriesToggle");
            _toggleButtonGo.transform.SetParent(transform, false);

            // Top-left of screen, mirroring the standard hamburger-menu placement.
            var rt = _toggleButtonGo.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(40f, -40f);
            rt.sizeDelta        = new Vector2(380f, 110f);

            _toggleBg = _toggleButtonGo.AddComponent<Image>();
            _toggleBg.color = ToggleClosedColor;

            var btn = _toggleButtonGo.AddComponent<Button>();
            btn.targetGraphic = _toggleBg;
            btn.onClick.AddListener(() => SetPanelOpen(!_panelOpen));

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(_toggleButtonGo.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            _toggleLabel = labelGo.AddComponent<Text>();
            _toggleLabel.text      = "≡  Categories";
            _toggleLabel.alignment = TextAnchor.MiddleCenter;
            _toggleLabel.color     = Color.white;
            _toggleLabel.fontSize  = 42;
            _toggleLabel.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _toggleLabel.raycastTarget = false;
        }

        private void BuildCategoryPanel()
        {
            _categoryPanel = new GameObject("CategoryPanel");
            _categoryPanel.transform.SetParent(transform, false);

            var rt = _categoryPanel.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 200f);
            rt.sizeDelta        = new Vector2(1180f, 320f);

            var bg = _categoryPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);
            bg.raycastTarget = true;

            var grid = _categoryPanel.AddComponent<GridLayoutGroup>();
            grid.cellSize        = new Vector2(220f, 130f);
            grid.spacing         = new Vector2(12f, 12f);
            grid.padding         = new RectOffset(20, 20, 20, 20);
            grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis       = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment  = TextAnchor.MiddleCenter;

            foreach (var (key, label) in s_Categories)
            {
                BuildCategoryButton(_categoryPanel.transform, key, label);
            }
        }

        private void BuildCategoryButton(Transform parent, string key, string label)
        {
            var btnGo = new GameObject("CatBtn_" + key);
            btnGo.transform.SetParent(parent, false);
            btnGo.AddComponent<RectTransform>();

            var img = btnGo.AddComponent<Image>();
            img.color = NormalColor;
            img.raycastTarget = true;

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            // Match the button's per-state tint to white so img.color is what shows.
            var colors = btn.colors;
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.95f);
            colors.pressedColor     = new Color(0.85f, 0.85f, 0.85f, 0.95f);
            colors.selectedColor    = Color.white;
            colors.disabledColor    = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            colors.colorMultiplier  = 1f;
            btn.colors = colors;

            string localKey = key;
            btn.onClick.AddListener(() => OnCategoryPressed(localKey));

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            var txt = labelGo.AddComponent<Text>();
            txt.text          = label;
            txt.alignment     = TextAnchor.MiddleCenter;
            txt.color         = Color.white;
            txt.fontSize      = 24;
            txt.fontStyle     = FontStyle.Bold;
            txt.font          = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;

            _categoryButtons[key] = (btn, img);
        }

        // ---------- Click handlers -------------------------------------------------
        private void OnCategoryPressed(string key)
        {
            Debug.Log($"[CategoryFolderButton] Category pressed: '{key}'.");

            var highlighter = ResolveHighlighter();
            if (highlighter == null)
            {
                Debug.LogWarning("[CategoryFolderButton] No spawned table — place the table first.");
                // Still flash the button visually so the user knows the press registered.
                StartCoroutine(FlashButton(key));
                return;
            }

            highlighter.Highlight(key);                 // toggles internally
            _activeKey = highlighter.ActiveButton;       // null if just toggled off
            UpdateButtonVisuals();
            Debug.Log($"[CategoryFolderButton] After Highlight: activeKey='{_activeKey}'.");
        }

        private IEnumerator FlashButton(string key)
        {
            if (!_categoryButtons.TryGetValue(key, out var entry)) yield break;
            var img = entry.img;
            var orig = img.color;
            img.color = ActiveColor;
            yield return new WaitForSeconds(0.25f);
            if (key != _activeKey) img.color = orig;
        }

        private void UpdateButtonVisuals()
        {
            foreach (var kv in _categoryButtons)
            {
                kv.Value.img.color = (kv.Key == _activeKey) ? ActiveColor : NormalColor;
            }
        }

        private ElementCategoryHighlighter ResolveHighlighter()
        {
            if (_placer == null) _placer = FindAnyObjectByType<TapToPlace>();
            if (_placer == null || _placer.CurrentInstance == null) return null;
            var go = _placer.CurrentInstance;
            return go.GetComponent<ElementCategoryHighlighter>()
                ?? go.AddComponent<ElementCategoryHighlighter>();
        }

        // ---------- Panel open/close ---------------------------------------------
        private void SetPanelOpen(bool open)
        {
            _panelOpen = open;
            if (_categoryPanel != null) _categoryPanel.SetActive(open);
            if (_toggleBg != null)     _toggleBg.color = open ? ToggleOpenColor : ToggleClosedColor;
            if (_toggleLabel != null)  _toggleLabel.text = open ? "✕  Close" : "≡  Categories";
        }

        private static readonly Color ToggleClosedColor = new Color(0.20f, 0.20f, 0.20f, 0.92f);
        private static readonly Color ToggleOpenColor   = new Color(0.20f, 0.55f, 0.95f, 0.92f);

        // ---------- Startup banner -----------------------------------------------
        private void ShowStartupBanner(string text)
        {
            _bannerGo = new GameObject("StartupBanner");
            _bannerGo.transform.SetParent(transform, false);

            var rt = _bannerGo.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 1f);
            rt.anchorMax        = new Vector2(0.5f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -180f);
            rt.sizeDelta        = new Vector2(900f, 110f);

            var bg = _bannerGo.AddComponent<Image>();
            bg.color = new Color(1f, 0f, 1f, 0.95f); // magenta — impossible to confuse with anything else
            bg.raycastTarget = false;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(_bannerGo.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            var txt = labelGo.AddComponent<Text>();
            txt.text          = text;
            txt.alignment     = TextAnchor.MiddleCenter;
            txt.color         = Color.white;
            txt.fontSize      = 44;
            txt.fontStyle     = FontStyle.Bold;
            txt.font          = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.raycastTarget = false;

            _bannerHideAt = Time.unscaledTime + 4f;
        }
    }
}
