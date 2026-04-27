// Assets/Scripts/Debug/HandJointVisualizer.cs
//
// Self-bootstrapping hand-skeleton visualizer modelled on the RealtimeHand
// sample / HoloKit reference image: solid bright-blue spheres at every joint,
// solid lines along each finger chain, dashed lines from the wrist out to
// each finger's MCP knuckle (the implied palm spine).
//
// No scene wiring required — bootstraps itself in any loaded scene.
// Includes a small status pill at top-center showing whether
// RealtimeHand is currently producing tracking updates.
//
// To remove later: delete this file. No other cleanup needed.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using RTHand;
using PeriodicAR.AR.HandTracking;

namespace PeriodicAR.Debugging
{
    public class HandJointVisualizer : MonoBehaviour
    {
        // Bootstrap re-enabled per user request. The hand-skeleton overlay
        // (joint dots + finger bones + status pill) self-instantiates after
        // every scene load, and can be toggled on/off at runtime via
        // HandOverlayToggle (which calls SetVisualizerEnabled below).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<HandJointVisualizer>() != null) return;

            var go = new GameObject("[HandJointVisualizer]");
            DontDestroyOnLoad(go);
            go.AddComponent<HandJointVisualizer>();
        }

        // ---------- Toggle API ----------------------------------------------------
        // Single point of control for the on/off button. When OFF, every
        // visual element this component owns is hidden (markers, bones,
        // status pill) but the component keeps running so the user can
        // re-enable it without losing tracking state.
        public static HandJointVisualizer Instance { get; private set; }

        private bool _visualizerOn = true;
        public bool IsVisualizerOn => _visualizerOn;

        public void SetVisualizerEnabled(bool on)
        {
            _visualizerOn = on;
            ApplyVisibility();
            Debug.Log($"[HandJointVisualizer] Overlay {(on ? "ON" : "OFF")}");
        }

        public void ToggleVisualizer() => SetVisualizerEnabled(!_visualizerOn);

        private void ApplyVisibility()
        {
            // When OFF, force every visual element off. When ON, the per-frame
            // OnHandUpdated callback will re-enable the joints/bones that have
            // valid tracking, and Update() will keep the pill text current.
            if (!_visualizerOn)
            {
                foreach (var m in _markers.Values) if (m != null) m.SetActive(false);
                foreach (var b in _solidLines)  if (b.line != null) b.line.enabled = false;
                foreach (var b in _dashedLines) if (b.line != null) b.line.enabled = false;
            }
            if (_statusBg != null)
            {
                var pillCanvas = _statusBg.canvas;
                if (pillCanvas != null) pillCanvas.gameObject.SetActive(_visualizerOn);
                else                    _statusBg.gameObject.SetActive(_visualizerOn);
            }
        }

        // ---------- Tunables (match the reference image) --------------------------
        private const float JointSize     = 0.016f; // 1.6cm spheres, uniform across all joints
        private const float SolidBoneW    = 0.005f; // 5mm solid bones along each finger chain
        private const float DashedBoneW   = 0.005f; // matching width for the palm spine
        private const float MinConfidence = 0.35f;  // hide joints below this

        private static readonly Color HandBlue = new Color(0.27f, 0.79f, 1.00f, 1.00f);

        // ---------- Hand topology -------------------------------------------------
        // Solid bones run along each finger chain and across the inter-MCP web.
        // Dashed bones are the wrist→MCP "palm spine" (the implied connections in
        // the reference image).
        private static readonly (JointName a, JointName b)[] SolidBones =
        {
            // Thumb chain
            (JointName.thumbCMC,   JointName.thumbMP),
            (JointName.thumbMP,    JointName.thumbIP),
            (JointName.thumbIP,    JointName.thumbTip),

            // Index chain
            (JointName.indexMCP,   JointName.indexPIP),
            (JointName.indexPIP,   JointName.indexDIP),
            (JointName.indexDIP,   JointName.indexTip),

            // Middle chain
            (JointName.middleMCP,  JointName.middlePIP),
            (JointName.middlePIP,  JointName.middleDIP),
            (JointName.middleDIP,  JointName.middleTip),

            // Ring chain
            (JointName.ringMCP,    JointName.ringPIP),
            (JointName.ringPIP,    JointName.ringDIP),
            (JointName.ringDIP,    JointName.ringTip),

            // Little chain
            (JointName.littleMCP,  JointName.littlePIP),
            (JointName.littlePIP,  JointName.littleDIP),
            (JointName.littleDIP,  JointName.littleTip),

            // Inter-MCP web across the top of the palm
            (JointName.indexMCP,   JointName.middleMCP),
            (JointName.middleMCP,  JointName.ringMCP),
            (JointName.ringMCP,    JointName.littleMCP),
        };

        private static readonly (JointName a, JointName b)[] DashedBones =
        {
            (JointName.wrist, JointName.thumbCMC),
            (JointName.wrist, JointName.indexMCP),
            (JointName.wrist, JointName.middleMCP),
            (JointName.wrist, JointName.ringMCP),
            (JointName.wrist, JointName.littleMCP),
        };

