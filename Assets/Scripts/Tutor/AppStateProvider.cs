using System.Collections.Generic;
using System.Text;
using PeriodicAR.AR;
using PeriodicAR.AR.HandTracking;
using PeriodicAR.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PeriodicAR.Tutor
{
    /// <summary>
    /// Singleton that tracks what the user is currently interacting with.
    /// Other systems push state in; TutorController reads it when building the system prompt.
    /// Self-bootstrapping — no Inspector wiring needed.
    /// </summary>
    public class AppStateProvider : MonoBehaviour
    {
        // ---- Singleton ----------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<AppStateProvider>() != null) return;
            var go = new GameObject("[AppStateProvider]");
            DontDestroyOnLoad(go);
            go.AddComponent<AppStateProvider>();
        }

        public static AppStateProvider Instance { get; private set; }

        // ---- State --------------------------------------------------------------

        public string       heldElementSymbol;
        public string       lastScannedObject;
        public List<string> lastScannedElements  = new();
        public List<string> currentlyHighlighted = new();

        // v2 state
        public string lastReactionProduct;  // e.g. "NaCl"
        public string activeHeatmap;        // property display name or null
        public bool   originOverlayActive;
        public string activeScaleMode;      // null | "atom" | "mole"

        // v3 state
        public string activePhaseSubstance;     // "H2O", "CO2", null
        public string currentPhase;             // "solid", "liquid", "gas", null
        public List<string> activeFlameElements = new();
        public string spectrumMode;             // "emission", "absorption"
        public string beakerState;              // "acidic", "neutral", "basic"
        public float  currentPh = 7f;
        public bool   orbitalViewActive;
        public string orbitalViewElement;
        public string activeEquilibriumReaction;
        public string buildingMolecule;
        public string halfLifeIsotope;
        public string todaysElement;
        public string activeExperiment;
        public string activeAbundanceContext;
        public string lastIdentifiedCompound;
        public bool   offlineMode;

        // ---- Grab-tracking internals --------------------------------------------

        private TapToPlace    _placer;
        private ElementLoader _loader;
        private GameObject    _hookedTable;

        // Store UnityAction refs directly so RemoveListener finds them by reference.
        private readonly List<(
            XRGrabInteractable grab,
            UnityAction<SelectEnterEventArgs> onEnter,
            UnityAction<SelectExitEventArgs>  onExit
        )> _touchHooks = new();

        private readonly List<(
            CubeHandGrabbable hand,
            System.Action     onGrab,
            System.Action     onRel
        )> _handHooks = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (_placer == null) _placer = FindAnyObjectByType<TapToPlace>();
            if (_loader == null) _loader = FindAnyObjectByType<ElementLoader>();

            var current = _placer != null ? _placer.CurrentInstance : null;
            if (current != _hookedTable)
            {
                Unhook();
                _hookedTable = current;
                if (current != null) HookAll(current);
                heldElementSymbol = null;
            }
        }

        private void HookAll(GameObject table)
        {
            foreach (var grab in table.GetComponentsInChildren<XRGrabInteractable>(true))
            {
                int z = PeriodicTableLayout.ResolveAtomicNumber(grab.transform);
                if (z <= 0) continue;
                int localZ = z;

                UnityAction<SelectEnterEventArgs> onEnter = (_) => SetHeld(localZ);
                UnityAction<SelectExitEventArgs>  onExit  = (_) => ClearHeld();

                grab.selectEntered.AddListener(onEnter);
                grab.selectExited .AddListener(onExit);
                _touchHooks.Add((grab, onEnter, onExit));
            }

            foreach (var hand in table.GetComponentsInChildren<CubeHandGrabbable>(true))
            {
                int z = PeriodicTableLayout.ResolveAtomicNumber(hand.transform);
                if (z <= 0) continue;
                int localZ = z;

                System.Action onGrab = () => SetHeld(localZ);
                System.Action onRel  = ClearHeld;
                hand.Grabbed  += onGrab;
                hand.Released += onRel;
                _handHooks.Add((hand, onGrab, onRel));
            }
        }

        private void Unhook()
        {
            foreach (var (grab, onEnter, onExit) in _touchHooks)
            {
                if (grab == null) continue;
                grab.selectEntered.RemoveListener(onEnter);
                grab.selectExited .RemoveListener(onExit);
            }
            _touchHooks.Clear();

            foreach (var (hand, onGrab, onRel) in _handHooks)
            {
                if (hand == null) continue;
                hand.Grabbed  -= onGrab;
                hand.Released -= onRel;
            }
            _handHooks.Clear();
        }

        private void SetHeld(int atomicNumber)
        {
            if (_loader == null) return;
            var data = _loader.FindByNumber(atomicNumber);
            heldElementSymbol = data?.symbol;
        }

        private void ClearHeld() => heldElementSymbol = null;

        // ---- Prompt description -------------------------------------------------

        public string DescribeForPrompt()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(heldElementSymbol))
            {
                string name = ElementName(heldElementSymbol);
                sb.AppendLine($"- User is holding: {name} ({heldElementSymbol})");
            }
            if (!string.IsNullOrEmpty(lastScannedObject))
            {
                string elems = lastScannedElements.Count > 0
                    ? string.Join(", ", lastScannedElements)
                    : "no elements mapped";
                sb.AppendLine($"- Last scanned object: {lastScannedObject} ({elems})");
            }
            if (currentlyHighlighted.Count > 0)
                sb.AppendLine($"- Currently highlighted: {string.Join(", ", currentlyHighlighted)}");
            if (!string.IsNullOrEmpty(lastReactionProduct))
                sb.AppendLine($"- Last reaction product: {lastReactionProduct}");
            if (!string.IsNullOrEmpty(activeHeatmap))
                sb.AppendLine($"- Heatmap active: {activeHeatmap}");
            if (originOverlayActive)
                sb.AppendLine($"- Origin overlay: active");
            if (!string.IsNullOrEmpty(activeScaleMode))
                sb.AppendLine($"- Scale visualizer: {activeScaleMode} mode");
            if (!string.IsNullOrEmpty(activePhaseSubstance))
                sb.AppendLine($"- Phase bench: {activePhaseSubstance}, {currentPhase}");
            if (activeFlameElements.Count > 0)
                sb.AppendLine($"- Spectroscopy flame: {string.Join(", ", activeFlameElements)}, mode: {spectrumMode}");
            if (!string.IsNullOrEmpty(beakerState))
                sb.AppendLine($"- pH beaker: {beakerState}, pH={currentPh:F1}");
            if (orbitalViewActive)
                sb.AppendLine($"- Orbital view: {orbitalViewElement}");
            if (!string.IsNullOrEmpty(activeEquilibriumReaction))
                sb.AppendLine($"- Equilibrium bench: {activeEquilibriumReaction}");
            if (!string.IsNullOrEmpty(halfLifeIsotope))
                sb.AppendLine($"- Half-life sim: {halfLifeIsotope}");
            if (!string.IsNullOrEmpty(todaysElement))
                sb.AppendLine($"- Today's element: {todaysElement}");
            if (!string.IsNullOrEmpty(activeExperiment))
                sb.AppendLine($"- Experiment: {activeExperiment}");
            if (!string.IsNullOrEmpty(activeAbundanceContext))
                sb.AppendLine($"- Abundance overlay: {activeAbundanceContext}");
            if (!string.IsNullOrEmpty(lastIdentifiedCompound))
                sb.AppendLine($"- Last compound scan: {lastIdentifiedCompound}");
            if (offlineMode)
                sb.AppendLine("- Offline mode: active");

            return sb.Length > 0 ? sb.ToString().TrimEnd() : "(nothing selected)";
        }

        private string ElementName(string symbol)
        {
            if (_loader == null || _loader.Table == null) return symbol;
            foreach (var el in _loader.Table.elements)
                if (el.symbol == symbol) return el.name;
            return symbol;
        }
    }
}
