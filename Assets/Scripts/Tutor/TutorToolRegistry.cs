using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeriodicAR.Heatmaps;
using PeriodicAR.ObjectDetection;
using PeriodicAR.Reactions;
using PeriodicAR.UI;
using UnityEngine;
// v3 namespaces
using PeriodicAR.Phase;
using PeriodicAR.Spectroscopy;
using PeriodicAR.AcidBase;
using PeriodicAR.Orbitals;
using PeriodicAR.HalfLife;
using PeriodicAR.Equilibrium;
using PeriodicAR.Experiments;
using PeriodicAR.Abundance;
using PeriodicAR.BondBuilder;
using PeriodicAR.CompoundScanner;
using PeriodicAR.Safety;
using PeriodicAR.Quiz;

namespace PeriodicAR.Tutor
{
    public class TutorToolRegistry : MonoBehaviour
    {
        // ---- Wiring (auto-found if null) ----------------------------------------
        private ElementSetHighlighter   _setHighlighter;
        private ElementCategoryHighlighter _catHighlighter;
        private ElementLoader           _loader;

        private void Awake()
        {
            _setHighlighter = FindFirstObjectByType<ElementSetHighlighter>();
            _catHighlighter = FindFirstObjectByType<ElementCategoryHighlighter>();
            _loader         = FindAnyObjectByType<ElementLoader>();
        }

        private void RefreshRefs()
        {
            if (_setHighlighter == null) _setHighlighter = FindFirstObjectByType<ElementSetHighlighter>();
            if (_catHighlighter == null) _catHighlighter = FindFirstObjectByType<ElementCategoryHighlighter>();
            if (_loader         == null) _loader         = FindAnyObjectByType<ElementLoader>();
        }

        // ---- Tool definitions ---------------------------------------------------

        /// <summary>
        /// Returns ~12 tools scoped to the current app state so the LLM context
        /// stays focused. Falls back to <see cref="GetToolDefinitions"/> if
        /// AppStateProvider is unavailable.
        /// </summary>
        public List<ToolDef> GetContextualTools()
        {
            var allByName = GetToolDefinitions().ToDictionary(t => t.function.name);
            var selected  = new List<string>();
            var s         = AppStateProvider.Instance;

            // Core tools — always present
            selected.AddRange(new[]
            {
                "highlight_elements", "focus_element", "clear_highlights",
                "show_bohr_model",    "compare_elements", "filter_table_by",
                "ask_quiz_question",  "show_safety_card",
            });

            if (s != null)
            {
                // Overlay toggles — one per overlay type, respecting current state
                selected.Add(s.originOverlayActive                             ? "clear_origin_overlay" : "show_origin_overlay");
                selected.Add(!string.IsNullOrEmpty(s.activeAbundanceContext)   ? "clear_abundance"      : "show_abundance");
                selected.Add(!string.IsNullOrEmpty(s.activeHeatmap)            ? "clear_heatmap"        : "show_heatmap");

                // Element-in-hand — surfacing most useful per-element actions
                if (!string.IsNullOrEmpty(s.heldElementSymbol))
                    selected.AddRange(new[] { "suggest_reaction", "where_do_i_find" });

                // Active feature — add tools for whatever is currently open (priority order)
                if (!string.IsNullOrEmpty(s.halfLifeIsotope))
                    selected.AddRange(new[] { "simulate_half_life", "date_sample" });
                else if (s.activeFlameElements.Count > 0 || !string.IsNullOrEmpty(s.spectrumMode))
                    selected.AddRange(new[] { "ignite_element", "show_stellar_spectrum" });
                else if (!string.IsNullOrEmpty(s.beakerState) || s.currentPh != 7f)
                    selected.AddRange(new[] { "add_to_beaker", "empty_beaker" });
                else if (s.orbitalViewActive)
                    selected.Add("show_orbitals");
                else if (!string.IsNullOrEmpty(s.activeEquilibriumReaction))
                    selected.Add("show_equilibrium");
                else if (!string.IsNullOrEmpty(s.activeExperiment))
                    selected.Add("run_experiment");
                else if (!string.IsNullOrEmpty(s.buildingMolecule))
                    selected.Add("show_molecule");
                else if (!string.IsNullOrEmpty(s.activePhaseSubstance))
                    selected.Add("show_phase");
                else if (!string.IsNullOrEmpty(s.lastIdentifiedCompound))
                    selected.Add("identify_compound");
            }

            // Deduplicate while preserving order, then map back to ToolDef objects
            var seen   = new HashSet<string>();
            var result = new List<ToolDef>();
            foreach (var name in selected)
            {
                if (seen.Add(name) && allByName.TryGetValue(name, out var def))
                    result.Add(def);
            }
            return result;
        }