        // ---------- State ---------------------------------------------------------
        private RealtimeHandManager _hands;
        private readonly Dictionary<JointName, GameObject> _markers = new();
        private readonly List<(LineRenderer line, JointName a, JointName b)> _solidLines  = new();
        private readonly List<(LineRenderer line, JointName a, JointName b)> _dashedLines = new();
        private Material _jointMat, _solidMat, _dashedMat;

        private Text _statusText;
        private Image _statusBg;
        private float _lastUpdateTime = -999f;

        private void Awake()
        {
            Instance = this;

            _jointMat  = MakeUnlit(HandBlue, isTransparent: false);
            _solidMat  = MakeUnlit(HandBlue, isTransparent: false);
            _dashedMat = MakeDashedLineMaterial(HandBlue);

            BuildMarkers();
            BuildBones();
            BuildStatusPill();

            TryHookHandManager();
            Debug.Log("[HandJointVisualizer] Active. Watch the top-center pill for tracking state.");
        }

        private bool _lastHandVisible;

        private void Update()
        {
            // When the user turned the overlay off, skip all per-frame work.
            if (!_visualizerOn) return;

            if (_hands == null) TryHookHandManager();

            // Walk the pipeline from device → subsystem → session → manager → hand,
            // and surface whichever stage is the first one to fail. That stage IS the
            // problem the user needs to fix; everything downstream of it is moot.
            string label;
            Color  bg;

            var occlusion = FindAnyObjectByType<AROcclusionManager>();
            if (occlusion == null)
            {
                label = "LIDAR: NO MANAGER";       bg = ColorRed;
            }
            else if (occlusion.descriptor == null)
            {
                label = "LIDAR: STARTING";         bg = ColorGrey;
            }
            else if (occlusion.descriptor.humanSegmentationStencilImageSupported != Supported.Supported)
            {
                label = "LIDAR: NOT SUPPORTED";    bg = ColorRed;
            }
            else if (occlusion.currentHumanStencilMode == HumanSegmentationStencilMode.Disabled)
            {
                label = "LIDAR: STENCIL OFF";      bg = ColorRed;
            }
            else if (ARSession.state != ARSessionState.SessionTracking)
            {
                label = $"AR: {ARSession.state}";  bg = ColorGrey;
            }
            else if (_hands == null)
            {
                label = "HAND: NO MANAGER";        bg = ColorRed;
            }
            else if (!_hands.enabled)
            {
                label = "HAND: MGR DISABLED";      bg = ColorOrange;
            }
            else if ((Time.unscaledTime - _lastUpdateTime) > 1.0f)
            {
                label = "HAND: NO FRAMES";         bg = ColorOrange;
            }
            else if (_lastHandVisible)
            {
                label = "HAND: TRACKED";           bg = ColorGreen;
            }
            else
            {
                label = "HAND: SEARCHING";         bg = ColorBlue;
            }

            if (_statusText != null) _statusText.text = label;
            if (_statusBg   != null) _statusBg.color  = bg;
        }

        private static readonly Color ColorRed    = new Color(0.78f, 0.18f, 0.18f, 0.90f);
        private static readonly Color ColorOrange = new Color(0.95f, 0.55f, 0.10f, 0.90f);
        private static readonly Color ColorGrey   = new Color(0.35f, 0.35f, 0.35f, 0.90f);
        private static readonly Color ColorBlue   = new Color(0.20f, 0.50f, 0.90f, 0.90f);
        private static readonly Color ColorGreen  = new Color(0.18f, 0.70f, 0.28f, 0.90f);

        private void TryHookHandManager()
        {
            var found = FindAnyObjectByType<RealtimeHandManager>();
            if (found == null || found == _hands) return;
            _hands = found;
            _hands.HandUpdated += OnHandUpdated;
            Debug.Log("[HandJointVisualizer] Hooked RealtimeHandManager.");
        }

        private void OnDestroy()
        {
            if (_hands != null) _hands.HandUpdated -= OnHandUpdated;
        }

        // ---------- Per-frame update ---------------------------------------------
        private void OnHandUpdated(RealtimeHand hand)
        {
            _lastUpdateTime    = Time.unscaledTime;
            _lastHandVisible   = hand != null && hand.IsVisible;

            // Overlay disabled — keep tracking state up to date but draw nothing.
            if (!_visualizerOn)
            {
                foreach (var m in _markers.Values) m.SetActive(false);
                foreach (var b in _solidLines)  b.line.enabled = false;
                foreach (var b in _dashedLines) b.line.enabled = false;
                return;
            }

            if (hand == null || !hand.IsVisible)
            {
                foreach (var m in _markers.Values) m.SetActive(false);
                foreach (var b in _solidLines)  b.line.enabled = false;
                foreach (var b in _dashedLines) b.line.enabled = false;
                return;
            }

            foreach (var kv in _markers)
            {
                var name = kv.Key;
                var go   = kv.Value;
                if (!hand.Joints.TryGetValue(name, out var joint) ||
                    !joint.isVisible || joint.confidence < MinConfidence)
                {
                    go.SetActive(false);
                    continue;
                }
                go.SetActive(true);
                go.transform.position = joint.worldPos;
            }

            UpdateBoneList(hand, _solidLines);
            UpdateBoneList(hand, _dashedLines);
        }

