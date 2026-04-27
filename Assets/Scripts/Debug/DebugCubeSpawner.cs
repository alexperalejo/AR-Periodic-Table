// Assets/Scripts/Debug/DebugCubeSpawner.cs
//
// Self-bootstrapping diagnostic. Adds a giant green "TEST CUBE" button at the
// top-LEFT of the screen at runtime — no scene/prefab wiring required.
//
// Behavior:
//   1. Tap the button once   → arms placement. Label changes to "TAP A PLANE".
//   2. Tap a detected plane  → spawns a 5cm black cube at the tap point.
//   3. Override clears, label resets to "TEST CUBE".
//   4. Tap the button again while armed → cancels (clears the override).
//
// Uses TapToPlace's one-shot override so it goes through the same input filtering,
// UI blocking, and AR raycast as the real placement. If a plane tap successfully
// places the cube, you've proven that input → raycast → placement → rendering all
// work, and the only remaining variable is the periodic-table prefab itself.
//
// To remove later: delete this file. No other cleanup needed.

using UnityEngine;
using UnityEngine.UI;
using PeriodicAR.AR;

namespace PeriodicAR.Debugging
{
    public class DebugCubeSpawner : MonoBehaviour
    {
        // Bootstrap disabled — TEST CUBE button retired per user request.
        // The class compiles but never runs now.
        private static void Bootstrap() { }

        // ---------- Tunables ------------------------------------------------------
        private const float CubeSizeMeters = 0.05f;     // 5cm cube
        private static readonly Color CubeColor = Color.black;

        private const string LabelIdle  = "TEST CUBE";
        private const string LabelArmed = "TAP A PLANE";

        // ---------- State ---------------------------------------------------------
        private TapToPlace _tapToPlace;
        private GameObject _cubeTemplate; // disabled in-scene "prefab" we instantiate from
        private Button     _button;
        private Image      _buttonImage;
        private Text       _label;

        private void Awake()
        {
            BuildUi();
            BuildCubeTemplate();
            // Hook TapToPlace lazily — it might not exist yet in the very first frame
            // if the scene is still loading. Awake-time scene access is normally fine,
            // but defend with a fallback.
            TryHookTapToPlace();
            Debug.Log("[DebugCubeSpawner] Active. Tap TEST CUBE to arm; then tap a detected plane.");
        }

        private void Update()
        {
            // If TapToPlace appeared after we initialized (e.g. additive scene load), connect now.
            if (_tapToPlace == null) TryHookTapToPlace();
        }

        private void TryHookTapToPlace()
        {
            var found = FindAnyObjectByType<TapToPlace>();
            if (found == null || found == _tapToPlace) return;
            _tapToPlace = found;
            _tapToPlace.PlacementOverrideChanged += OnOverrideChanged;
            Debug.Log("[DebugCubeSpawner] Hooked TapToPlace.");
        }

        private void OnDestroy()
        {
            if (_tapToPlace != null) _tapToPlace.PlacementOverrideChanged -= OnOverrideChanged;
        }

        // ---------- UI ------------------------------------------------------------
        private void BuildUi()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 31000; // above HUD, below TapIndicator(32000)

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            gameObject.AddComponent<GraphicRaycaster>();

            // Button
            var btnGo = new GameObject("TestCubeButton");
            btnGo.transform.SetParent(transform, false);

            var rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(40f, -40f);
            rt.sizeDelta        = new Vector2(380f, 180f);

            _buttonImage = btnGo.AddComponent<Image>();
            _buttonImage.color = IdleColor;

            _button = btnGo.AddComponent<Button>();
            _button.targetGraphic = _buttonImage;
            _button.onClick.AddListener(OnButtonClicked);

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);

            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            _label = labelGo.AddComponent<Text>();
            _label.text      = LabelIdle;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color     = Color.black;
            _label.fontSize  = 44;
            _label.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static readonly Color IdleColor  = new Color(0.15f, 0.85f, 0.25f, 0.95f); // bright green
        private static readonly Color ArmedColor = new Color(1.00f, 0.55f, 0.10f, 0.95f); // bright orange

        // ---------- Cube template -------------------------------------------------
        private void BuildCubeTemplate()
        {
            // A GameObject acting as an in-memory prefab. Instantiate(go) gives us a clone.
            // We keep it ACTIVE (so clones spawn active too) but stash it far below the world
            // and hide its renderer so it never shows up.
            _cubeTemplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cubeTemplate.name = "DebugCubeTemplate";
            _cubeTemplate.transform.SetParent(transform, false);
            _cubeTemplate.transform.localPosition = new Vector3(0f, -10000f, 0f);
            _cubeTemplate.transform.localScale    = Vector3.one * CubeSizeMeters;

            ApplyVisibleMaterial(_cubeTemplate);
            // Strip the collider so it doesn't block subsequent taps when spawned.
            var col = _cubeTemplate.GetComponent<Collider>();
            if (col != null) Destroy(col);
            // Renderer stays ENABLED on the template so cloned cubes also have an enabled renderer.
            // The template itself is invisible only because it's positioned 10km below the world.
        }

        // ---------- Click handler -------------------------------------------------
        private void OnButtonClicked()
        {
            TryHookTapToPlace();
            if (_tapToPlace == null)
            {
                Debug.LogWarning("[DebugCubeSpawner] No TapToPlace in scene; cannot arm placement.");
                return;
            }

            if (_tapToPlace.IsOverrideArmed)
            {
                // Cancel
                _tapToPlace.SetOneShotPlacementOverride(null);
                Debug.Log("[DebugCubeSpawner] Cancelled — override cleared.");
            }
            else
            {
                _tapToPlace.SetOneShotPlacementOverride(_cubeTemplate);
                Debug.Log("[DebugCubeSpawner] Armed — next plane tap places a cube.");
            }
        }

        private void OnOverrideChanged(bool armed)
        {
            if (_buttonImage != null) _buttonImage.color = armed ? ArmedColor : IdleColor;
            if (_label       != null) _label.text        = armed ? LabelArmed : LabelIdle;
        }

        // ---------- Material ------------------------------------------------------
        private static void ApplyVisibleMaterial(GameObject go)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Unlit/Color") ??
                Shader.Find("Standard");
            if (shader == null) return;

            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", CubeColor);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     CubeColor);
            renderer.sharedMaterial = mat;
        }
    }
}