        public List<ToolDef> GetToolDefinitions() => new List<ToolDef>
        {
            MakeTool("highlight_elements",
                "Highlight a specific set of elements on the 3D periodic table.",
                MakeParams(("symbols", "array", "Element symbols, e.g. [\"Na\",\"Cl\"]"))),

            MakeTool("focus_element",
                "Focus on a single element: show its Bohr model and info card.",
                MakeParams(("symbol", "string", "Element symbol, e.g. \"Fe\""))),

            MakeTool("clear_highlights",
                "Clear all highlighted elements on the table and restore normal view.",
                new JObject()),

            MakeTool("show_bohr_model",
                "Show the Bohr atomic model for a given element.",
                MakeParams(("symbol", "string", "Element symbol"))),

            MakeTool("compare_elements",
                "Highlight two elements side by side on the table for comparison.",
                MakeParams(
                    ("symbol_a", "string", "First element symbol"),
                    ("symbol_b", "string", "Second element symbol"))),

            MakeTool("filter_table_by",
                "Filter the periodic table to highlight a chemical category such as 'alkali metals', 'noble gases', 'transition metals', etc.",
                MakeParams(("category", "string", "Category name, e.g. 'alkali metals' or 'noble gases'"))),

            MakeTool("suggest_reaction",
                "Show the user which elements react with a given element, from the curated reaction catalog.",
                MakeParams(("symbol", "string", "Element symbol to find reactions for, e.g. 'Na'"))),

            MakeTool("show_heatmap",
                "Color the periodic table with a property gradient (viridis). Properties: AtomicMass, Density, MeltingPoint, BoilingPoint, Electronegativity, FirstIonizationEnergy.",
                MakeParams(("property", "string", "Property name, e.g. 'Electronegativity'"))),

            MakeTool("clear_heatmap",
                "Remove the property heatmap and restore the table's original colors.",
                new JObject()),

            MakeTool("show_atom_scale",
                "Show the atom radius of an element as a to-scale AR sphere. Optionally compare against H, C, Fe, or Cs.",
                MakeParams(("symbol", "string", "Element symbol to visualize"),
                           ("reference", "string", "Reference atom symbol for comparison (optional, e.g. 'H')"))),

            MakeTool("show_mole",
                "Show the volume of one mole of an element as a to-scale AR cube or sphere.",
                MakeParams(("symbol", "string", "Element symbol to visualize"))),

            MakeTool("show_origin_overlay",
                "Color the periodic table by cosmic origin (Big Bang, supernovae, neutron star mergers, etc.).",
                new JObject()),

            MakeTool("clear_origin_overlay",
                "Remove the cosmic origin color overlay from the periodic table.",
                new JObject()),

            MakeTool("where_do_i_find",
                "Show common household items that contain a given element, plus a fun fact.",
                MakeParams(("symbol", "string", "Element symbol, e.g. 'Fe'"))),

            // ---- v3 tools -------------------------------------------------------

            MakeTool("show_phase",
                "Open the State of Matter sandbox for a substance at given temperature and pressure.",
                MakeParams(("substance", "string", "Substance key: H2O, CO2, N2, O2, He, Fe, Hg, NaCl"),
                           ("temperature_k", "number", "Temperature in Kelvin"),
                           ("pressure_atm",  "number", "Pressure in atm"))),

            MakeTool("ignite_element",
                "Add an element to the spectroscopy flame and show its emission lines.",
                MakeParams(("symbol", "string", "Element symbol, e.g. 'Na'"))),

            MakeTool("show_stellar_spectrum",
                "Show the absorption spectrum of a star (Sun, Vega, or Betelgeuse) on the spectrum strip.",
                MakeParams(("star", "string", "Star name: 'Sun', 'Vega', or 'Betelgeuse'"))),

            MakeTool("add_to_beaker",
                "Add a substance to the pH beaker and show the resulting pH color change.",
                MakeParams(("substance", "string", "Substance name or formula, e.g. 'HCl' or 'Vinegar'"))),

            MakeTool("empty_beaker",
                "Reset the pH beaker to neutral water (pH 7).",
                new JObject()),

            MakeTool("show_orbitals",
                "Display the electron orbitals for a given element using the orbital viewer.",
                MakeParams(("symbol", "string", "Element symbol, e.g. 'Fe'"))),

            MakeTool("simulate_half_life",
                "Open the half-life simulator for a specific isotope and start the decay animation.",
                MakeParams(("isotope_id", "string", "Isotope ID: C-14, U-238, U-235, K-40, I-131, Tc-99m, H-3, Po-210, Sr-90"))),

            MakeTool("date_sample",
                "Use the half-life simulator to date a sample by its remaining parent isotope fraction.",
                MakeParams(("isotope_id", "string", "Isotope ID, e.g. 'C-14'"),
                           ("fraction_remaining", "number", "Fraction of parent atoms remaining (0.0 to 1.0)"))),

            MakeTool("show_equilibrium",
                "Open the equilibrium bench for a specific reaction.",
                MakeParams(("reaction_id", "string", "Reaction ID: haber, co2_carbonate, fe_scn, no2_n2o4, synthesis_hcl"))),

            MakeTool("run_experiment",
                "Show a famous chemistry experiment with its story and significance.",
                MakeParams(("experiment_id", "string", "Experiment ID: rutherford, millikan, bohr_hydrogen, curie_radium, chadwick_neutron, moseley_xray, hahn_fission, lavoisier_conservation, volta_battery"))),

            MakeTool("show_abundance",
                "Color the periodic table by elemental abundance in a chosen context.",
                MakeParams(("context_id", "string", "Context: universe, earth_crust, ocean, atmosphere, human_body, sun, jupiter"))),

            MakeTool("clear_abundance",
                "Remove the abundance color overlay from the periodic table.",
                new JObject()),

            MakeTool("identify_compound",
                "Look up a chemical formula or compound name and highlight its constituent elements.",
                MakeParams(("formula", "string", "Chemical formula or name, e.g. 'H2O' or 'NaCl'"))),

            MakeTool("show_safety_card",
                "Show a dangerous chemical combination card.",
                MakeParams(("combo_id", "string", "Combo ID: bleach_ammonia, bleach_acid, hydrogen_peroxide_vinegar, bleach_rubbing_alcohol, drain_cleaner_acid, pool_shock_antifreeze, batteries_water, bleach_heat"))),

            MakeTool("show_molecule",
                "Display a 3D molecule model in the Bond Builder.",
                MakeParams(("molecule_id", "string", "Molecule ID: H2O, CO2, NH3, CH4, O2, NaCl, C6H6, HCl"))),

            MakeTool("ask_quiz_question",
                "Present a contextual quiz question based on what the user is currently looking at.",
                new JObject()),
        };

