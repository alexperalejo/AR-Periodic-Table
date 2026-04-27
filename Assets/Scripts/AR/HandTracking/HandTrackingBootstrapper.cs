// Assets/Scripts/AR/HandTracking/HandTrackingBootstrapper.cs
//
// Forces the scene's AROcclusionManager into the configuration that the
// RealtimeHand package requires:
//
//   environmentDepthMode               = Fastest
//   humanSegmentationStencilMode       = Fastest
//   humanSegmentationDepthMode         = Fastest
//   environmentDepthTemporalSmoothing  = OFF
//
// RealtimeHand's per-frame Process() bails silently if either CPUEnvironmentDepth
// or CPUHumanStencil come back null from AROcclusionManager — and the human
// stencil one is the gotcha, because it's not on by default. Without this script,
// every Inspector edit risks turning hand tracking off again.
//
// Self-bootstrapping; no scene wiring required.
//
// To remove later: delete this file (and re-enable Human Segmentation manually
// on the AROcclusionManager component).

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace PeriodicAR.AR.HandTracking
{
    public class HandTrackingBootstrapper : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<HandTrackingBootstrapper>() != null) return;

            var go = new GameObject("[HandTrackingBootstrapper]");
            DontDestroyOnLoad(go);
            go.AddComponent<HandTrackingBootstrapper>();
        }

        private AROcclusionManager _occlusion;
        private bool _requested;     // we've issued the requested* setters
        private bool _confirmed;     // the descriptor is up and we've logged the actual modes

        private void Update()
        {
            if (_confirmed) return;

            // ---------- 1. Find the occlusion manager --------------------------------
            if (_occlusion == null)
            {
                _occlusion = FindAnyObjectByType<AROcclusionManager>();
                if (_occlusion == null) return;
                Debug.Log($"[HandTrackingBootstrapper] Found AROcclusionManager on '{_occlusion.gameObject.name}'.");
            }

            // ---------- 2. Issue the requested-mode setters --------------------------
            // These are safe to call before the subsystem is up — the requests are
            // stored and applied as soon as the descriptor becomes available.
            if (!_requested)
            {
                _occlusion.requestedEnvironmentDepthMode  = EnvironmentDepthMode.Fastest;
                _occlusion.requestedHumanStencilMode      = HumanSegmentationStencilMode.Fastest;
                _occlusion.requestedHumanDepthMode        = HumanSegmentationDepthMode.Fastest;
                _occlusion.environmentDepthTemporalSmoothingRequested = false;

                Debug.Log("[HandTrackingBootstrapper] Requested " +
                          "envDepth=Fastest, humanStencil=Fastest, humanDepth=Fastest, smoothing=Off.");
                _requested = true;
            }

            // ---------- 3. Wait for the subsystem to come online ---------------------
            // The descriptor is null until ARSession actually starts producing frames.
            var d = _occlusion.descriptor;
            if (d == null) return;

            // ---------- 4. Verify support and log the actual configuration -----------
            string envSup    = d.environmentDepthImageSupported.ToString();
            string stencilSup = d.humanSegmentationStencilImageSupported.ToString();
            string depthSup   = d.humanSegmentationDepthImageSupported.ToString();
            string envCur    = _occlusion.currentEnvironmentDepthMode.ToString();
            string stencilCur = _occlusion.currentHumanStencilMode.ToString();
            string depthCur   = _occlusion.currentHumanDepthMode.ToString();

            Debug.Log("[HandTrackingBootstrapper] Subsystem online. " +
                      $"envDepthSupported={envSup} (current={envCur}), " +
                      $"humanStencilSupported={stencilSup} (current={stencilCur}), " +
                      $"humanDepthSupported={depthSup} (current={depthCur}).");

            if (d.humanSegmentationStencilImageSupported != Supported.Supported)
                Debug.LogError("[HandTrackingBootstrapper] Device reports humanSegmentationStencil NOT supported. RealtimeHand requires it; hand tracking will not work on this device.");
            else if (_occlusion.currentHumanStencilMode == HumanSegmentationStencilMode.Disabled)
                Debug.LogWarning("[HandTrackingBootstrapper] humanStencil supported but currently Disabled — request may not have applied yet, will keep checking.");

            _confirmed = true;
        }

        // ---------- Public read-only state for the diagnostic pill ----------------
        public AROcclusionManager OcclusionManager => _occlusion;

        public string DescribeOcclusionState()
        {
            if (_occlusion == null) return "no AROcclusionManager";
            var d = _occlusion.descriptor;
            if (d == null) return "subsystem starting";
            if (d.humanSegmentationStencilImageSupported != Supported.Supported) return "device lacks human stencil";
            if (_occlusion.currentHumanStencilMode == HumanSegmentationStencilMode.Disabled) return "stencil request pending";
            return "ready";
        }
    }
}