        private static void UpdateBoneList(RealtimeHand hand, List<(LineRenderer line, JointName a, JointName b)> list)
        {
            foreach (var entry in list)
            {
                bool aOk = hand.Joints.TryGetValue(entry.a, out var ja) && ja.isVisible && ja.confidence >= MinConfidence;
                bool bOk = hand.Joints.TryGetValue(entry.b, out var jb) && jb.isVisible && jb.confidence >= MinConfidence;
                if (!aOk || !bOk)
                {
                    entry.line.enabled = false;
                    continue;
                }
                entry.line.enabled = true;
                entry.line.SetPosition(0, ja.worldPos);
                entry.line.SetPosition(1, jb.worldPos);
            }
        }

        // ---------- Build helpers ------------------------------------------------
        private void BuildMarkers()
        {
            foreach (JointName name in Enum.GetValues(typeof(JointName)))
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "Joint_" + name;
                sphere.transform.SetParent(transform, false);
                sphere.transform.localScale = Vector3.one * JointSize;

                var col = sphere.GetComponent<Collider>();
                if (col != null) Destroy(col);

                sphere.GetComponent<Renderer>().sharedMaterial = _jointMat;
                sphere.SetActive(false);
                _markers[name] = sphere;
            }
        }

        private void BuildBones()
        {
            foreach (var (a, b) in SolidBones)  _solidLines.Add(MakeLine(a, b, SolidBoneW,  _solidMat,  false));
            foreach (var (a, b) in DashedBones) _dashedLines.Add(MakeLine(a, b, DashedBoneW, _dashedMat, true));
        }

        private (LineRenderer line, JointName a, JointName b) MakeLine(JointName a, JointName b, float width, Material mat, bool dashed)
        {
            var go = new GameObject($"Bone_{a}_{b}");
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace   = true;
            lr.positionCount   = 2;
            lr.startWidth      = width;
            lr.endWidth        = width;
            lr.numCapVertices  = 4;
            lr.sharedMaterial  = mat;
            lr.textureMode     = dashed ? LineTextureMode.Tile : LineTextureMode.Stretch;
            lr.alignment       = LineAlignment.View;
            lr.enabled         = false;

            return (lr, a, b);
        }

        private void BuildStatusPill()
        {
            var pillCanvasGo = new GameObject("StatusPillCanvas");
            pillCanvasGo.transform.SetParent(transform, false);
            var canvas = pillCanvasGo.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 31500;
            pillCanvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            var pillGo = new GameObject("StatusPill");
            pillGo.transform.SetParent(pillCanvasGo.transform, false);
            var rt = pillGo.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 1f);
            rt.anchorMax        = new Vector2(0.5f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -32f);
            rt.sizeDelta        = new Vector2(420f, 76f);

            _statusBg = pillGo.AddComponent<Image>();
            _statusBg.color = new Color(0.65f, 0.15f, 0.15f, 0.85f);
            _statusBg.raycastTarget = false;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(pillGo.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            _statusText = labelGo.AddComponent<Text>();
            _statusText.text      = "HAND: WAITING";
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.color     = Color.white;
            _statusText.fontSize  = 28;
            _statusText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statusText.raycastTarget = false;
        }

        // ---------- Material helpers ---------------------------------------------
        private static Material MakeUnlit(Color c, bool isTransparent)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Unlit/Color") ??
                Shader.Find("Standard");
            if (shader == null) return null;

            var m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color"))     m.SetColor("_Color",     c);

            if (isTransparent && m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", 1f); // 0=Opaque, 1=Transparent
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            return m;
        }

        // Builds a line material whose main texture is a 1×16 strip of alternating
        // opaque/transparent pixels. Combined with LineTextureMode.Tile and a
        // matching tile factor it renders as a dashed line.
        private static Material MakeDashedLineMaterial(Color c)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Sprites/Default");
            if (shader == null) return null;

            var m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color"))     m.SetColor("_Color",     c);

            // Make sure transparency is honored for the dash pattern.
            if (m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", 1f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            // 16-pixel-wide strip: 8 opaque, 8 transparent. Tiled along the line.
            var tex = new Texture2D(16, 1, TextureFormat.RGBA32, false)
            {
                wrapMode   = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point,
            };
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++)
            {
                bool on = (i / 4) % 2 == 0;       // dash length 4, gap length 4
                pixels[i] = on ? Color.white : new Color(1, 1, 1, 0);
            }
            tex.SetPixels(pixels);
            tex.Apply(false, true);

            if (m.HasProperty("_BaseMap"))   m.SetTexture("_BaseMap",   tex);
            if (m.HasProperty("_MainTex"))   m.SetTexture("_MainTex",   tex);
            // Tile factor: how often the 16-px strip repeats per meter of line. Higher = more dashes.
            if (m.HasProperty("_BaseMap_ST")) m.SetTextureScale("_BaseMap", new Vector2(40f, 1f));
            if (m.HasProperty("_MainTex_ST")) m.SetTextureScale("_MainTex", new Vector2(40f, 1f));

            return m;
        }
    }
}