        // ---- Dispatch -----------------------------------------------------------

        public string Dispatch(string toolName, string argumentsJson)
        {
            RefreshRefs();
            try
            {
                JObject args = string.IsNullOrEmpty(argumentsJson)
                    ? new JObject()
                    : JObject.Parse(argumentsJson);

                return toolName switch
                {
                    "highlight_elements" => ToolHighlightElements(args),
                    "focus_element"      => ToolFocusElement(args),
                    "clear_highlights"   => ToolClearHighlights(),
                    "show_bohr_model"    => ToolShowBohrModel(args),
                    "compare_elements"   => ToolCompareElements(args),
                    "filter_table_by"    => ToolFilterTableBy(args),
                    "suggest_reaction"   => ToolSuggestReaction(args),
                    "show_heatmap"       => ToolShowHeatmap(args),
                    "clear_heatmap"      => ToolClearHeatmap(),
                    "show_atom_scale"    => ToolShowAtomScale(args),
                    "show_mole"          => ToolShowMole(args),
                    "show_origin_overlay"=> ToolShowOriginOverlay(),
                    "clear_origin_overlay"=>ToolClearOriginOverlay(),
                    "where_do_i_find"    => ToolWhereDoIFind(args),

                    // v3
                    "show_phase"         => ToolShowPhase(args),
                    "ignite_element"     => ToolIgniteElement(args),
                    "show_stellar_spectrum" => ToolShowStellarSpectrum(args),
                    "add_to_beaker"      => ToolAddToBeaker(args),
                    "empty_beaker"       => ToolEmptyBeaker(),
                    "show_orbitals"      => ToolShowOrbitals(args),
                    "simulate_half_life" => ToolSimulateHalfLife(args),
                    "date_sample"        => ToolDateSample(args),
                    "show_equilibrium"   => ToolShowEquilibrium(args),
                    "run_experiment"     => ToolRunExperiment(args),
                    "show_abundance"     => ToolShowAbundance(args),
                    "clear_abundance"    => ToolClearAbundance(),
                    "identify_compound"  => ToolIdentifyCompound(args),
                    "show_safety_card"   => ToolShowSafetyCard(args),
                    "show_molecule"      => ToolShowMolecule(args),
                    "ask_quiz_question"  => ToolAskQuizQuestion(),
                    _                    => Error($"Unknown tool '{toolName}'"),
                };
            }
            catch (Exception ex)
            {
                return Error(ex.Message);
            }
        }

