using System.Collections.Generic;
using System.Linq;
using System.Text;
using PeriodicAR.History;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Household
{
    /// <summary>
    /// Self-bootstrapping. Adds "House" button to HUD (-440, -240).
    /// Shows household items that contain the currently held element,
    /// cross-referenced with the user's scan history.
    /// </summary>
    public class HouseholdController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<HouseholdController>() != null) return;
            var go = new GameObject("[HouseholdController]");
            DontDestroyOnLoad(go);
            go.AddComponent<HouseholdController>();
        }

        public static HouseholdController Instance { get; private set; }

        private Button   _houseButton;
        private GameObject _panel;
        private TMP_Text   _panelTitle;
        private TMP_Text   _panelBody;
        private bool       _panelOpen;
        private string     _lastShownSymbol;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (UI.AppShellController.Instance != null)
            {
                UI.AppShellController.Instance.RegisterWheelItem(new UI.WheelItem
                {
                    id    = "household",
                    label = "Home",
                    iconName = "⌂",
                    page  = 1,
                    onTap = TogglePanel,
                });
            }
            else
            {
                CreateHudButton();
            }
            BuildPanel();
        }


        private void Update()
        {
            if (!_panelOpen) return;
            // Refresh if held element changed.
            var sym = Tutor.AppStateProvider.Instance?.heldElementSymbol;
            if (!string.IsNullOrEmpty(sym) && sym != _lastShownSymbol)
                RefreshPanel(sym);
        }

        public void ShowForElement(string symbol)
        {
            if (_panel == null) return;
            RefreshPanel(symbol);
            _panel.SetActive(true);
            _panelOpen = true;
        }

        private void RefreshPanel(string symbol)
        {
            _lastShownSymbol = symbol;
            var entry = HouseholdSourceCatalog.Lookup(symbol);

            var sb = new StringBuilder();
            if (entry == null || (entry.items == null || entry.items.Count == 0))
            {
                sb.AppendLine("No common household sources recorded for this element.");
            }
            else
            {
                sb.AppendLine("<b>Found in your home:</b>");
                foreach (var item in entry.items)
                    sb.AppendLine($"  • {item}");

                if (!string.IsNullOrEmpty(entry.funFact))
                {
                    sb.AppendLine();
                    sb.AppendLine($"<i>{entry.funFact}</i>");
                }

                // Cross-reference scan history.
                var seenObjects = ScanHistory.AllSeenObjects();
                if (seenObjects.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("<b>You've scanned:</b>");
                    foreach (var obj in seenObjects.Take(5))
                        sb.AppendLine($"  • {obj}");
                }
            }

            if (_panelTitle != null) _panelTitle.text = $"{symbol} — In Your House";
            if (_panelBody  != null) _panelBody.text  = sb.ToString();
        }

        public void TogglePanel()
        {
            if (_panelOpen)
            {
                _panel?.SetActive(false);
                _panelOpen = false;
            }
            else
            {
                var sym = Tutor.AppStateProvider.Instance?.heldElementSymbol;
                if (string.IsNullOrEmpty(sym)) sym = "H";
                ShowForElement(sym);
            }
        }

        private void BuildPanel()
        {
            Canvas hudCanvas = FindHudCanvas();
            if (hudCanvas == null) return;

            _panel = new GameObject("HouseholdPanel", typeof(RectTransform));
            DontDestroyOnLoad(_panel);
            _panel.transform.SetParent(hudCanvas.transform, false);

            var rt = _panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.03f, 0.05f); rt.anchorMax = new Vector2(0.97f, 0.55f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = new Color(0.06f, 0.10f, 0.06f, 0.96f);

            var vlg = _panel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft; vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false; vlg.spacing = 8f;
            vlg.padding = new RectOffset(24, 24, 16, 16);

            // Title
            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(_panel.transform, false);
            titleGo.AddComponent<LayoutElement>().preferredHeight = 64f;
            _panelTitle = titleGo.AddComponent<TextMeshProUGUI>();
            _panelTitle.fontSize = 40f; _panelTitle.fontStyle = FontStyles.Bold;
            _panelTitle.color = new Color(0.6f, 1.0f, 0.6f);

            // Body scroll view
            var scrollGo = new GameObject("Scroll", typeof(RectTransform));
            scrollGo.transform.SetParent(_panel.transform, false);
            var scrollLe = scrollGo.AddComponent<LayoutElement>(); scrollLe.flexibleHeight = 1f;
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scrollGo.AddComponent<Image>().color = Color.clear;
            scrollGo.AddComponent<Mask>().showMaskGraphic = false;
            scroll.horizontal = false; scroll.vertical = true;

            var vpGo = new GameObject("Viewport", typeof(RectTransform));
            vpGo.transform.SetParent(scrollGo.transform, false);
            var vpRt = vpGo.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            vpGo.AddComponent<RectMask2D>();
            scroll.viewport = vpRt;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(vpGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f); contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f); contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRt;

            _panelBody = contentGo.AddComponent<TextMeshProUGUI>();
            _panelBody.fontSize = 30f; _panelBody.color = Color.white;
            _panelBody.textWrappingMode = TMPro.TextWrappingModes.Normal;

            // Close button
            var closeGo = new GameObject("Close", typeof(RectTransform));
            closeGo.transform.SetParent(_panel.transform, false);
            closeGo.AddComponent<LayoutElement>().preferredHeight = 70f;
            closeGo.AddComponent<Image>().color = new Color(0.5f, 0.1f, 0.1f, 1f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.onClick.AddListener(TogglePanel);
            var closeTxtGo = new GameObject("Label", typeof(RectTransform));
            closeTxtGo.transform.SetParent(closeGo.transform, false);
            var closert = closeTxtGo.GetComponent<RectTransform>();
            closert.anchorMin = Vector2.zero; closert.anchorMax = Vector2.one;
            closert.offsetMin = Vector2.zero; closert.offsetMax = Vector2.zero;
            var ctmp = closeTxtGo.AddComponent<TextMeshProUGUI>();
            ctmp.text = "Close"; ctmp.fontSize = 32f;
            ctmp.alignment = TextAlignmentOptions.Center; ctmp.color = Color.white;

            _panel.SetActive(false);
        }

        private void CreateHudButton()
        {
            Canvas hudCanvas = FindHudCanvas();
            if (hudCanvas == null) return;

            var btnGo = new GameObject("HouseButton", typeof(RectTransform));
            btnGo.transform.SetParent(hudCanvas.transform, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-440f, -240f);
            rt.sizeDelta = new Vector2(180f, 180f);
            btnGo.AddComponent<Image>().color = new Color(0.10f, 0.20f, 0.10f, 0.85f);
            _houseButton = btnGo.AddComponent<Button>();
            _houseButton.onClick.AddListener(TogglePanel);

            var lGo = new GameObject("Label", typeof(RectTransform));
            lGo.transform.SetParent(btnGo.transform, false);
            var lrt = lGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(4, 30); lrt.offsetMax = new Vector2(-4, -4);
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "House"; tmp.fontSize = 36f; tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
        }

        private static Canvas FindHudCanvas()
        {
            var hud = FindAnyObjectByType<UI.ARHudController>();
            Canvas c = hud != null ? hud.GetComponentInParent<Canvas>() : null;
            return c != null ? c : FindFirstObjectByType<Canvas>();
        }
    }
}
