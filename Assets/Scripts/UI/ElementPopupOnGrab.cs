// Assets/Scripts/UI/ElementPopupOnGrab.cs
//
// When the user picks up an element cube from the spawned periodic table —
// either by touch-grab (XRGrabInteractable.selectEntered) or hand-pinch
// (CubeHandGrabbable.Grabbed) — show the corresponding ElementInfoCard
// and Bohr model. When the cube is released, hide them.
//
// The card and Bohr model are populated through the existing
// ElementLoader.LoadElement(int atomicNumber) — same code path as the tap
// info card you already have, so the visuals match.
//
// Self-bootstrapping; no scene wiring required.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using PeriodicAR.AR;
using PeriodicAR.AR.HandTracking;

namespace PeriodicAR.UI
{
    public class ElementPopupOnGrab : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<ElementPopupOnGrab>() != null) return;

            var go = new GameObject("[ElementPopupOnGrab]");
            DontDestroyOnLoad(go);
            go.AddComponent<ElementPopupOnGrab>();
        }

        // ---------- Cached references --------------------------------------------
        private TapToPlace      _placer;
        private ElementLoader   _loader;
        private ElementInfoCard _card;
        private GameObject      _atomGeneratorRoot;

        // Original (home) transform info so we can restore on release.
        private Transform   _cardHomeParent;
        private Vector3     _cardHomeLocalPos;
        private Quaternion  _cardHomeLocalRot;
        private Vector3     _cardHomeLocalScale;
        private bool        _cardHomeCaptured;

        private Transform   _atomHomeParent;
        private Vector3     _atomHomeLocalPos;
        private Quaternion  _atomHomeLocalRot;
        private Vector3     _atomHomeLocalScale;
        private bool        _atomHomeCaptured;

        // The cube currently anchoring the popup (latest grabbed one).
        private Transform   _currentAnchor;

        // Sideways offset (in metres along the camera's right vector) for the
        // card and the Bohr model. They float on opposite sides of the cube
        // close enough to feel attached to it but with the hand still readable
        // in the middle.
        private const float SideOffsetMeters = 0.10f;
        private const float CardVerticalLift = 0.03f;
        private const float AtomVerticalLift = 0.00f;

        // World-space scale we want the card and the Bohr model to display at
        // while anchored — small enough to comfortably read at hand distance.
        // Card root localScale of 0.05 × the canvas's inner 0.002 × 700 sizeDelta = 7 cm wide.
        private const float CardWorldRootScale = 0.05f;

        // Bohr model: the AtomGenerator has its own ApplyAdaptiveScale that
        // overwrites transform.localScale on every GenerateAtom call AND can
        // be triggered by external code paths (e.g. ElementTileSelector taps
        // before the user grabs anything) using the default masterScale. So
        // we don't trust masterScale alone — we ALSO force-clamp the atom's
        // transform.localScale every frame while a cube is held. With the
        // scene's nucleusRadius=0.025, shellSpacing=0.2 and proton prefab
        // localScale=0.04, a clamp of 0.04 yields:
        //   • protons ≈ 0.04 × 0.04 × 0.5(mesh) = 0.8 mm radius dot
        //   • nucleus cluster (14 nucleons) ≈ 0.025 × 14^0.32 × 0.04 ≈ 2.3 mm
        //   • outer shell (6 shells) ≈ 0.2 × 6 × 0.04 = 4.8 cm
        // Comfortably hand-sized.
        private const float AtomMasterScale     = 0.04f; // hint to AtomGenerator
        private const float AtomFinalLocalScale = 0.04f; // hard clamp every frame

        // Tracks the currently spawned table so we know when to re-hook.
        private GameObject _hookedTable;

        // Tracks which cubes are currently held so we can stop/start cleanly.
        private readonly HashSet<int> _heldAtomicNumbers = new();

        // Each hook stored so we can unsubscribe when the table is destroyed.
        private struct Hook
        {
            public XRGrabInteractable        grab;
            public CubeHandGrabbable         handGrab;
            public int                       atomicNumber;
            public Transform                 cubeTransform;
            public UnityAction<SelectEnterEventArgs> onGrabEnter;
            public UnityAction<SelectExitEventArgs>  onGrabExit;
            public System.Action             onHandGrab;
            public System.Action             onHandRelease;
        }
        private readonly List<Hook> _hooks = new();

        // ---------- Per-frame ----------------------------------------------------
        private void Update()
        {
            // Lazy-resolve scene singletons.
            if (_placer == null) _placer = FindAnyObjectByType<TapToPlace>();
            if (_loader == null) _loader = FindAnyObjectByType<ElementLoader>();
            if (_card   == null) _card   = FindAnyObjectByType<ElementInfoCard>(FindObjectsInactive.Include);
            if (_atomGeneratorRoot == null && _loader != null && _loader.atomGenerator != null)
                _atomGeneratorRoot = _loader.atomGenerator.gameObject;

            // Capture original parents/local transforms so we can restore on release.
            if (!_cardHomeCaptured && _card != null)
            {
                var ct = _card.transform;
                _cardHomeParent     = ct.parent;
                _cardHomeLocalPos   = ct.localPosition;
                _cardHomeLocalRot   = ct.localRotation;
                _cardHomeLocalScale = ct.localScale;
                _cardHomeCaptured   = true;
            }
            if (!_atomHomeCaptured && _atomGeneratorRoot != null)
            {
                var at = _atomGeneratorRoot.transform;
                _atomHomeParent     = at.parent;
                _atomHomeLocalPos   = at.localPosition;
                _atomHomeLocalRot   = at.localRotation;
                _atomHomeLocalScale = at.localScale;
                _atomHomeCaptured   = true;
            }

            // While a cube is held, keep the card and Bohr model floating on
            // either side of it (in world space) and face the card at the camera.
            UpdateAnchorPositioning();

            // Detect spawn / despawn / re-spawn of the table.
            var current = (_placer != null) ? _placer.CurrentInstance : null;
            if (current != _hookedTable)
            {
                Unhook();
                _hookedTable = current;
                if (current != null) HookAll(current);
                // When the whole table changes, drop any popup that was open.
                _heldAtomicNumbers.Clear();
                ApplyVisibility();
            }
        }

        private void OnDisable()
        {
            Unhook();
        }

        // ---------- Hook / unhook -------------------------------------------------
        private void HookAll(GameObject table)
        {
            int hooked = 0;

            // Touch-drag grab interactables (one per element cube).
            var grabs = table.GetComponentsInChildren<XRGrabInteractable>(true);
            foreach (var grab in grabs)
            {
                int z = ResolveAtomicNumberFor(grab.transform);
                if (z <= 0) continue;
                int localZ = z;
                Transform localCube = grab.transform;

                UnityAction<SelectEnterEventArgs> onEnter = (_) => OnGrabbed(localZ, localCube);
                UnityAction<SelectExitEventArgs>  onExit  = (_) => OnReleased(localZ);

                grab.selectEntered.AddListener(onEnter);
                grab.selectExited.AddListener(onExit);

                _hooks.Add(new Hook
                {
                    grab = grab, atomicNumber = z, cubeTransform = localCube,
                    onGrabEnter = onEnter, onGrabExit = onExit,
                });
                hooked++;
            }

            // Hand-pinch grab via CubeHandGrabbable.
            var hands = table.GetComponentsInChildren<CubeHandGrabbable>(true);
            foreach (var hand in hands)
            {
                int z = ResolveAtomicNumberFor(hand.transform);
                if (z <= 0) continue;
                int localZ = z;
                Transform localCube = hand.transform;

                System.Action onHGrab = () => OnGrabbed(localZ, localCube);
                System.Action onHRel  = () => OnReleased(localZ);
                hand.Grabbed  += onHGrab;
                hand.Released += onHRel;

                _hooks.Add(new Hook
                {
                    handGrab = hand, atomicNumber = z, cubeTransform = localCube,
                    onHandGrab = onHGrab, onHandRelease = onHRel,
                });
                hooked++;
            }

            Debug.Log($"[ElementPopupOnGrab] Hooked {hooked} grab sources on the spawned table.");
        }

        private void Unhook()
        {
            foreach (var h in _hooks)
            {
                if (h.grab != null)
                {
                    if (h.onGrabEnter != null) h.grab.selectEntered.RemoveListener(h.onGrabEnter);
                    if (h.onGrabExit  != null) h.grab.selectExited .RemoveListener(h.onGrabExit );
                }
                if (h.handGrab != null)
                {
                    if (h.onHandGrab    != null) h.handGrab.Grabbed  -= h.onHandGrab;
                    if (h.onHandRelease != null) h.handGrab.Released -= h.onHandRelease;
                }
            }
            _hooks.Clear();
        }

        // ---------- Element resolution -------------------------------------------
        // Walks up from the grabbed transform to find an ElementTile, or falls back
        // to GameObject-name matching via PeriodicTableLayout.
        private static int ResolveAtomicNumberFor(Transform t)
        {
            for (var cur = t; cur != null; cur = cur.parent)
            {
                int z = PeriodicTableLayout.ResolveAtomicNumber(cur);
                if (z > 0) return z;
            }
            return 0;
        }

        // ---------- Grab / release handlers --------------------------------------
        private void OnGrabbed(int atomicNumber, Transform cube)
        {
            _heldAtomicNumbers.Add(atomicNumber);

            // Resolve any newly-instanced singletons before we anchor.
            if (_loader == null) _loader = FindAnyObjectByType<ElementLoader>();
            if (_card   == null) _card   = FindAnyObjectByType<ElementInfoCard>(FindObjectsInactive.Include);
            if (_atomGeneratorRoot == null && _loader != null && _loader.atomGenerator != null)
                _atomGeneratorRoot = _loader.atomGenerator.gameObject;

            // Anchor the card and Bohr model to the cube *before* loading the
            // element data so they pop into existence already attached to the hand.
            AnchorPopupTo(cube);
            _currentAnchor = cube;

            if (_loader != null) _loader.LoadElement(atomicNumber);

            // Belt-and-suspenders: clamp localScale immediately after GenerateAtom
            // runs. UpdateAnchorPositioning will keep clamping it every frame, but
            // overriding here too prevents a single-frame "huge atom" flash.
            if (_loader != null && _loader.atomGenerator != null)
            {
                _loader.atomGenerator.transform.localScale = Vector3.one * AtomFinalLocalScale;
            }

            Debug.Log($"[ElementPopupOnGrab] Grabbed Z={atomicNumber} on '{cube?.name}' → atom localScale={_loader?.atomGenerator?.transform?.localScale}, masterScale={_loader?.atomGenerator?.masterScale}.");
        }

        private void OnReleased(int atomicNumber)
        {
            _heldAtomicNumbers.Remove(atomicNumber);
            ApplyVisibility();
            Debug.Log($"[ElementPopupOnGrab] Released Z={atomicNumber}. Held count={_heldAtomicNumbers.Count}.");
        }

        // Anchor info card and Bohr model so they spawn near the held cube and
        // follow its position. We DON'T re-parent — that would cascade scales
        // through the cube's transform (the InfoCard has a deeply-nested 0.002
        // canvas scale, and AtomGenerator.ApplyAdaptiveScale overwrites the
        // atom's localScale on every GenerateAtom call). Instead we drive both
        // transforms in world space each frame from UpdateAnchorPositioning().
        private void AnchorPopupTo(Transform cube)
        {
            if (cube == null) return;

            // Force the card root to a sensible visible scale (the original was
            // set up for far-distance viewing).
            if (_card != null)
            {
                _card.transform.localScale = Vector3.one * CardWorldRootScale;
            }

            // Drive the Bohr-model size through masterScale so ApplyAdaptiveScale
            // doesn't fight us. ElementLoader.LoadElement → SetElementData →
            // GenerateAtom uses this multiplier.
            if (_loader != null && _loader.atomGenerator != null)
            {
                _loader.atomGenerator.masterScale = AtomMasterScale;
            }
        }

        // Each frame, while a cube is anchored, place the card and the Bohr
        // model on opposite sides of the cube in world space and face the card
        // at the camera so the text stays readable.
        private void UpdateAnchorPositioning()
        {
            if (_currentAnchor == null) return;

            var cam = Camera.main;
            // Right and up vectors as viewed by the camera. If no camera yet,
            // fall back to world axes — better than NaN.
            Vector3 right = (cam != null) ? cam.transform.right : Vector3.right;
            Vector3 up    = (cam != null) ? cam.transform.up    : Vector3.up;

            Vector3 anchorWorld = _currentAnchor.position;

            // Card on the cube's right (camera-relative), Bohr model on the left.
            if (_card != null && _card.gameObject.activeSelf)
            {
                _card.transform.position =
                    anchorWorld + right * SideOffsetMeters + up * CardVerticalLift;

                if (cam != null)
                {
                    // Billboard: this card's text is rendered on the side
                    // OPPOSITE to local +Z (i.e., the camera should look at
                    // the canvas's -Z face). So local +Z points AWAY from the
                    // camera. With (card - camera) as the forward arg,
                    // LookRotation aligns +Z away from the camera, putting the
                    // text-side toward the user.
                    Vector3 awayFromCam = _card.transform.position - cam.transform.position;
                    if (awayFromCam.sqrMagnitude > 0.0001f)
                        _card.transform.rotation = Quaternion.LookRotation(awayFromCam, Vector3.up);
                }
            }
            if (_atomGeneratorRoot != null)
            {
                _atomGeneratorRoot.transform.position =
                    anchorWorld - right * SideOffsetMeters + up * AtomVerticalLift;
                // Hard clamp the atom's localScale every frame. AtomGenerator's
                // ApplyAdaptiveScale runs once per GenerateAtom call and uses
                // masterScale × adaptive — but other code paths (e.g. tap-to-load
                // through ElementTileSelector → ElementLoader.LoadElement) may
                // call GenerateAtom with the default masterScale of 1, blowing
                // the atom up. Clamping here makes the atom's size deterministic
                // for as long as we're anchored.
                _atomGeneratorRoot.transform.localScale = Vector3.one * AtomFinalLocalScale;
            }
        }

        // Restore card + Bohr model to their home parents/transforms.
        // (We didn't re-parent, but we did mutate localScale and the atom's
        //  world position, so reset both.)
        private void DetachPopup()
        {
            if (_card != null && _cardHomeCaptured)
            {
                var ct = _card.transform;
                ct.SetParent(_cardHomeParent, worldPositionStays: false);
                ct.localPosition = _cardHomeLocalPos;
                ct.localRotation = _cardHomeLocalRot;
                ct.localScale    = _cardHomeLocalScale;
            }
            if (_atomGeneratorRoot != null && _atomHomeCaptured)
            {
                var at = _atomGeneratorRoot.transform;
                at.SetParent(_atomHomeParent, worldPositionStays: false);
                at.localPosition = _atomHomeLocalPos;
                at.localRotation = _atomHomeLocalRot;
                at.localScale    = _atomHomeLocalScale;
            }
            _currentAnchor = null;
        }

        // Call after any state change. Hides card + Bohr model when nothing's held.
        private void ApplyVisibility()
        {
            if (_heldAtomicNumbers.Count == 0)
            {
                if (_card != null) _card.HideCard();
                if (_loader != null && _loader.atomGenerator != null) _loader.atomGenerator.ClearAtom();
                DetachPopup();
            }
        }
    }
}