        // ---- Tool implementations -----------------------------------------------

        private string ToolHighlightElements(JObject args)
        {
            var symbolsToken = args["symbols"] as JArray;
            if (symbolsToken == null) return Error("Missing 'symbols' array.");
            var symbols = symbolsToken.Select(t => t.ToString()).ToList();

            if (_setHighlighter == null) return Error("ElementSetHighlighter not available.");
            _setHighlighter.HighlightElements(symbols);

            if (AppStateProvider.Instance != null)
                AppStateProvider.Instance.currentlyHighlighted = new List<string>(symbols);

            return Ok(new { highlighted = symbols.Count });
        }

        private string ToolFocusElement(JObject args)
        {
            string sym = args["symbol"]?.ToString();
            if (string.IsNullOrEmpty(sym)) return Error("Missing 'symbol'.");

            int z = ResolveAtomicNumber(sym);
            if (z <= 0) return Error($"Unknown element '{sym}'.");

            _loader?.LoadElement(z);

            // Highlight the single element on the table as well.
            if (_setHighlighter != null)
                _setHighlighter.HighlightElements(new[] { sym });

            return Ok(new { focused = sym, atomicNumber = z });
        }

        private string ToolClearHighlights()
        {
            _setHighlighter?.ClearHighlight();
            _catHighlighter?.ClearHighlight();

            if (AppStateProvider.Instance != null)
                AppStateProvider.Instance.currentlyHighlighted.Clear();

            return Ok(new { cleared = true });
        }

        private string ToolShowBohrModel(JObject args)
        {
            string sym = args["symbol"]?.ToString();
            if (string.IsNullOrEmpty(sym)) return Error("Missing 'symbol'.");

            int z = ResolveAtomicNumber(sym);
            if (z <= 0) return Error($"Unknown element '{sym}'.");

            _loader?.LoadElement(z);
            return Ok(new { showing = sym });
        }

        private string ToolCompareElements(JObject args)
        {
            string a = args["symbol_a"]?.ToString();
            string b = args["symbol_b"]?.ToString();
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return Error("Requires 'symbol_a' and 'symbol_b'.");

            if (_setHighlighter == null) return Error("ElementSetHighlighter not available.");
            _setHighlighter.HighlightElements(new[] { a, b });

            return Ok(new { comparing = new[] { a, b } });
        }

