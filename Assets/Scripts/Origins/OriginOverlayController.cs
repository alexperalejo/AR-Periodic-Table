using System.Collections.Generic;
using System.Globalization;
using PeriodicAR.AR;
using PeriodicAR.AR.HandTracking;
using PeriodicAR.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PeriodicAR.Origins
{
    /// <summary>
    /// Self-bootstrapping. Adds "Origin" button to HUD (-240, -240).
    /// Colors all cubes by cosmic origin category. Tap any cube to read its story.
    /// </summary>
    public class OriginOverlayController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<OriginOverlayController>() != null) return;
            var go = new GameObject("[OriginOverlayController]");
            DontDestroyOnLoad(go);
            go.AddComponent<OriginOverlayController>();
        }

        public static OriginOverlayController Instance { get; private set; }

        private static readonly int s_BaseColorId = Shader.PropertyToID("_BaseColor");

        private ElementOriginsFile _originsFile;
        private bool               _overlayActive;

        private TapToPlace  _placer;
        private ElementLoader _loader;
        private GameObject  _hookedTable;
        private MaterialPropertyBlock _mpb;

        private struct CubeEntry
        {
            public Renderer  renderer;
            public Color     originalColor;
            public string    symbol;
            public Transform tileRoot;
        }
        private List<CubeEntry> _cubes = new();

        // Grab hooks for story tap.
        private readonly List<(
            XRGrabInteractable grab,
            UnityAction<SelectEnterEventArgs> onEnter
        )> _touchHooks = new();
        private readonly List<(CubeHandGrabbable hand, System.Action onGrab)> _handHooks = new();

        // Story panel
        private GameObject  _storyPanel;
        private TMP_Text    _storyTitle;
        private TMP_Text    _storyBody;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _mpb = new MaterialPropertyBlock();
        }

        private void Start()
        {
            LoadData();
            if (UI.AppShellController.Instance != null)
            {
                UI.AppShellController.Instance.RegisterWheelItem(new UI.WheelItem
                {
                    id    = "origin",
                    label = "Origin",
                    iconName = "★",
                    page  = 0,
                    onTap = ToggleOverlay,
                });
            }
            else
            {
                CreateHudButton();
            }
            BuildStoryPanel();
        }

        private void Update()
        {
            if (_placer == null) _placer = FindAnyObjectByType<TapToPlace>();
            if (_loader == null) _loader = FindAnyObjectByType<ElementLoader>();

            var current = _placer?.CurrentInstance;
            if (current != _hookedTable)
            {
                UnhookTable();
                _cubes.Clear();
                _hookedTable = current;
                if (current != null && _loader?.Table != null)
                {
                    CacheTable(current);
                    if (_overlayActive) ApplyColors();
                }
            }
        }

        private void LoadData()
        {
            var asset = Resources.Load<TextAsset>("ElementOrigins");
            if (asset == null) { Debug.LogError("[OriginOverlay] ElementOrigins.json not found."); return; }
            _originsFile = Newtonsoft.Json.JsonConvert.DeserializeObject<ElementOriginsFile>(asset.text);
        }

        private void CacheTable(GameObject table)
        {
            foreach (Transform child in table.transform)
            {
                int z = PeriodicTableLayout.ResolveAtomicNumber(child);
                if (z <= 0) continue;
                var rend = child.GetComponentInChildren<Renderer>();
                if (rend == null) continue;
                var data = _loader.FindByNumber(z);
                if (data == null) continue;

                rend.GetPropertyBlock(_mpb);
                Color c = _mpb.GetColor(s_BaseColorId);
                if (c == default && rend.sharedMaterial != null && rend.sharedMaterial.HasProperty(s_BaseColorId))
                    c = rend.sharedMaterial.GetColor(s_BaseColorId);

                _cubes.Add(new CubeEntry { renderer = rend, originalColor = c, symbol = data.symbol, tileRoot = child });
            }
        }

        public void ToggleOverlay()
        {
            _overlayActive = !_overlayActive;
            if (_overlayActive)
            {
                ApplyColors();
                HookGrabs();
            }
            else
            {
                ClearColors();
                UnhookTable();
                HideStory();
            }

            if (Tutor.AppStateProvider.Instance != null)
                Tutor.AppStateProvider.Instance.originOverlayActive = _overlayActive;
        }

        private void ApplyColors()
        {
            if (_originsFile?.origins == null) return;
            foreach (var entry in _cubes)
            {
                Color tint = Color.gray;
                if (_originsFile.origins.TryGetValue(entry.symbol, out var origin))
                    tint = CategoryColor(origin.source);

                entry.renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(s_BaseColorId, tint);
                entry.renderer.SetPropertyBlock(_mpb);
            }
        }

        private void ClearColors()
        {
            foreach (var entry in _cubes)
            {
                entry.renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(s_BaseColorId, entry.originalColor);
                entry.renderer.SetPropertyBlock(_mpb);
            }
        }

        private Color CategoryColor(string source)
        {
            if (_originsFile?.categories != null &&
                _originsFile.categories.TryGetValue(source, out var cat) &&
                TryParseHex(cat.color, out Color c))
                return c;
            return new Color(0.5f, 0.5f, 0.5f);
        }

        // ---- Grab hooks to show story panel -----------------------------------

        private void HookGrabs()
        {
            if (_hookedTable == null) return;
            foreach (var grab in _hookedTable.GetComponentsInChildren<XRGrabInteractable>(true))
            {
                int z = PeriodicTableLayout.ResolveAtomicNumber(grab.transform);
                if (z <= 0) continue;
                var data = _loader?.FindByNumber(z);
                if (data == null) continue;
                string sym = data.symbol;
                UnityAction<SelectEnterEventArgs> onEnter = (_) => ShowStory(sym);
                grab.selectEntered.AddListener(onEnter);
                _touchHooks.Add((grab, onEnter));
            }
            foreach (var hand in _hookedTable.GetComponentsInChildren<CubeHandGrabbable>(true))
            {
                int z = PeriodicTableLayout.ResolveAtomicNumber(hand.transform);
                if (z <= 0) continue;
                var data = _loader?.FindByNumber(z);
                if (data == null) continue;
                string sym = data.symbol;
                System.Action onGrab = () => ShowStory(sym);
                hand.Grabbed += onGrab;
                _handHooks.Add((hand, onGrab));
            }
        }

        private void UnhookTable()
        {
            foreach (var (grab, onEnter) in _touchHooks)
                if (grab != null) grab.selectEntered.RemoveListener(onEnter);
            _touchHooks.Clear();
            foreach (var (hand, onGrab) in _handHooks)
                if (hand != null) hand.Grabbed -= onGrab;
            _handHooks.Clear();
        }

        // ---- Story panel ------------------------------------------------------

        private void ShowStory(string symbol)
        {
            if (_originsFile?.origins == null || _storyPanel == null) return;
            if (!_originsFile.origins.TryGetValue(symbol, out var origin)) return;

            string catLabel = "Unknown";
            if (_originsFile.categories != null &&
                _originsFile.categories.TryGetValue(origin.source, out var cat))
                catLabel = cat.label;

            if (_storyTitle != null) _storyTitle.text = $"{symbol} — {catLabel}";
            if (_storyBody  != null) _storyBody.text  = origin.story ?? "";
            _storyPanel.SetActive(true);
        }

        private void HideStory() { if (_storyPanel != null) _storyPanel.SetActive(false); }

        private void BuildStoryPanel()
        {
            var canvas = FindHudCanvas();
            if (canvas == null) return;

            _storyPanel = new GameObject("OriginStoryPanel", typeof(RectTransform));
            DontDestroyOnLoad(_storyPanel);
            _storyPanel.transform.SetParent(canvas.transform, false);
            var rt = _storyPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.05f); rt.anchorMax = new Vector2(0.95f, 0.35f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _storyPanel.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.12f, 0.94f);

            var vlg = _storyPanel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft; vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false; vlg.spacing = 8f;
            vlg.padding = new RectOffset(20, 20, 16, 16);

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(_storyPanel.transform, false);
            titleGo.AddComponent<LayoutElement>().preferredHeight = 60f;
            _storyTitle = titleGo.AddComponent<TextMeshProUGUI>();
            _storyTitle.fontSize = 38f; _storyTitle.fontStyle = FontStyles.Bold;
            _storyTitle.color = Color.white;

            var bodyGo = new GameObject("Body", typeof(RectTransform));
            bodyGo.transform.SetParent(_storyPanel.transform, false);
            var bodyCsf = bodyGo.AddComponent<ContentSizeFitter>();
            bodyCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _storyBody = bodyGo.AddComponent<TextMeshProUGUI>();
            _storyBody.fontSize = 30f; _storyBody.color = new Color(0.9f, 0.9f, 0.9f);
            _storyBody.textWrappingMode = TMPro.TextWrappingModes.Normal;

            // Close button
            var closeGo = new GameObject("Close", typeof(RectTransform));
            closeGo.transform.SetParent(_storyPanel.transform, false);
            closeGo.AddComponent<LayoutElement>().preferredHeight = 60f;
            closeGo.AddComponent<Image>().color = new Color(0.5f, 0.1f, 0.1f, 1f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.onClick.AddListener(HideStory);
            var closeTmp = new GameObject("Label", typeof(RectTransform));
            closeTmp.transform.SetParent(closeGo.transform, false);
            var crt = closeTmp.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            var ctmp = closeTmp.AddComponent<TextMeshProUGUI>();
            ctmp.text = "Close"; ctmp.fontSize = 32f; ctmp.alignment = TextAlignmentOptions.Center;
            ctmp.color = Color.white;

            _storyPanel.SetActive(false);
        }

        private void CreateHudButton()
        {
            Canvas hudCanvas = FindHudCanvas();
            if (hudCanvas == null) return;

            var btnGo = new GameObject("OriginButton", typeof(RectTransform));
            btnGo.transform.SetParent(hudCanvas.transform, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-240f, -240f);
            rt.sizeDelta        = new Vector2(180f, 180f);
            btnGo.AddComponent<Image>().color = new Color(0.22f, 0.10f, 0.08f, 0.85f);
            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(ToggleOverlay);

            var lGo = new GameObject("Label", typeof(RectTransform));
            lGo.transform.SetParent(btnGo.transform, false);
            var lrt = lGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(4, 30); lrt.offsetMax = new Vector2(-4, -4);
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "Origin"; tmp.fontSize = 36f; tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
        }

        private static bool TryParseHex(string hex, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrEmpty(hex)) return false;
            if (hex[0] == '#') hex = hex.Substring(1);
            if (hex.Length != 6) return false;
            if (!byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r)) return false;
            if (!byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)) return false;
            if (!byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b)) return false;
            color = new Color(r / 255f, g / 255f, b / 255f, 1f); return true;
        }

        private static Canvas FindHudCanvas()
        {
            var hud = FindAnyObjectByType<UI.ARHudController>();
            Canvas c = hud != null ? hud.GetComponentInParent<Canvas>() : null;
            return c != null ? c : FindFirstObjectByType<Canvas>();
        }
    }
}
