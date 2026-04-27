// Assets/Scripts/Debug/EditorPreviewSpawner.cs
//
// Editor-mode preview for the periodic table spawn. Self-bootstraps when you
// hit Play in the Unity editor (and ONLY in the editor — does nothing on
// device). Adds two big UI buttons in the top-right of the Game view:
//
//   SPAWN TABLE  — calls TapToPlace.PlaceAt() at a point in front of the
//                  scene's main camera. Goes through the same code path as
//                  a real plane tap, so spawnRotationEuler / absoluteScale /
//                  auto-fit / floor-anchor all behave identically to AR.
//   CLEAR        — destroys the current spawn so you can try again.
//
// Once you've spawned, use the Scene view (or the Game view's camera) to
// rotate around the table, walk the camera through it, etc. The table is
// just a regular GameObject; you can also select it in the Hierarchy to
// inspect or tweak its Transform live.
//
// To remove later: delete this file. No other cleanup needed.

using UnityEngine;
using UnityEngine.UI;
using PeriodicAR.AR;

namespace PeriodicAR.Debugging
{
    public class EditorPreviewSpawner : MonoBehaviour
    {
        // Bootstrap disabled — SPAWN/CLEAR editor preview UI retired per user request.
        // The class compiles but never runs now.
        private static void Bootstrap() { }

        // ---------- Tunables ----------------------------------------------------
        // How far in front of the camera to drop the table when SPAWN is clicked.
        // 1.5m matches the typical "AR placement distance" so the visible scale
        // matches what you'd see on the phone.
        private const float SpawnDistanceMeters = 1.5f;
        // Y of the spawn pose in world coordinates — i.e. the simulated "floor"
        // height. 0 puts the floor at world Y=0 which matches an empty scene.
        private const float FloorY = 0f;

        // ---------- State -------------------------------------------------------
        private TapToPlace _tapToPlace;
        private Text _statusText;
        private bool _autoSpawnDone;
        private float _autoSpawnDelay = 0.4f;  // seconds — wait for camera/scene to settle

        private void Awake()
        {
            BuildUi();
            TryHook();
            Debug.Log("[EditorPreviewSpawner] Active. Auto-spawning table after brief delay; use buttons to re-spawn or clear.");
        }

        private void Update()
        {
            if (_tapToPlace == null) TryHook();

            // Auto-spawn once when both TapToPlace and Camera.main are ready.
            if (!_autoSpawnDone && _tapToPlace != null && Camera.main != null)
            {
                _autoSpawnDelay -= Time.unscaledDeltaTime;
                if (_autoSpawnDelay <= 0f)
                {
                    _autoSpawnDone = true;
                    OnSpawnClicked();
                }
            }

            if (_statusText != null)
            {
                _statusText.text = _tapToPlace == null
                    ? "no TapToPlace in scene"
                    : (_tapToPlace.HasSpawned ? "table spawned" : "ready");
            }
        }

        private void TryHook()
        {
            var found = FindAnyObjectByType<TapToPlace>();
            if (found != null) _tapToPlace = found;
        }

        // ---------- UI ----------------------------------------------------------
        private void BuildUi()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;

            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            gameObject.AddComponent<GraphicRaycaster>();

            // Stack a small column at the top-right.
            // [SPAWN TABLE]  [CLEAR]  [status text]
            BuildButton("SPAWN TABLE", new Vector2(-220f, -40f), new Vector2(360f, 90f),
                        new Color(0.20f, 0.55f, 0.95f, 0.95f), Color.white, 36, OnSpawnClicked);
            BuildButton("CLEAR",       new Vector2(-220f, -150f), new Vector2(360f, 70f),
                        new Color(0.65f, 0.20f, 0.20f, 0.95f), Color.white, 30, OnClearClicked);

            // Status text under the buttons
            var statusGo = new GameObject("Status");
            statusGo.transform.SetParent(transform, false);
            var rt = statusGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-40f, -240f);
            rt.sizeDelta = new Vector2(360f, 40f);

            _statusText = statusGo.AddComponent<Text>();
            _statusText.text      = "...";
            _statusText.alignment = TextAnchor.MiddleRight;
            _statusText.color     = new Color(0.9f, 0.9f, 0.9f, 0.9f);
            _statusText.fontSize  = 22;
            _statusText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statusText.raycastTarget = false;
        }

        private void BuildButton(string label, Vector2 anchoredPos, Vector2 size,
                                 Color bg, Color fg, int fontSize,
                                 UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject("Btn_" + label);
            btnGo.transform.SetParent(transform, false);

            var rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = btnGo.AddComponent<Image>();
            img.color = bg;

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            var text = labelGo.AddComponent<Text>();
            text.text      = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color     = fg;
            text.fontSize  = fontSize;
            text.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.raycastTarget = false;
        }

        // ---------- Click handlers ---------------------------------------------
        private void OnSpawnClicked()
        {
            TryHook();
            if (_tapToPlace == null)
            {
                Debug.LogWarning("[EditorPreviewSpawner] No TapToPlace in scene; cannot spawn.");
                return;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[EditorPreviewSpawner] No Camera.main — tag a camera as MainCamera.");
                return;
            }

            // Drop the spawn point in front of the camera, projected onto the simulated floor.
            Vector3 fwd = cam.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
            fwd.Normalize();

            Vector3 spawnPos = cam.transform.position + fwd * SpawnDistanceMeters;
            spawnPos.y = FloorY;

            var pose = new Pose(spawnPos, Quaternion.identity);
            _tapToPlace.PlaceAt(pose);

            Debug.Log($"[EditorPreviewSpawner] Spawned at {spawnPos}. Use the Scene view to rotate around it; tweak spawnRotationEuler / absoluteScale on TapToPlace if it doesn't look right.");
        }

        private void OnClearClicked()
        {
            TryHook();
            if (_tapToPlace == null)
            {
                Debug.LogWarning("[EditorPreviewSpawner] No TapToPlace in scene.");
                return;
            }
            _tapToPlace.RemoveTable();
            Debug.Log("[EditorPreviewSpawner] Cleared.");
        }
    }
}