        private string ToolFilterTableBy(JObject args)
        {
            string cat = args["category"]?.ToString()?.ToLowerInvariant() ?? "";
            string key = CategoryToKey(cat);

            if (string.IsNullOrEmpty(key))
                return Error($"Unknown category '{cat}'. Try 'alkali metals', 'noble gases', 'transition metals', 'halogens', 'metalloids', 'alkaline earth metals', 'lanthanides', 'actinides', or 'other nonmetals'.");

            if (_catHighlighter == null) return Error("ElementCategoryHighlighter not available.");
            _catHighlighter.Highlight(key);

            // Collect the highlighted symbols for AppState.
            if (AppStateProvider.Instance != null)
            {
                var symbols = GetSymbolsForCategory(key);
                AppStateProvider.Instance.currentlyHighlighted = symbols;
            }

            return Ok(new { category = cat, key });
        }

        // ---- v2 tools -----------------------------------------------------------

        private string ToolSuggestReaction(JObject args)
        {
            string sym = args["symbol"]?.ToString();
            if (string.IsNullOrEmpty(sym)) return Error("Missing 'symbol'.");

            var reactions = ReactionCatalog.AllReactionsContaining(sym);
            if (reactions.Count == 0)
                return Ok(new { symbol = sym, reactions = new string[0], note = "No reactions in catalog for this element." });

            var list = reactions.Select(r =>
                r.animationType == "no_reaction"
                    ? $"{r.reactantA} + {r.reactantB} → no reaction"
                    : $"{r.reactantA} + {r.reactantB} → {r.productFormula} ({r.productName})"
            ).ToArray();

            return Ok(new { symbol = sym, reactions = list });
        }

        private string ToolShowHeatmap(JObject args)
        {
            string propName = args["property"]?.ToString() ?? "";
            if (!System.Enum.TryParse<ElementPropertyResolver.Property>(propName, true, out var prop))
                return Error($"Unknown property '{propName}'. Valid: {string.Join(", ", System.Enum.GetNames(typeof(ElementPropertyResolver.Property)))}");

            var ctrl = HeatmapOverlayController.Instance ?? FindFirstObjectByType<HeatmapOverlayController>();
            if (ctrl == null) return Error("HeatmapOverlayController not available.");
            ctrl.ApplyHeatmap(prop);
            return Ok(new { property = ElementPropertyResolver.DisplayNameFor(prop) });
        }

        private string ToolClearHeatmap()
        {
            var ctrl = HeatmapOverlayController.Instance ?? FindFirstObjectByType<HeatmapOverlayController>();
            ctrl?.ClearHeatmap();
            return Ok(new { cleared = true });
        }

        private string ToolShowAtomScale(JObject args)
        {
            string sym = args["symbol"]?.ToString();
            if (string.IsNullOrEmpty(sym)) return Error("Missing 'symbol'.");

            var ctrl = Scale.ScaleController.Instance ?? FindFirstObjectByType<Scale.ScaleController>();
            if (ctrl == null) return Error("ScaleController not available.");
            ctrl.ShowAtomMode(sym);
            return Ok(new { mode = "atom", symbol = sym });
        }

        private string ToolShowMole(JObject args)
        {
            string sym = args["symbol"]?.ToString();
            if (string.IsNullOrEmpty(sym)) return Error("Missing 'symbol'.");

            var ctrl = Scale.ScaleController.Instance ?? FindFirstObjectByType<Scale.ScaleController>();
            if (ctrl == null) return Error("ScaleController not available.");
            ctrl.ShowMoleMode(sym);
            return Ok(new { mode = "mole", symbol = sym });
        }

        private string ToolShowOriginOverlay()
        {
            var ctrl = Origins.OriginOverlayController.Instance ?? FindFirstObjectByType<Origins.OriginOverlayController>();
            if (ctrl == null) return Error("OriginOverlayController not available.");
            if (!AppStateProvider.Instance?.originOverlayActive ?? true)
                ctrl.ToggleOverlay();
            return Ok(new { overlayActive = true });
        }

        private string ToolClearOriginOverlay()
        {
            var ctrl = Origins.OriginOverlayController.Instance ?? FindFirstObjectByType<Origins.OriginOverlayController>();
            if (ctrl == null) return Error("OriginOverlayController not available.");
            if (AppStateProvider.Instance?.originOverlayActive ?? false)
                ctrl.ToggleOverlay();
            return Ok(new { overlayActive = false });
        }

