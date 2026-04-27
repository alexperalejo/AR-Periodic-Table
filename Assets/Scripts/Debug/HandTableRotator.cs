// Assets/Scripts/Debug/HandTableRotator.cs
//
// Touch-driven rotation tool for the spawned periodic table. Rotation is
// gated by an on-screen toggle: tap "ROTATE: OFF" to enable, tap again to
// disable. While enabled, drag on the screen to rotate the table; while
// disabled, touches fall through to TapToPlace as normal taps.
//
// Live rotation values are shown in big text at the bottom-center so you
// can read off Euler X/Y/Z and copy them into TapToPlace's
// `Spawn Rotation Euler` field for future spawns.
//
// Touch interactions:
//   • Single-finger drag — yaw (horizontal) + pitch (vertical)
//   • Two-finger twist   — roll
//   • Touches under 12px of movement fall through as taps.
//   • Touches that begin on a Selectable UI element are ignored.
//
// Self-bootstrapping; no scene wiring required.
// (Hand-pinch rotation was removed — only screen touch is used now.)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using PeriodicAR.AR;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace PeriodicAR.Debugging
{
    public class HandTableRotator : MonoBehaviour
    {
        // Bootstrap disabled — touch rotation feature retired per user request.
        // The class is kept so existing references compile; it just never runs now.
        private static void Bootstrap() { }

        // ---------- Tunables ------------------------------------------------------
        // Degrees of rotation per pixel of touch travel. ~0.5°/px means a
        // half-screen swipe spins the table roughly 300°. Tunable.
        private const float YawDegPerPixel    = 0.5f;
        private const float PitchDegPerPixel  = 0.5f;
        // Touches with movement below this in pixels are forwarded to the existing
        // tap handlers (TapToPlace) rather than being treated as a rotation drag.
        private const float TouchDeadZonePixels = 12f;

        // ---------- State ---------------------------------------------------------
        private TapToPlace _tapToPlace;

        private bool       _rotationEnabled;     // gated by the on-screen toggle; default OFF
        private bool       _dragging;
        private Vector2    _dragStartTouchPos;
        private Vector2    _dragStartTouch2Pos;
        private float      _dragStartTwoFingerAngle;
        private bool       _twoFingerActive;
        private Vector3    _dragStartTablePos;
        private Quaternion _dragStartTableRot;
        private Vector3    _dragStartBoundsCenter;
        private bool       _enhancedTouchEnabledByUs;

        // ---------- UI ------------------------------------------------------------
        private Canvas _canvas;
        private Text   _rotationText;
        private Image  _rotationBg;
        private Button _toggleButton;
        private Image  _toggleBg;
        private Text   _toggleLabel;

        private void Awake()
        {
            BuildUi();
            TryHook();
            Debug.Log("[HandTableRotator] Active. Tap 'ROTATE: OFF' at the bottom to enable touch-rotation.");
        }

        private void OnEnable()
        {
            if (!_enhancedTouchEnabledByUs)
            {
                EnhancedTouchSupport.Enable();
                _enhancedTouchEnabledByUs = true;
            }
        }

        private void OnDisable()
        {
            if (_enhancedTouchEnabledByUs)
            {
                EnhancedTouchSupport.Disable();
                _enhancedTouchEnabledByUs = false;
            }
        }

        private void Update()
        {
            if (_tapToPlace == null) TryHook();
            HandleTouchInput();
            UpdateRotationDisplay();
        }

        private void TryHook()
        {
            if (_tapToPlace == null)
            {
                _tapToPlace = FindAnyObjectByType<TapToPlace>();
            }
        }

        // ---------- Per-frame touch-driven rotation ------------------------------
        private void HandleTouchInput()
        {
            // If rotation is disabled, never grab touches. They flow through to
            // TapToPlace and any other tap consumers.
            if (!_rotationEnabled)
            {
                if (_dragging) StopDrag();
                return;
            }

            if (_tapToPlace == null) return;
            var table = _tapToPlace.CurrentInstance;
            if (table == null)
            {
                if (_dragging) StopDrag();
                return;
            }

            // ---- collect screen-space pointers from all input layers ----
            Vector2 p1 = Vector2.zero, p2 = Vector2.zero;
            int activeCount = 0;
            bool primaryBegan = false, primaryEnded = false;

            var touches = Touch.activeTouches;
            for (int i = 0; i < touches.Count && activeCount < 2; i++)
            {
                if (activeCount == 0) p1 = touches[i].screenPosition;
                else                  p2 = touches[i].screenPosition;
                if (touches[i].phase == UnityEngine.InputSystem.TouchPhase.Began && activeCount == 0)
                    primaryBegan = true;
                if ((touches[i].phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                     touches[i].phase == UnityEngine.InputSystem.TouchPhase.Canceled) && activeCount == 0)
                    primaryEnded = true;
                activeCount++;
            }

            // Mouse fallback (Editor)
            if (activeCount == 0 && Mouse.current != null)
            {
                if (Mouse.current.leftButton.isPressed)
                {
                    p1 = Mouse.current.position.ReadValue();
                    activeCount = 1;
                    if (Mouse.current.leftButton.wasPressedThisFrame) primaryBegan = true;
                }
                else if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    p1 = Mouse.current.position.ReadValue();
                    primaryEnded = true;
                }
            }

            // ---- gesture state machine ----
            if (primaryBegan)
            {
                if (IsOverInteractiveUI(p1)) return;
                StartDrag(p1, p2, activeCount, table);
                return;
            }

            if (primaryEnded || activeCount == 0)
            {
                if (_dragging) StopDrag();
                return;
            }

            if (_dragging)
            {
                ApplyTouchRotation(p1, p2, activeCount, table);
            }
        }

        private void StartDrag(Vector2 p1, Vector2 p2, int count, GameObject table)
        {
            var renderers = table.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            _dragStartTablePos     = table.transform.position;
            _dragStartTableRot     = table.transform.rotation;
            _dragStartBoundsCenter = b.center;
            _dragStartTouchPos     = p1;
            _twoFingerActive       = count >= 2;
            if (_twoFingerActive)
            {
                _dragStartTouch2Pos      = p2;
                _dragStartTwoFingerAngle = Mathf.Atan2(p2.y - p1.y, p2.x - p1.x) * Mathf.Rad2Deg;
            }
            _dragging = true;
            Debug.Log($"[HandTableRotator] TOUCH drag started at {p1} (fingers={count}).");
        }

        private void StopDrag()
        {
            _dragging        = false;
            _twoFingerActive = false;
            if (_tapToPlace != null && _tapToPlace.CurrentInstance != null)
            {
                Debug.Log($"[HandTableRotator] Drag released. Final rotation: {_tapToPlace.CurrentInstance.transform.rotation.eulerAngles}");
            }
        }

        private void ApplyTouchRotation(Vector2 p1, Vector2 p2, int count, GameObject table)
        {
            Vector2 delta = p1 - _dragStartTouchPos;
            // Dead zone — small movements still count as taps and fall through.
            if (delta.magnitude < TouchDeadZonePixels && !_twoFingerActive) return;

            float yawDeg   = delta.x * YawDegPerPixel;
            float pitchDeg = -delta.y * PitchDegPerPixel;
            float rollDeg  = 0f;

            if (count >= 2 && _twoFingerActive)
            {
                float currentAngle = Mathf.Atan2(p2.y - p1.y, p2.x - p1.x) * Mathf.Rad2Deg;
                rollDeg += Mathf.DeltaAngle(_dragStartTwoFingerAngle, currentAngle);
            }

            Quaternion deltaRot = Quaternion.AngleAxis(yawDeg,   Vector3.up)
                                * Quaternion.AngleAxis(pitchDeg, Vector3.right)
                                * Quaternion.AngleAxis(rollDeg,  Vector3.forward);

            Vector3 pivot = _dragStartBoundsCenter;
            table.transform.rotation = deltaRot * _dragStartTableRot;
            table.transform.position = pivot + deltaRot * (_dragStartTablePos - pivot);
        }

        private static bool IsOverInteractiveUI(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var data = new PointerEventData(es) { position = screenPos };
            var results = new List<RaycastResult>();
            es.RaycastAll(data, results);
            for (int i = 0; i < results.Count; i++)
            {
                var go = results[i].gameObject;
                if (go == null) continue;
                if (go.GetComponentInParent<Selectable>() != null) return true;
            }
            return false;
        }

        // ---------- UI ------------------------------------------------------------
        private void BuildUi()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 30500;

            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            gameObject.AddComponent<GraphicRaycaster>(); // needed so the toggle button receives taps

            // Background pill (rotation values)
            var pillGo = new GameObject("RotationPill");
            pillGo.transform.SetParent(transform, false);
            var rt = pillGo.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 60f);
            rt.sizeDelta        = new Vector2(900f, 130f);

            _rotationBg = pillGo.AddComponent<Image>();
            _rotationBg.color = new Color(0f, 0f, 0f, 0.65f);
            _rotationBg.raycastTarget = false;

            var labelGo = new GameObject("RotationLabel");
            labelGo.transform.SetParent(pillGo.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            _rotationText = labelGo.AddComponent<Text>();
            _rotationText.text      = "ROTATION  X: -  Y: -  Z: -";
            _rotationText.alignment = TextAnchor.MiddleCenter;
            _rotationText.color     = Color.white;
            _rotationText.fontSize  = 38;
            _rotationText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _rotationText.raycastTarget = false;

            // Toggle button (above the rotation pill)
            var toggleGo = new GameObject("RotationToggle");
            toggleGo.transform.SetParent(transform, false);
            var trt = toggleGo.AddComponent<RectTransform>();
            trt.anchorMin        = new Vector2(0.5f, 0f);
            trt.anchorMax        = new Vector2(0.5f, 0f);
            trt.pivot            = new Vector2(0.5f, 0f);
            trt.anchoredPosition = new Vector2(0f, 210f);
            trt.sizeDelta        = new Vector2(420f, 90f);

            _toggleBg = toggleGo.AddComponent<Image>();
            _toggleBg.color = new Color(0.25f, 0.25f, 0.25f, 0.95f);

            _toggleButton = toggleGo.AddComponent<Button>();
            _toggleButton.targetGraphic = _toggleBg;
            _toggleButton.onClick.AddListener(OnToggleClicked);

            var tLabelGo = new GameObject("ToggleLabel");
            tLabelGo.transform.SetParent(toggleGo.transform, false);
            var tlrt = tLabelGo.AddComponent<RectTransform>();
            tlrt.anchorMin = Vector2.zero;
            tlrt.anchorMax = Vector2.one;
            tlrt.offsetMin = tlrt.offsetMax = Vector2.zero;

            _toggleLabel = tLabelGo.AddComponent<Text>();
            _toggleLabel.text      = "ROTATE: OFF";
            _toggleLabel.alignment = TextAnchor.MiddleCenter;
            _toggleLabel.color     = Color.white;
            _toggleLabel.fontSize  = 36;
            _toggleLabel.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _toggleLabel.raycastTarget = false;

            RefreshToggleVisuals();
        }

        private void OnToggleClicked()
        {
            _rotationEnabled = !_rotationEnabled;
            if (!_rotationEnabled && _dragging) StopDrag();
            RefreshToggleVisuals();
            Debug.Log($"[HandTableRotator] Rotation {(_rotationEnabled ? "ENABLED" : "DISABLED")}.");
        }

        private void RefreshToggleVisuals()
        {
            if (_toggleBg != null)
            {
                _toggleBg.color = _rotationEnabled
                    ? new Color(0.20f, 0.55f, 0.95f, 0.95f)  // blue when on
                    : new Color(0.25f, 0.25f, 0.25f, 0.95f); // grey when off
            }
            if (_toggleLabel != null)
            {
                _toggleLabel.text = _rotationEnabled ? "ROTATE: ON" : "ROTATE: OFF";
            }
        }

        private void UpdateRotationDisplay()
        {
            if (_rotationText == null) return;

            string state;
            string values;
            if (_tapToPlace == null || _tapToPlace.CurrentInstance == null)
            {
                state  = "(no table spawned)";
                values = "X: -  Y: -  Z: -";
            }
            else
            {
                Vector3 e = _tapToPlace.CurrentInstance.transform.rotation.eulerAngles;
                e.x = NormalizeAngle(e.x);
                e.y = NormalizeAngle(e.y);
                e.z = NormalizeAngle(e.z);
                state = !_rotationEnabled
                    ? "(disabled — toggle ON to rotate)"
                    : (_dragging
                        ? (_twoFingerActive ? "TOUCH (2 fingers)" : "TOUCH (1 finger)")
                        : "READY  —  drag on screen to rotate");
                values = $"X: {e.x,7:F2}   Y: {e.y,7:F2}   Z: {e.z,7:F2}";
            }
            _rotationText.text = $"ROTATION  {values}\n[{state}]";

            if (_rotationBg != null)
            {
                _rotationBg.color = _dragging
                    ? new Color(0.10f, 0.45f, 0.10f, 0.80f)
                    : new Color(0.00f, 0.00f, 0.00f, 0.65f);
            }
        }

        private static float NormalizeAngle(float a)
        {
            a %= 360f;
            if (a > 180f)  a -= 360f;
            if (a < -180f) a += 360f;
            return a;
        }
    }
}
