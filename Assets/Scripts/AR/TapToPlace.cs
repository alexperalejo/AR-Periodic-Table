using System;
using System.Collections.Generic;
using PeriodicAR.UI;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace PeriodicAR.AR
{
    public class TapToPlace : MonoBehaviour
    {
        public enum State { Idle, Armed, Placed }

        [Header("Placement")]
        [SerializeField] private GameObject placePrefab;
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Camera arCamera;
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARPlaneManager planeManager;

        [Header("Placement geometry")]
        [Tooltip("Absolute uniform scale applied to the spawned table.")]
        [SerializeField] private float absoluteScale = 0.4f;
        [Tooltip("Height offset above the tapped plane (in meters).")]
        [SerializeField] private float heightOffset = 1.2f;
        [Tooltip("World-space Euler rotation applied to the spawned object at placement time. Default values are the orientation that looks correct for pTableGroup (read off HandTableRotator's pill on a real device). Tweak here if you find a better orientation manually in the scene.")]
        [SerializeField] private Vector3 spawnRotationEuler = new Vector3(51.48f, -18.07f, 3.79f);
        [Tooltip("If true, the table will be rotated around the Y axis to face the camera when placed.")]
        [SerializeField] private bool faceCameraOnPlacement = true;
        [Tooltip("DEPRECATED — kept only for legacy serialization. New rotation logic uses spawnRotationEuler instead.")]
        [SerializeField] private Vector3 extraRotationEuler = new Vector3(0f, 0f, 0f);

        [Header("Behaviour")]
        [Tooltip("If true, tapping a plane while already placed will MOVE the table to that point. Cannot be triggered if the tap hit a 3D collider (e.g. an element cube).")]
        [SerializeField] private bool allowRepositionOnTap = true;
        [Tooltip("Tap is rejected if held longer than this many seconds (filters out drags).")]
        [SerializeField] private float tapMaxDurationSeconds = 0.5f;
        [Tooltip("Tap is rejected if the finger moved more than this many pixels (filters out drags).")]
        [SerializeField] private float tapMaxMovementPixels = 25f;

        [Header("Auto-fit (defends against broken prefab layouts)")]
        [Tooltip("After spawning, translate the table so its renderer bounds center sits exactly on the tap point. Prevents huge baked-in transform offsets from putting the table out of view.")]
        [SerializeField] private bool autoCenterOnTap = true;
        [Tooltip("After spawning, if the table's largest bounding-box dimension is bigger than this (in meters), scale it down to match. 0 disables auto-fit.")]
        [SerializeField] private float autoFitMaxDimensionMeters = 0.6f;
        [Tooltip("DESTRUCTIVE: resets all direct child local rotations to identity. Breaks the element grid layout in pTableGroup because the children's positions were authored assuming the parent's stored rotation. Leave OFF; only enable for prefabs where you know the grid doesn't depend on the rotation.")]
        [SerializeField] private bool resetChildRotations = false;
        [Tooltip("DESTRUCTIVE: re-layout the element children into a flat textbook grid by atomic number. Disabled by default — it rewrote the prefab geometry in a way that didn't match the source layout.")]
        [SerializeField] private bool forceFlatLayout = false;
        [Tooltip("Only matters when forceFlatLayout is on. Disabled by default along with forceFlatLayout.")]
        [SerializeField] private bool flatLayoutFaceCamera = false;

        [Header("Diagnostics")]
        [Tooltip("Show a coloured ring at the tap location for debugging. White=tap detected, Red=blocked by UI, Orange=blocked by 3D interactable, Yellow=missed plane, Green=placed.")]
        [SerializeField] private bool showTapIndicator = true;
        [Tooltip("Verbose Debug.Log for every press, every filter rejection, and every placement.")]
        [SerializeField] private bool verboseLogging = true;

        public State CurrentState { get; private set; } = State.Idle;
        public GameObject CurrentInstance => _spawned;
        public bool HasSpawned => _spawned != null;

        // ---- Lock state ----------------------------------------------------------------
        private bool _tableLocked = false;
        public bool IsLocked => _tableLocked;

        /// <summary>Fires when the lock state changes (true = locked, false = unlocked).</summary>
        public event Action<bool> LockChanged;

        /// <summary>Prevent repositioning taps while the table is placed.</summary>
        public void LockTable()
        {
            if (!HasSpawned) { Debug.LogWarning("[TapToPlace] LockTable called but no table is placed."); return; }
            _tableLocked = true;
            LockChanged?.Invoke(true);
            Debug.Log("[TapToPlace] Table LOCKED — placement taps will be ignored.");
        }

        /// <summary>Re-allow repositioning after the table was locked.</summary>
        public void UnlockTable()
        {
            _tableLocked = false;
            LockChanged?.Invoke(false);
            Debug.Log("[TapToPlace] Table UNLOCKED — repositioning enabled.");
        }

        /// <summary>
        /// The Transform of the currently placed periodic table, or null if no table is placed.
        /// Prefer subscribing to <see cref="TablePlaced"/>/<see cref="TableRemoved"/> for
        /// event-driven updates; use this property for one-shot queries.
        /// </summary>
        public Transform PlacedTransform => _spawned != null ? _spawned.transform : null;

        public event Action<State> StateChanged;
        // Fires every time the next placement override is set or cleared (true=armed, false=cleared).
        public event Action<bool> PlacementOverrideChanged;

        /// <summary>
        /// Fires immediately after a new table is placed (or repositioned).
        /// The argument is the spawned table's Transform — never null when this fires.
        /// </summary>
        public event Action<Transform> TablePlaced;

        /// <summary>
        /// Fires just before the current table is destroyed (via RemoveTable or reposition).
        /// _spawned is still valid at the moment this fires.
        /// </summary>
        public event Action TableRemoved;

        // One-shot override. When non-null, the next placement will use this prefab/GameObject
        // INSTEAD of placePrefab. Cleared automatically after one placement. Auto-fit and
        // auto-center are skipped for overrides — they're meant for raw test geometry.
        private GameObject _oneShotOverride;
        public bool IsOverrideArmed => _oneShotOverride != null;

        /// <summary>
        /// Arm a one-shot placement override. The next successful plane tap will spawn
        /// <paramref name="prefab"/> instead of the default placePrefab, then the override
        /// clears itself. Pass null to cancel a pending override.
        /// </summary>
        public void SetOneShotPlacementOverride(GameObject prefab)
        {
            _oneShotOverride = prefab;
            PlacementOverrideChanged?.Invoke(prefab != null);
            if (verboseLogging)
                Debug.Log($"[TapToPlace] One-shot override {(prefab != null ? "ARMED with " + prefab.name : "CLEARED")}.");
        }

        private GameObject _spawned;
        private readonly List<ARRaycastHit> _hits = new List<ARRaycastHit>();

        // Tap detection state
        private bool    _pressActive;
        private float   _pressStartTime;
        private Vector2 _pressStartPos;
        private bool    _enhancedTouchEnabledByUs;

        private void Awake()
        {
            if (arCamera == null && xrOrigin != null) arCamera = xrOrigin.Camera;
            if (arCamera == null) arCamera = Camera.main;

            // Try to discover dependencies if the inspector references were lost.
            if (raycastManager == null && xrOrigin != null)
                raycastManager = xrOrigin.GetComponentInChildren<ARRaycastManager>();
            if (raycastManager == null)
                raycastManager = FindAnyObjectByType<ARRaycastManager>();

            if (planeManager == null && xrOrigin != null)
                planeManager = xrOrigin.GetComponentInChildren<ARPlaneManager>();
            if (planeManager == null)
                planeManager = FindAnyObjectByType<ARPlaneManager>();
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
            if (raycastManager == null)
            {
                // Try once more in case AR rig spawned later.
                raycastManager = FindAnyObjectByType<ARRaycastManager>();
                if (raycastManager == null) return;
            }

            // --- Sample touch / mouse with phase tracking so we only act on a clean tap. ---
            bool began = false, ended = false;
            Vector2 pos = _pressStartPos;

            // Touch (iOS device)
            if (Touch.activeTouches.Count > 0)
            {
                var t = Touch.activeTouches[0];
                pos = t.screenPosition;
                if (t.phase == UnityEngine.InputSystem.TouchPhase.Began) began = true;
                if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                    t.phase == UnityEngine.InputSystem.TouchPhase.Canceled) ended = true;
            }
            // Fallback: Touchscreen primary touch
            else if (Touchscreen.current != null)
            {
                var primary = Touchscreen.current.primaryTouch;
                if (primary.press.wasPressedThisFrame) { began = true; pos = primary.position.ReadValue(); }
                else if (primary.press.wasReleasedThisFrame) { ended = true; pos = primary.position.ReadValue(); }
            }
            // Mouse (Editor)
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    began = true;
                    pos = Mouse.current.position.ReadValue();
                }
                else if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    ended = true;
                    pos = Mouse.current.position.ReadValue();
                }
            }

            if (began)
            {
                _pressActive = true;
                _pressStartTime = Time.unscaledTime;
                _pressStartPos = pos;

                // Diagnostic: ALWAYS show feedback for every detected press, BEFORE any filter.
                // If you don't see this ring, the tap never reached this script.
                if (showTapIndicator) TapIndicator.Show(pos, Color.white);
                if (verboseLogging)   Debug.Log($"[TapToPlace] Press began at {pos}. State={CurrentState}");
                return;
            }

            if (!ended || !_pressActive) return;
            _pressActive = false;

            float duration = Time.unscaledTime - _pressStartTime;
            float movement = Vector2.Distance(_pressStartPos, pos);
            if (duration > tapMaxDurationSeconds || movement > tapMaxMovementPixels)
            {
                if (verboseLogging) Debug.Log($"[TapToPlace] Press rejected as drag/long-press. duration={duration:F2}s movement={movement:F0}px");
                return;
            }

            // Guard: suppress table placement while the user is dragging an AR world object.
            if (ARWorldObjectManipulator.AnyDragging)
            {
                if (verboseLogging) Debug.Log("[TapToPlace] Tap suppressed: ARWorldObjectManipulator drag is active.");
                return;
            }

            // Use the press-start position so we don't get a slightly drifted release point.
            Vector2 screen = _pressStartPos;

            // --- Filter: ignore taps that landed on UI controls (button etc.) ---
            if (IsOverInteractiveUI(screen))
            {
                if (showTapIndicator) TapIndicator.Show(screen, new Color(1f, 0.25f, 0.25f, 1f)); // red
                if (verboseLogging)   Debug.Log($"[TapToPlace] Tap blocked: hit a UI Selectable at {screen}.");
                return;
            }

            // --- Filter: ignore taps that hit an existing element cube / interactable.
            //     ElementTileSelector handles those for the info card. ---
            if (HitsExistingInteractive(screen))
            {
                if (showTapIndicator) TapIndicator.Show(screen, new Color(1f, 0.55f, 0f, 1f)); // orange
                if (verboseLogging)   Debug.Log($"[TapToPlace] Tap blocked: hit a 3D interactable at {screen}.");
                return;
            }

            // --- Plane raycast. ---
            // PlaneWithinPolygon is strict; fall back to PlaneWithinBounds, then any Plane,
            // because some iOS devices report imprecise polygons early on.
            bool didHit =
                raycastManager.Raycast(screen, _hits, TrackableType.PlaneWithinPolygon) ||
                raycastManager.Raycast(screen, _hits, TrackableType.PlaneWithinBounds)  ||
                raycastManager.Raycast(screen, _hits, TrackableType.Planes);

            if (!didHit || _hits.Count == 0)
            {
                if (showTapIndicator) TapIndicator.Show(screen, new Color(1f, 0.95f, 0.2f, 1f)); // yellow
                int planeCount = planeManager != null ? CountPlanes() : -1;
                Debug.Log($"[TapToPlace] Tap missed: no plane hit at {screen}. State={CurrentState}. Tracked planes={planeCount}.");
                return;
            }

            // Honour state machine semantics:
            //   Idle  → place (auto-arm so we don't strand the user without a HUD)
            //   Armed → place
            //   Placed → reposition if allowed and not locked, otherwise ignore
            if (CurrentState == State.Placed && !allowRepositionOnTap) return;

            // Lock guard: ignore repositioning taps when the table is locked.
            if (CurrentState == State.Placed && _tableLocked)
            {
                if (verboseLogging) Debug.Log("[TapToPlace] Tap ignored: table is LOCKED.");
                return;
            }

            PlaceAt(_hits[0].pose);
        }

        public void ArmPlacement()
        {
            if (CurrentState == State.Placed) return;
            SetState(State.Armed);
        }

        public void CancelPlacement()
        {
            if (CurrentState != State.Armed) return;
            SetState(State.Idle);
        }

        public void RemoveTable()
        {
            if (_spawned != null)
            {
                TableRemoved?.Invoke();          // notify before Destroy so listeners can still read the transform
                Debug.Log("[TapToPlace] TableRemoved event fired.");
                Destroy(_spawned);
            }
            _spawned = null;
            if (_tableLocked)
            {
                _tableLocked = false;
                LockChanged?.Invoke(false);
                Debug.Log("[TapToPlace] Lock cleared because table was removed.");
            }
            SetState(State.Idle);
        }

        public void PlaceAt(Pose pose)
        {
            // Pick which prefab to spawn: one-shot override wins; otherwise the default.
            bool isOverride = _oneShotOverride != null;
            GameObject prefabToSpawn = isOverride ? _oneShotOverride : placePrefab;

            if (prefabToSpawn == null)
            {
                Debug.LogError("[TapToPlace] No prefab to place — placePrefab is unassigned and no override is set.");
                return;
            }
            if (_spawned != null)
            {
                // Table is being repositioned — fire TableRemoved so listeners can clear their refs
                // before the old instance is destroyed.
                TableRemoved?.Invoke();
                Debug.Log("[TapToPlace] TableRemoved event fired (reposition).");
                Destroy(_spawned);
            }

            // Rotation: spawn at the user-tuned spawnRotationEuler. Test cube override uses
            // identity. Reverted from the flat-layout LookRotation experiment because it
            // produced a far-below-the-floor placement.
            Quaternion rot = isOverride
                ? Quaternion.identity
                : Quaternion.Euler(spawnRotationEuler);

            if (!isOverride)
            {
                pose.position.y += heightOffset;
                
                if (faceCameraOnPlacement && arCamera != null)
                {
                    Vector3 toCamera = arCamera.transform.position - pose.position;
                    toCamera.y = 0;
                    if (toCamera.sqrMagnitude > 0.001f)
                    {
                        // The table's "front" is its -Z axis.
                        // We want its -Z axis to point towards the camera, so +Z points away.
                        float yaw = Quaternion.LookRotation(-toCamera.normalized).eulerAngles.y;
                        rot = Quaternion.Euler(spawnRotationEuler.x, yaw, spawnRotationEuler.z);
                    }
                }
            }

            _spawned = Instantiate(prefabToSpawn, pose.position, rot);
            // For overrides, do NOT mash scale or auto-fit — the caller passed exact geometry.
            // For the real prefab, apply the configured absolute scale.
            if (!isOverride) _spawned.transform.localScale = Vector3.one * absoluteScale;

            // Override is one-shot: clear it as soon as it's been consumed.
            if (isOverride)
            {
                _oneShotOverride = null;
                PlacementOverrideChanged?.Invoke(false);
                Debug.Log($"[TapToPlace] One-shot override placed: {_spawned.name} at {pose.position}.");

                // Skip the auto-fit/center pass entirely for overrides — the caller is
                // assumed to have authored their geometry at the right scale already.
                if (showTapIndicator && arCamera != null)
                {
                    Vector3 sp2 = arCamera.WorldToScreenPoint(pose.position);
                    if (sp2.z > 0f) TapIndicator.Show(new Vector2(sp2.x, sp2.y), new Color(0.2f, 1f, 0.4f, 1f));
                }
                TablePlaced?.Invoke(_spawned.transform);
                Debug.Log($"[TapToPlace] TablePlaced event fired for override '{_spawned.name}'.");
                SetState(State.Placed);
                return;
            }

            // ---- Force flat 2D layout (DISABLED — was destructive) -------------------------
            // The runtime relayout broke the prefab geometry in a way that pushed the table
            // way below the tap point. Hard-disabled until the layout math is corrected; the
            // prefab is now spawned with its authored child positions intact.
            //
            //   if (forceFlatLayout)
            //   {
            //       PeriodicTableLayout.Relayout(_spawned.transform);
            //   }

            // ---- Cancel uniform baked-in tilt ----------------------------------------------
            // Every direct child of pTableGroup carries the SAME m_LocalRotation override
            // (≈ -49°,26°,-13°), which is what produced the diagonal-into-the-floor look even
            // after the root rotation was clean. Resetting direct children to identity cancels
            // that uniform tilt without disturbing each element prefab's internal hierarchy
            // (so the cube + label + scripts inside each element are still intact).
            // Note: forceFlatLayout already does this as part of repositioning.
            if (resetChildRotations && !forceFlatLayout)
            {
                int reset = 0;
                foreach (Transform child in _spawned.transform)
                {
                    child.localRotation = Quaternion.identity;
                    reset++;
                }
                Debug.Log($"[TapToPlace] Reset localRotation on {reset} direct children to identity.");
            }

            // ---- Auto-fit and auto-center ---------------------------------------------------
            // The pTableGroup prefab has its element children positioned at very large local
            // coordinates (tens of meters), so even with absoluteScale = 0.3 the visible content
            // ends up far from the tap point and likely huge. Force it to behave: if too big,
            // scale it down; if off-center, translate it back onto the tap point.
            var renderers = _spawned.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[TapToPlace] Placed at {pose.position} but spawned object has NO renderers. Prefab is likely empty or all visuals are gated off.");
            }
            else
            {
                Bounds RecomputeBounds()
                {
                    var bb = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) bb.Encapsulate(renderers[i].bounds);
                    return bb;
                }

                Bounds b = RecomputeBounds();
                Debug.Log($"[TapToPlace] Pre-fit: renderers={renderers.Length}, " +
                          $"bounds.center={b.center}, bounds.size={b.size}, " +
                          $"distance(tap→center)={Vector3.Distance(pose.position, b.center):F2}m.");

                // Auto-fit: scale down so the largest dimension matches the requested max.
                if (autoFitMaxDimensionMeters > 0f)
                {
                    float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                    if (maxDim > autoFitMaxDimensionMeters)
                    {
                        float factor = autoFitMaxDimensionMeters / maxDim;
                        _spawned.transform.localScale *= factor;
                        Debug.Log($"[TapToPlace] Auto-fit: max dim was {maxDim:F2}m, scaled by {factor:F4} → new local scale {_spawned.transform.localScale.x:F4}.");
                        b = RecomputeBounds();
                    }
                }

                // Auto-center + sit-on-floor: align bounds.center.x/z with pose.position,
                // and lift the table up so bounds.min.y == pose.position.y. This stops the
                // table from clipping through the floor — its bottom face sits on the plane.
                if (autoCenterOnTap)
                {
                    Vector3 desiredCenter = new Vector3(
                        pose.position.x,
                        pose.position.y + b.size.y * 0.5f,  // raise so the bottom touches floor
                        pose.position.z);
                    Vector3 offset = desiredCenter - b.center;
                    _spawned.transform.position += offset;
                    b = RecomputeBounds();
                    Debug.Log($"[TapToPlace] Auto-center: floor-anchored. " +
                              $"bounds.center={b.center}, bounds.min.y={b.min.y:F3}, pose.y={pose.position.y:F3}.");
                }

                // (Face-camera math removed — orientation is now driven entirely by
                //  spawnRotationEuler. If you want camera-relative facing again, this is
                //  where to compute and apply a yaw delta around bounds.center.)
            }

            if (showTapIndicator && arCamera != null)
            {
                Vector3 sp = arCamera.WorldToScreenPoint(pose.position);
                if (sp.z > 0f) TapIndicator.Show(new Vector2(sp.x, sp.y), new Color(0.2f, 1f, 0.4f, 1f)); // green
            }
            TablePlaced?.Invoke(_spawned.transform);
            Debug.Log($"[TapToPlace] Placed table at {pose.position} — TablePlaced event fired.");
            SetState(State.Placed);
        }

        // Cheap helper for diagnostics — counts how many planes ARPlaneManager
        // currently has, so a missed raycast can be distinguished from "no planes".
        private int CountPlanes()
        {
            if (planeManager == null) return -1;
            int n = 0;
            foreach (var _ in planeManager.trackables) n++;
            return n;
        }

        private void SetState(State s)
        {
            if (CurrentState == s) return;
            CurrentState = s;
            StateChanged?.Invoke(s);
        }

        // Only block the tap when it actually lands on something interactive
        // (Selectable derivatives like Button, Toggle, Slider, etc).
        // A plain transparent Image with raycastTarget=true should NOT swallow the tap.
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

        // Block taps that landed on a 3D interactable (e.g. an element cube).
        // ElementTileSelector handles those for the info card.
        private bool HitsExistingInteractive(Vector2 screenPos)
        {
            if (arCamera == null) return false;
            var ray = arCamera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return false;

            // If we hit anything that's part of the spawned periodic table, don't reposition.
            if (_spawned != null && hit.collider.transform.IsChildOf(_spawned.transform))
                return true;

            // If we hit any XR interactable (cube grab handle), let that system handle it.
            if (hit.collider.GetComponentInParent<XRBaseInteractable>() != null) return true;

            return false;
        }

        public void Reset() => RemoveTable();
    }
}