        private string ToolWhereDoIFind(JObject args)
        {
            string sym = args["symbol"]?.ToString();
            if (string.IsNullOrEmpty(sym)) return Error("Missing 'symbol'.");

            var entry = Household.HouseholdSourceCatalog.Lookup(sym);
            if (entry == null) return Ok(new { symbol = sym, items = new string[0], fun_fact = "" });

            var ctrl = Household.HouseholdController.Instance ?? FindFirstObjectByType<Household.HouseholdController>();
            ctrl?.ShowForElement(sym);

            return Ok(new { symbol = sym, items = entry.items?.ToArray(), fun_fact = entry.funFact });
        }

        // ---- v3 tool implementations -------------------------------------------

        private string ToolShowPhase(JObject args)
        {
            string substance = args["substance"]?.ToString() ?? "H2O";
            float  tempK     = args["temperature_k"]?.Value<float>() ?? 298f;
            float  pressAtm  = args["pressure_atm"]?.Value<float>()  ?? 1f;
            var ctrl = PhaseController.Instance ?? FindFirstObjectByType<PhaseController>();
            if (ctrl == null) return Error("PhaseController not available.");
            ctrl.ShowPhase(substance, tempK, pressAtm);
            return Ok(new { substance, temperature_k = tempK, pressure_atm = pressAtm });
        }

        private string ToolIgniteElement(JObject args)
        {
            string sym = args["symbol"]?.ToString();
            if (string.IsNullOrEmpty(sym)) return Error("Missing 'symbol'.");
            var ctrl = SpectroscopyController.Instance ?? FindFirstObjectByType<SpectroscopyController>();
            if (ctrl == null) return Error("SpectroscopyController not available.");
            ctrl.IgniteElement(sym);
            return Ok(new { ignited = sym });
        }

        private string ToolShowStellarSpectrum(JObject args)
        {
            string star = args["star"]?.ToString() ?? "Sun";
            var ctrl = SpectroscopyController.Instance ?? FindFirstObjectByType<SpectroscopyController>();
            if (ctrl == null) return Error("SpectroscopyController not available.");
            ctrl.ShowStellarSpectrum(star);
            return Ok(new { star });
        }

        private string ToolAddToBeaker(JObject args)
        {
            string substance = args["substance"]?.ToString();
            if (string.IsNullOrEmpty(substance)) return Error("Missing 'substance'.");
            var ctrl = AcidsController.Instance ?? FindFirstObjectByType<AcidsController>();
            if (ctrl == null) return Error("AcidsController not available.");
            ctrl.AddToBeaker(substance);
            return Ok(new { added = substance });
        }

        private string ToolEmptyBeaker()
        {
            var ctrl = AcidsController.Instance ?? FindFirstObjectByType<AcidsController>();
            if (ctrl == null) return Error("AcidsController not available.");
            ctrl.EmptyBeaker();
            return Ok(new { emptied = true });
        }

        private string ToolShowOrbitals(JObject args)
        {
            string sym = args["symbol"]?.ToString();
            if (string.IsNullOrEmpty(sym)) return Error("Missing 'symbol'.");
            var ctrl = OrbitalController.Instance ?? FindFirstObjectByType<OrbitalController>();
            if (ctrl == null) return Error("OrbitalController not available.");
            ctrl.ShowOrbitalsForSymbol(sym);
            return Ok(new { showing = sym });
        }

        private string ToolSimulateHalfLife(JObject args)
        {
            string isotopeId = args["isotope_id"]?.ToString();
            if (string.IsNullOrEmpty(isotopeId)) return Error("Missing 'isotope_id'.");
            var ctrl = HalfLifeController.Instance ?? FindFirstObjectByType<HalfLifeController>();
            if (ctrl == null) return Error("HalfLifeController not available.");
            ctrl.SimulateIsotope(isotopeId);
            return Ok(new { simulating = isotopeId });
        }

