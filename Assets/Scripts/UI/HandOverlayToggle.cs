// Assets/Scripts/UI/HandOverlayToggle.cs
//
// Self-bootstrapping top-right toggle button that turns the hand-skeleton
// overlay (HandJointVisualizer's joint dots, finger bones, and status pill)
// on or off at runtime.
//
// Why this exists: when the overlay is on top of the user's hand, it can
// occlude the cube they're trying to grab — they can't tell whether the
// info card / Bohr model popup actually fired. Letting the user flip the
// overlay off briefly lets them see the AR scene cleanly.
//
// No scene wiring required.

using UnityEngine;
using UnityEngine.UI;
using PeriodicAR.Debugging;

namespace PeriodicAR.UI
{
    public class HandOverlayToggle : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<HandOverlayToggle>() != null) return;

            var go = new GameObject("[HandOverlayToggle]");
            DontDestroyOnLoad(go);
            go.AddComponent<HandOverlayToggle>();
        }

        // Same palette family as the Categories toggle so the two read as a set.
        private static readonly Color OffColor = new Color(0.20f, 0.20f, 0.20f, 0.92f);  // dark grey when overlay is OFF
        private static readonly Color OnColor  = new Color(0.20f, 0.55f, 0.95f, 0.92f);  // blue when overlay is ON

        private GameObject _buttonGo;
        private Image      _bg;
        private Text       _label;

        private void Start()
        {
            BuildButton();
            ApplyVisualState();
            Debug.Log("[HandOverlayToggle] Top-right hand overlay toggle ready.");
        }

        private void Update()
        {
            // Keep the button visuals in sync with the visualizer in case
            // something else flips it (or the visualizer hasn't bootstrapped
            // yet on the very first frame).
            ApplyVisualState();
        }

        private void BuildButton()
        {
            // Own canvas so we sit above the HUD and the categories toggle.
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;

            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            gameObject.AddComponent<GraphicRaycaster>();

            _buttonGo = new GameObject("HandOverlayToggleButton");
            _buttonGo.transform.SetParent(transform, false);

            // Top-right placement, mirroring the Categories button on the top-left.
            var rt = _buttonGo.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-40f, -40f);
            rt.sizeDelta        = new Vector2(380f, 110f);

            _bg = _buttonGo.AddComponent<Image>();
            _bg.color = OffColor;

            var btn = _buttonGo.AddComponent<Button>();
            btn.targetGraphic = _bg;
            // Per-state tint kept neutral so _bg.color is what shows.
            var colors = btn.colors;
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.95f);
            colors.pressedColor     = new Color(0.85f, 0.85f, 0.85f, 0.95f);
            colors.selectedColor    = Color.white;
            colors.disabledColor    = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            colors.colorMultiplier  = 1f;
            btn.colors = colors;
            btn.onClick.AddListener(OnPressed);

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(_buttonGo.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            _label = labelGo.AddComponent<Text>();
            _label.text          = "Hand Overlay: ON";
            _label.alignment     = TextAnchor.MiddleCenter;
            _label.color         = Color.white;
            _label.fontSize      = 36;
            _label.fontStyle     = FontStyle.Bold;
            _label.font          = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.raycastTarget = false;
        }

        private void OnPressed()
        {
            var viz = HandJointVisualizer.Instance;
            if (viz == null)
            {
                // Visualizer not up yet — try to find it directly.
                viz = FindAnyObjectByType<HandJointVisualizer>();
            }

            if (viz == null)
            {
                Debug.LogWarning("[HandOverlayToggle] HandJointVisualizer not found; cannot toggle.");
                return;
            }

            viz.ToggleVisualizer();
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            var viz = HandJointVisualizer.Instance;
            bool isOn = viz != null && viz.IsVisualizerOn;

            if (_bg != null)    _bg.color   = isOn ? OnColor : OffColor;
            if (_label != null) _label.text = isOn ? "Hand Overlay: ON" : "Hand Overlay: OFF";
        }
    }
}