        private string ToolDateSample(JObject args)
        {
            string isotopeId = args["isotope_id"]?.ToString();
            float  fraction  = args["fraction_remaining"]?.Value<float>() ?? 0.5f;
            if (string.IsNullOrEmpty(isotopeId)) return Error("Missing 'isotope_id'.");
            var ctrl = HalfLifeController.Instance ?? FindFirstObjectByType<HalfLifeController>();
            if (ctrl == null) return Error("HalfLifeController not available.");
            ctrl.DateSample(isotopeId, fraction);
            double halfLives = -Math.Log(fraction) / Math.Log(2);
            var iso = IsotopeCatalog.Find(isotopeId);
            double ageSeconds = iso != null ? halfLives * iso.halfLifeSeconds : 0;
            return Ok(new { isotope = isotopeId, fraction_remaining = fraction, half_lives = halfLives, age_seconds = ageSeconds });
        }

        private string ToolShowEquilibrium(JObject args)
        {
            string reactionId = args["reaction_id"]?.ToString();
            if (string.IsNullOrEmpty(reactionId)) return Error("Missing 'reaction_id'.");
            var ctrl = EquilibriumController.Instance ?? FindFirstObjectByType<EquilibriumController>();
            if (ctrl == null) return Error("EquilibriumController not available.");
            ctrl.ShowReaction(reactionId);
            return Ok(new { reaction = reactionId });
        }

        private string ToolRunExperiment(JObject args)
        {
            string expId = args["experiment_id"]?.ToString();
            if (string.IsNullOrEmpty(expId)) return Error("Missing 'experiment_id'.");
            var ctrl = ExperimentsController.Instance ?? FindFirstObjectByType<ExperimentsController>();
            if (ctrl == null) return Error("ExperimentsController not available.");
            ctrl.ShowExperimentById(expId);
            var exp = ExperimentsCatalog.Find(expId);
            return Ok(new { experiment = exp?.name ?? expId, scientist = exp?.scientist });
        }

        private string ToolShowAbundance(JObject args)
        {
            string contextId = args["context_id"]?.ToString();
            if (string.IsNullOrEmpty(contextId)) return Error("Missing 'context_id'.");
            var ctrl = AbundanceOverlayController.Instance ?? FindFirstObjectByType<AbundanceOverlayController>();
            if (ctrl == null) return Error("AbundanceOverlayController not available.");
            ctrl.ApplyContext(contextId);
            return Ok(new { context = contextId });
        }

        private string ToolClearAbundance()
        {
            var ctrl = AbundanceOverlayController.Instance ?? FindFirstObjectByType<AbundanceOverlayController>();
            if (ctrl == null) return Error("AbundanceOverlayController not available.");
            ctrl.ClearAbundance();
            return Ok(new { cleared = true });
        }

        private string ToolIdentifyCompound(JObject args)
        {
            string formula = args["formula"]?.ToString();
            if (string.IsNullOrEmpty(formula)) return Error("Missing 'formula'.");
            var ctrl = CompoundScannerController.Instance ?? FindFirstObjectByType<CompoundScannerController>();
            if (ctrl == null) return Error("CompoundScannerController not available.");
            ctrl.IdentifyCompound(formula);
            var entry = CompoundDatabase.Find(formula);
            return entry != null
                ? Ok(new { found = entry.name, elements = entry.elements, common_in = entry.commonIn })
                : Ok(new { found = (string)null, note = "Not in local database — AI will respond." });
        }

        private string ToolShowSafetyCard(JObject args)
        {
            string comboId = args["combo_id"]?.ToString();
            if (string.IsNullOrEmpty(comboId)) return Error("Missing 'combo_id'.");
            var ctrl = SafetyController.Instance ?? FindFirstObjectByType<SafetyController>();
            if (ctrl == null) return Error("SafetyController not available.");
            ctrl.ShowSafetyCard(comboId);
            var combo = SafetyCatalog.Find(comboId);
            return Ok(new { combo = combo?.ingredientA + " + " + combo?.ingredientB, hazard = combo?.hazard });
        }

        private string ToolShowMolecule(JObject args)
        {
            string molId = args["molecule_id"]?.ToString();
            if (string.IsNullOrEmpty(molId)) return Error("Missing 'molecule_id'.");
            var ctrl = BondBuilderController.Instance ?? FindFirstObjectByType<BondBuilderController>();
            if (ctrl == null) return Error("BondBuilderController not available.");
            ctrl.ShowMoleculeById(molId);
            var mol = MoleculeRecipeCatalog.Find(molId);
            return Ok(new { molecule = mol?.name ?? molId, geometry = mol?.geometry });
        }

        private string ToolAskQuizQuestion()
        {
            var ctrl = QuizController.Instance ?? FindFirstObjectByType<QuizController>();
            if (ctrl == null) return Error("QuizController not available.");
            ctrl.PresentContextualQuestion();
            return Ok(new { quiz_presented = true });
        }

        // ---- Helpers ------------------------------------------------------------

        private int ResolveAtomicNumber(string symbol)
        {
            if (_loader == null || _loader.Table == null) return 0;
            foreach (var el in _loader.Table.elements)
                if (string.Equals(el.symbol, symbol, StringComparison.OrdinalIgnoreCase)) return el.number;
            return 0;
        }

        private List<string> GetSymbolsForCategory(string key)
        {
            if (_loader == null || _loader.Table == null) return new List<string>();
            var result = new List<string>();
            foreach (var el in _loader.Table.elements)
            {
                if (CategoryMatchesKey(el, key)) result.Add(el.symbol);
            }
            return result;
        }

        private static bool CategoryMatchesKey(Data.AtomElementData el, string key)
        {
            string cat = el.category ?? string.Empty;
            return key switch
            {
                "alkali"        => cat == "alkali metal" || cat.Contains("alkali metal"),
                "alkaline"      => cat == "alkaline earth metal",
                "lanthanoid"    => cat == "lanthanide",
                "actinoid"      => cat == "actinide",
                "transition"    => cat == "transition metal" || cat.Contains("transition metal"),
                "posttransition"=> cat == "post-transition metal" || cat.Contains("post-transition"),
                "metalloid"     => cat == "metalloid" || cat.Contains("metalloid"),
                "halogen"       => new HashSet<int>{ 9,17,35,53,85,117 }.Contains(el.number),
                "noblegas"      => cat == "noble gas" || cat.Contains("noble gas"),
                "othernonmetal" => cat == "polyatomic nonmetal" || (cat == "diatomic nonmetal" && !new HashSet<int>{ 9,17,35,53,85,117 }.Contains(el.number)),
                _               => false,
            };
        }

        // Maps natural-language category names → ElementCategoryHighlighter button keys.
        private static string CategoryToKey(string input)
        {
            if (input.Contains("alkali") && !input.Contains("earth"))   return "alkali";
            if (input.Contains("alkaline") || input.Contains("earth"))  return "alkaline";
            if (input.Contains("lanthan"))                               return "lanthanoid";
            if (input.Contains("actinid") || input.Contains("actinoid"))return "actinoid";
            if (input.Contains("transition") && !input.Contains("post"))return "transition";
            if (input.Contains("post") || input.Contains("poor metal")) return "posttransition";
            if (input.Contains("metalloid") || input.Contains("semi"))  return "metalloid";
            if (input.Contains("halogen"))                               return "halogen";
            if (input.Contains("noble") || input.Contains("inert"))     return "noblegas";
            if (input.Contains("nonmetal") || input.Contains("non-metal")) return "othernonmetal";
            return string.Empty;
        }

        private static ToolDef MakeTool(string name, string description, JObject parameters)
            => new ToolDef
            {
                type     = "function",
                function = new FunctionDef { name = name, description = description, parameters = parameters },
            };

        private static JObject MakeParams(params (string name, string type, string desc)[] props)
        {
            var properties = new JObject();
            var required   = new JArray();
            foreach (var (name, type, desc) in props)
            {
                JObject propDef = type == "array"
                    ? new JObject { ["type"] = "array", ["items"] = new JObject { ["type"] = "string" }, ["description"] = desc }
                    : new JObject { ["type"] = type, ["description"] = desc };
                properties[name] = propDef;
                required.Add(name);
            }
            return new JObject
            {
                ["type"]       = "object",
                ["properties"] = properties,
                ["required"]   = required,
            };
        }

        private static string Ok(object data)  => JsonConvert.SerializeObject(new { status = "ok",   data });
        private static string Error(string msg) => JsonConvert.SerializeObject(new { status = "error", reason = msg });
    }
}
