# AR Periodic Table — Feature Pack v3

This pack adds **fourteen** teaching features and does a full UI cleanup.
The UI work is **mandatory, not optional, and comes first**. By the time
v3 ships, the app has 25+ actions; lining them up across the top of the
screen turns it into a calculator. Read Part 0 carefully — every v3
feature lands inside the new shell.

## What you're building

| #   | Feature                       | What it teaches                                  | Lift   |
|-----|-------------------------------|--------------------------------------------------|--------|
| 0   | UI Shell Redesign (mandatory) | (UX, not content)                                | M      |
| 1   | State of Matter Sandbox       | Phase changes, why ice floats                    | M      |
| 2   | Spectroscopy Mode             | How we know what stars are made of               | M      |
| 3   | pH / Acid-Base Visualizer     | Acids, bases, ions, indicators                   | M      |
| 4   | Orbital Viewer (real s/p/d/f) | Quantum mechanics of electron clouds             | M      |
| 5   | Equilibrium & Kinetics        | Reaction rates, Le Chatelier's principle         | M      |
| 6   | Bond Builder                  | Molecular geometry, formal charge                | L      |
| 7   | Half-Life Simulator           | Radioactive decay, dating, half-life             | S      |
| 8   | Element of the Day            | Daily return habit                               | S      |
| 9   | Famous Experiments Mode       | How science was actually done                    | M      |
| 10  | Planetary Abundance Overlay   | Where each element lives in the universe         | S      |
| 11  | Compound Scanner (OCR)        | Reading the chemistry of everyday labels         | M      |
| 12  | Contextual Quizzing           | Real understanding checks, not flashcards        | S      |
| 13  | Lab Safety / Dangerous Combos | Household chemistry safety literacy              | S      |
| 14  | Offline Mode                  | School-friendliness                              | S      |

Lift: S = ½ day, M = 1–2 days, L = 3+ days. Build order is **0, then 8,
then 10, then 7** to get easy wins under the new shell, then **2, 3, 4,
1, 5, 9** in any order, then **6, 11, 12, 13** as the long tail. Offline
mode (14) is last — it's a sweep across everything you built.

## Ground rules (same as v1/v2)

- Don't redeploy or modify the Coral or LLM servers.
- Don't break v1/v2 features. The cube grab, scan flow, reactions,
  heatmaps, origin overlay, scale viz, household cards, and tutor must
  all keep working.
- No external dependencies beyond Newtonsoft.Json and what v1/v2 added.
- Every new surface area updates `AppStateProvider` so the tutor stays
  context-aware. Every new visual gets a tutor tool so Qwen can drive it.
- Every new panel / widget uses a procedural factory — no Inspector
  prefab wiring.

---

# Part 0 — UI Shell Redesign (MANDATORY, BUILD FIRST)

## The problem

By the end of v3 the app has these actions, conservatively:

```
Place   Scan   Mix   Trends   Origin   Scale   Tutor
Spectrum   Phase   Orbitals   pH   Equilibrium   BondBuilder
HalfLife   Experiments   Abundance   ElementOfDay   CompoundScan
Quiz   Safety   Settings   Help
```

Twenty-plus. Stuffed at the top of the screen this is a TI-83, not an
AR app. It also conflicts with the iOS status bar, is unreachable
one-handed on phones over 6 inches, and most importantly it covers the
camera view — the entire point of an AR app is the camera frame.

## The design

Three persistent UI elements, **all bottom-anchored. Nothing on top.**

```
   ┌────────────────────────────────────┐
   │                                    │
   │                                    │
   │       [ AR camera view ]           │
   │                                    │
   │                                    │
   │                                    │
   │                                    │
   │                                    │
   │  ╭───────────────────╮             │   ← status pill
   │  │ Found: laptop 87% │             │     (bottom-left, ephemeral)
   │  ╰───────────────────╯             │
   │                                    │
   │           ●  ○                     │   ← page indicator (when expanded)
   │     ╭─╮ ╭─╮ ╭─╮ ╭─╮                │
   │     │ │ │ │ │ │ │ │                │   ← wheel pills (when expanded)
   │     ╰─╯ ╰─╯ ╰─╯ ╰─╯                │
   │              ╭───╮          ╭───╮  │
   │              │ + │          │ T │  │   ← FAB (left) + Tutor (right)
   │              ╰───╯          ╰───╯  │
   └────────────────────────────────────┘
```

### Element 1: Action Wheel (bottom center-left)

- **Collapsed:** a single circular FAB with a "+" icon, ~64 px.
- **Expanded:** the "+" rotates 45° to become "×", and a horizontal row
  of pill buttons fans upward. 6 pills per page, swipe to flip pages.
- Tapping a pill triggers its action and **collapses the wheel**.
- Tapping outside the wheel collapses it.
- Long-pressing a pill **pins it as the FAB shortcut**: short-tap the
  FAB now triggers that action directly. Re-long-press another pill to
  rebind. The default pinned action is "Scan" because that's the most
  common first move.
- The FAB itself shows the pinned action's icon at half-opacity when
  collapsed, so the user knows what short-tap will do.

### Element 2: Tutor button (bottom-right)

- Always visible, always one-tap.
- Opens the screen-space tutor panel (built in v1).
- When the tutor has unread output (e.g., it finished a long thinking
  response while the user was looking at AR content), the button shows
  a small dot.

### Element 3: Status Pill (bottom-left, above the wheel)

- Ephemeral feedback line. Shows what's happening or what just happened.
- Examples: `Scanning…`, `Found: laptop · 87% · 14 ms`, `Trends:
  Electronegativity`, `Reaction: Na + Cl → NaCl`.
- Auto-dismisses after 3 seconds of inactivity.
- Tap to dismiss early.
- Stack at most one — new messages replace old ones with a 200 ms cross-fade.

## Wheel pages (final layout including everything through v3)

**Page 1 — Explore the Table:**
- Place / Re-place table
- Trends (opens heatmap submenu)
- Origin overlay
- Abundance overlay (v3 #10)
- Orbitals view (v3 #4)
- Element of the Day (v3 #8)

**Page 2 — Real-world chemistry:**
- Scan object (existing)
- Compound scan / OCR (v3 #11)
- Household uses (existing v2)
- Safety / dangerous combos (v3 #13)
- Famous experiments (v3 #9)
- Quiz me (v3 #12)

**Page 3 — Lab benches:**
- Mix (reactions, v2)
- pH / acid-base bench (v3 #3)
- Phase / state-of-matter bench (v3 #1)
- Spectroscopy bench (v3 #2)
- Equilibrium bench (v3 #5)
- Bond builder (v3 #6)
- Half-life simulator (v3 #7)

Three pages, ~6 pills each, swipeable. Page indicator dots above the
wheel. This scales to ~25 features without ever feeling crowded.

## Visual design tokens

Define these as constants in one C# file (`Assets/Scripts/UI/Theme.cs`)
so the rest of the v3 code references them, not hex strings scattered
through every prefab:

```csharp
public static class Theme {
    // Surfaces
    public static readonly Color SurfaceGlass    = new(0.08f, 0.10f, 0.14f, 0.78f);
    public static readonly Color SurfaceElevated = new(0.12f, 0.14f, 0.18f, 0.92f);
    public static readonly Color OnSurface       = new(0.95f, 0.97f, 1.00f, 1.00f);
    public static readonly Color OnSurfaceDim    = new(0.95f, 0.97f, 1.00f, 0.60f);

    // Accent (used sparingly — focus indicators, active state)
    public static readonly Color Accent          = new(0.30f, 0.78f, 1.00f, 1.00f);
    public static readonly Color AccentMuted     = new(0.30f, 0.78f, 1.00f, 0.30f);

    // Semantic
    public static readonly Color Danger          = new(0.95f, 0.35f, 0.35f, 1.00f);
    public static readonly Color Success         = new(0.40f, 0.85f, 0.55f, 1.00f);
    public static readonly Color Warning         = new(0.98f, 0.78f, 0.30f, 1.00f);

    // Geometry
    public const float CornerRadius = 16f;
    public const float PillHeight   = 44f;
    public const float FabSize      = 64f;
    public const float WheelGap     = 8f;
    public const float SafeAreaBottom = 32f;  // above home indicator on iOS

    // Type scale (TMP point sizes at reference DPI; scale globally via Canvas Scaler)
    public const float TypeSizeS = 14f;
    public const float TypeSizeM = 18f;
    public const float TypeSizeL = 24f;
    public const float TypeSizeXL = 32f;

    // Motion
    public const float AnimFast = 0.15f;
    public const float AnimMed  = 0.30f;
    public const float AnimSlow = 0.60f;
}
```

### Visual principles

- **Glass surfaces, not solid panels.** Use the `SurfaceGlass` color with
  a backdrop blur if available; falls back to plain alpha if not. The
  camera should bleed through every UI surface — this app's content is
  the real world, not the UI.
- **One accent color, used sparingly.** Active wheel page, focused
  element, in-flight loading. Everything else is OnSurface white at
  varying opacity.
- **No drop shadows.** They look like 2014 Material Design. Use a 1 px
  inner stroke at 8% white for elevation cues instead.
- **Rounded corners on everything except the camera frame.** 16 px
  consistent.
- **Iconography only on the wheel.** Use a single icon set (Lucide
  outlines work well — they're free and clean). Don't mix icon styles.
  Every wheel pill has icon + label; labels are mandatory because no one
  remembers what 14 mystery icons mean.
- **Motion is fast.** 150 ms for taps and acknowledgments, 300 ms for
  panel transitions, 600 ms only for the truly attention-grabbing
  things (table placement, reaction ignite). The current AR scene has
  a lot of slow stuff; the UI should feel snappier than the AR.

### Accessibility (don't skip)

- All text must read at AAA contrast (7:1) against the glass surface.
  The `OnSurface` color above hits that.
- Minimum tap target: 44 × 44 pt. Wheel pills are 44 high; the FAB is 64.
- All wheel pills have an explicit accessibility label, even if a
  screen reader is improbable. The cost is one string per pill.
- The wheel respects the system safe area — never goes under the home
  indicator. Use Unity's `SafeArea` API, not hardcoded margins.
- A **Reduce Motion** toggle in Settings disables non-essential
  animations (the FAB rotation, page transitions). Use the system
  setting on iOS if available.

### Files for Part 0

```
Assets/
  Scripts/
    UI/
      Theme.cs
      ActionWheel.cs
      ActionWheelBindings.cs
      WheelItem.cs                       // data class — icon, label, action callback
      WheelRegistry.cs                   // builds the three-page list
      StatusPill.cs
      StatusPillBindings.cs
      AppShellController.cs              // owns wheel + tutor button + pill
      AppShellPrefabFactory.cs           // builds the whole shell at runtime
      SafeAreaResolver.cs                // simple utility for bottom margin
```

### `AppShellController.cs` API

```csharp
public class AppShellController : MonoBehaviour {
    public static AppShellController Instance { get; private set; }

    // Status pill
    public void ShowStatus(string text, float autoHideSeconds = 3f);
    public void ClearStatus();

    // Wheel state
    public void OpenWheel();
    public void CloseWheel();
    public void PinAsShortcut(string wheelItemId);

    // Tutor button "unread dot"
    public void SetTutorBadge(bool show);
}
```

Every other v3 feature gets at the UI **through this one object**. No
feature should be reaching into the wheel or the pill directly — it
asks the shell.

### `WheelRegistry.cs`

A single static method returns the full list of wheel items. Each item:

```csharp
public class WheelItem {
    public string id;             // e.g. "scan", "spectroscopy"
    public string label;          // displayed
    public string iconName;       // e.g. "scan", "atom", "flame"
    public int page;              // 0, 1, 2
    public Action onTap;          // wired by AppShellController to the feature's controller
    public Func<bool> isAvailable;// optional: e.g. spectroscopy is only available after table is placed
}
```

`AppShellController` rebuilds the visible wheel items on `Awake` and
again whenever a feature controller registers itself. This means each
new v3 feature just calls `AppShellController.Instance.RegisterWheelItem(...)`
in its own `Start()`; the wheel populates itself.

### Migration from existing buttons

Existing buttons (`Place`, `Scan`, `Mix`, `Trends`, `Origin`, `Scale`,
`Tutor`) all get removed from the top HUD and re-registered as wheel
items. The `Tutor` button is special — it's not on the wheel, it's the
dedicated bottom-right button. Everything else moves into the wheel.

The existing v1 `ScanController.scanButton` reference now points at a
hidden no-op button under the shell; the wheel item calls
`ScanController.OnScanPressed()` directly. Same pattern for the v2
controllers.

### Test checklist for Part 0

- [ ] Fresh build → app launches → camera view fills the entire screen.
- [ ] Bottom of screen shows FAB (+) and Tutor (T) buttons; nothing on top.
- [ ] Tap FAB → wheel fans up with 6 pills, page indicator shows
      "● ○ ○" (or matching the active page).
- [ ] Swipe left/right on the expanded wheel → flips pages with a
      smooth horizontal transition.
- [ ] Tap a pill → action fires, wheel collapses, status pill shows
      a confirmation.
- [ ] Tap outside the wheel → collapses without firing anything.
- [ ] Long-press a pill → small toast "Pinned as shortcut", FAB icon
      changes to reflect.
- [ ] Short-tap FAB now triggers the pinned action directly.
- [ ] Status pill auto-dismisses after 3 seconds.
- [ ] Tutor button opens the tutor panel from v1 (which itself is now
      bottom-anchored — see Tutor Panel Update below).
- [ ] All existing v1/v2 features (Scan, Mix, Trends, Origin overlay,
      Scale, household cards) still work, accessed via the wheel.
- [ ] On a phone with a home indicator (iPhone X+), the FAB doesn't
      overlap it.

### Tutor Panel Update

The tutor panel from v1 was probably built as a centered overlay or a
top-down sheet. It now slides up from the bottom edge, takes ~70% of
the screen height, and has a drag handle at the top to dismiss. The
input field stays pinned to just above the system keyboard when typing.
This is a small refactor of the existing v1 `TutorPanel.cs`, not a new
component.

---

# Part 1 — State of Matter Sandbox

## What the user does

Wheel → **Phase**. A virtual chamber spawns in front of the user
(~30 cm cube, floating in AR). The chamber contains ~50 molecule
sprites of a starting substance (default: water). Two sliders below
the chamber:

- **Temperature** — left to right, ranges from 1 K to 500 K (configurable).
- **Pressure** — bottom to top, ranges from 0.001 atm to 1000 atm.

As the user drags either slider, the molecules respond in real time:

- **Solid:** molecules locked in a lattice, vibrating slightly.
- **Liquid:** molecules flowing past each other, weak clustering.
- **Gas:** molecules zipping around the chamber, occasional collisions.
- **Supercritical:** above the critical point, particles behave like
  dense gas — visible as faster motion + density variation.

A small phase diagram sits to the right of the chamber with a moving
crosshair showing the current (T, P) point relative to the substance's
real phase boundaries. The phase label updates live: "Liquid · 25 °C ·
1 atm". A "Substance" picker at the top lets the user switch between
~8 substances: H₂O, CO₂, N₂, O₂, NaCl, Fe, Hg, He.

Special case for water: at standard pressure, dragging temperature down
below 4 °C visibly **decreases density** (the lattice spreads out). A
small annotation appears: "Why ice floats". Tap to see the tutor
explain.

## Files

```
Assets/
  Resources/
    PhaseDiagrams.json
  Scripts/
    Phase/
      PhaseDiagramData.cs
      PhaseChamber.cs
      PhaseChamberBindings.cs
      MoleculeSim.cs
      PhaseController.cs
      PhasePrefabFactory.cs
```

## `PhaseDiagrams.json`

For each supported substance, record:

- Triple point (T_tp, P_tp)
- Critical point (T_cp, P_cp)
- Boiling point at 1 atm (T_b)
- Melting point at 1 atm (T_m)
- A few line segments defining the solid-liquid, liquid-gas, and
  solid-gas boundaries (just enough to draw a recognizable diagram —
  three or four (T, P) points per boundary)
- Density at standard conditions for the visual sim
- Molecule color and approximate radius for rendering

```json
{
  "substances": {
    "H2O": {
      "name": "Water",
      "formula": "H₂O",
      "triple_point": {"T": 273.16, "P": 0.006},
      "critical_point": {"T": 647.1, "P": 218.3},
      "melting_at_1atm": 273.15,
      "boiling_at_1atm": 373.15,
      "ice_floats": true,
      "molecule_color": "#5BB0FF",
      "molecule_radius_px": 8,
      "boundaries": {
        "solid_liquid": [[273.16, 0.006], [273.15, 1.0], [251, 2070]],
        "liquid_gas":  [[273.16, 0.006], [373.15, 1.0], [647.1, 218.3]],
        "solid_gas":   [[173, 0.0001],   [273.16, 0.006]]
      }
    }
  }
}
```

Fill in the 7 other substances similarly. Use the simplified phase
diagrams in any general chemistry textbook — they don't need to be
research-grade.

## `MoleculeSim.cs`

A lightweight 2D particle sim rendered inside the chamber face. Use a
single quad with a custom shader, or instanced sprites. Skip a physics
engine — for 50 particles, a Verlet integrator in C# with a coroutine
is plenty.

Behavior modes driven by the current phase:

```csharp
public enum PhaseMode { Solid, Liquid, Gas, Supercritical }

public class MoleculeSim {
    public PhaseMode mode;
    public float temperatureKelvin;     // drives kinetic energy
    public float pressureAtm;           // drives density visualization

    public void Tick(float dt);         // updates particle positions
}
```

- **Solid:** anchor each particle to a lattice point; perturb position
  by a small vibration proportional to T.
- **Liquid:** Lennard-Jones-ish attraction with random thermal motion.
  Particles cluster but flow.
- **Gas:** straight-line motion, wall collisions, occasional particle-
  particle collisions. No clustering.
- **Supercritical:** behaves like dense gas but with fluctuating
  density (visible as cloudy patches).

This doesn't need to be physically accurate — it needs to *look* like
the right phase. Spend more time on the visual feel than on real
intermolecular potentials.

## `PhaseChamber.cs`

The world-space UI. A floating cube with:
- The molecule sim rendered on the front face (or in a viewport inside).
- Temperature slider along the bottom edge.
- Pressure slider along the left edge.
- Substance picker (horizontal scrolling chips) along the top.
- Phase diagram panel attached to the right side, with the (T, P)
  crosshair.
- Phase label below the chamber.

## `PhaseController.cs`

Inspector fields:
```csharp
public PhaseChamber chamber;          // built by factory
public Camera mainCam;                // auto-found
```

On Start:
- Register itself in the wheel registry as "Phase".
- Load `PhaseDiagrams.json`.

On wheel item tap:
- Spawn chamber 0.5 m in front of camera. Lock orientation to face user.
- Default substance = H₂O, T = 298 K, P = 1 atm. Phase = Liquid.

On slider drag:
- Compute current phase from substance's phase diagram boundaries.
- If phase changed, smoothly transition the molecule sim (don't snap).
- Update phase label and crosshair position.
- Update `AppStateProvider.activePhaseSubstance` and `currentPhase`.

## Tutor tools

```
show_phase(substance: string, temperature_k: float, pressure_atm: float)
explain_phase_transition(substance: string, from: string, to: string)
```

Now "show me what happens to water at -10 degrees" → tutor calls
`show_phase("H2O", 263, 1)` AND explains. The chamber spawns at that
exact (T, P) point.

## Test checklist

- [ ] Wheel → Phase → chamber spawns with water at room temp, ~50
      molecules flowing as liquid.
- [ ] Drag temperature slider right → at 100 °C, molecules transition
      to gas behavior, label updates to "Gas".
- [ ] Drag temperature slider left → at 0 °C, molecules lock into
      lattice. Lattice has more spacing than the liquid had. "Why ice
      floats" annotation appears.
- [ ] Drag pressure slider far up → at 1000 atm and high T, supercritical
      behavior visible.
- [ ] Switch to CO₂ → diagram changes, dry ice (solid → gas at 1 atm)
      behavior reachable.
- [ ] Crosshair on phase diagram tracks slider values.
- [ ] Asking tutor "what happens to water at -10°C?" both opens the
      chamber and lands at the right setting.

---

# Part 2 — Spectroscopy Mode

## What the user does

Wheel → **Spectrum**. Two things appear:
- A virtual "flame" object hovering in front of the user (just a stylized
  flame sprite — it's a metaphor).
- A spectrum strip across the bottom of the AR view: a horizontal bar
  showing the visible spectrum from 400 nm (violet) to 700 nm (red),
  initially empty.

User grabs any element cube (existing grab interaction) and drops it
into the flame. The flame changes color to match the dominant emission,
and **emission lines for that element appear on the spectrum strip** —
not pretty approximations, actual recorded wavelengths. Each line has
a small intensity bar.

For elements with no visible-light emission (helium has, but plutonium
mostly doesn't), the strip shows UV/IR markers at the edges with a
note "most emission outside visible range".

Tap a spectral line → opens a popup explaining which electron transition
produced it (uses the Bohr model engine you already have to animate the
electron drop).

A toggle below the strip switches between:
- **Emission** (default) — bright lines on dark background.
- **Absorption** — dark lines on bright background. Show the same lines.
  This is how astronomers actually identify elements in stars.

A "Stars" button reveals an overlay of three real spectra (Sun, Vega,
Betelgeuse) at small scale below — and tapping one matches its absorption
features to elements the user has dropped into the flame.

## Files

```
Assets/
  Resources/
    EmissionLines.json
  Scripts/
    Spectroscopy/
      EmissionLineCatalog.cs
      SpectrumStrip.cs
      SpectrumStripBindings.cs
      FlameTarget.cs
      SpectroscopyController.cs
      SpectroscopyPrefabFactory.cs
```

## `EmissionLines.json`

For each element, the dominant emission lines in nm with relative
intensities. Don't go crazy — 5 to 15 lines per element is plenty. Use
the NIST Atomic Spectra Database values; well-known ones for common
elements:

```json
{
  "elements": {
    "H": {
      "lines": [
        {"wavelength": 656.3, "intensity": 1.00, "transition": "n=3→2 (Balmer α)"},
        {"wavelength": 486.1, "intensity": 0.55, "transition": "n=4→2 (Balmer β)"},
        {"wavelength": 434.0, "intensity": 0.30, "transition": "n=5→2 (Balmer γ)"},
        {"wavelength": 410.2, "intensity": 0.15, "transition": "n=6→2 (Balmer δ)"}
      ]
    },
    "Na": {
      "lines": [
        {"wavelength": 589.0, "intensity": 1.00, "transition": "3p → 3s"},
        {"wavelength": 589.6, "intensity": 0.50, "transition": "3p → 3s (D₁)"}
      ]
    },
    "Ne": {
      "lines": [
        {"wavelength": 640.2, "intensity": 1.00, "transition": "3p → 3s"},
        {"wavelength": 614.3, "intensity": 0.80, "transition": "3p → 3s"},
        {"wavelength": 585.2, "intensity": 0.70, "transition": "3p → 3s"}
      ]
    }
  }
}
```

Cover at least: H, He, Li, Na, K, Mg, Ca, Sr, Ba, Fe, Cu, Ne, Ar, Hg, Au.
Most other common elements should have at least 2 lines. Trace elements
and synthetics can have one approximate line or none.

## `SpectrumStrip.cs`

The visualization. Two modes (emission/absorption), one rendering each:

- **Emission:** black background, render each line as a colored vertical
  bar at its wavelength's screen position, height proportional to
  intensity. Color is approximate sRGB for that wavelength (use the
  standard 380-780 nm → RGB approximation).
- **Absorption:** rainbow gradient background (full visible spectrum),
  render each line as a black vertical bar (gap) at its wavelength.

Width ~70% of screen, anchored along the bottom edge of the AR view but
above the action wheel. Height ~60 px.

Tappable lines: each line has a small invisible collider. Tapping opens
a popup with the transition info.

## `FlameTarget.cs`

A simple AR object that:
- Renders a stylized flame mesh (animated UVs are fine; no need for a
  particle system).
- Listens for grab-release events when the cube being released is
  inside its trigger volume.
- When an element is "consumed", emits its color (flame color = sum of
  RGB at line wavelengths, weighted by intensity) and pulses briefly.

## `SpectroscopyController.cs`

```csharp
public void OpenSession();             // wheel item callback
public void AddElementToFlame(string symbol);
public void ToggleMode(SpectrumMode mode);
public void ShowStellarSpectrum(string starName);   // "sun" | "vega" | "betelgeuse"
public void CloseSession();
```

State:
- Currently active elements (multiple at once — drop Na then drop Li and
  see both sets of lines).
- Current display mode.

`AppStateProvider` adds: `activeFlameElements`, `spectrumMode`.

## Tutor tools

```
ignite_element(symbol: string)
show_stellar_spectrum(star: string)
explain_emission_line(symbol: string, wavelength_nm: float)
```

"Why does sodium glow yellow?" → tutor explains AND drops Na into the
flame.

## Test checklist

- [ ] Wheel → Spectrum → flame and empty spectrum strip appear.
- [ ] Grab sodium cube, drop into flame → flame turns yellow, two lines
      appear near 589 nm.
- [ ] Drop hydrogen → red line at 656 nm appears (Balmer α).
- [ ] Tap the 656 nm line → popup shows "n=3 → n=2" with a small Bohr
      animation of the electron dropping.
- [ ] Toggle to absorption mode → strip becomes a rainbow with black
      gaps where the lines were.
- [ ] Tap "Stars" → Sun's spectrum overlays the strip; tutor can point
      out which features come from hydrogen vs. sodium vs. calcium.
- [ ] Tutor "why is neon orange?" → opens the spectrum bench AND shows
      neon's lines.

---

# Part 3 — pH / Acid-Base Visualizer

## What the user does

Wheel → **pH**. A virtual beaker spawns in AR. Above it, a small "pH
meter" reads 7.0 (pure water). The beaker is filled with ~30 transparent
water molecule sprites moving slowly.

User can:
- Drop in an acid (grab HCl or H₂SO₄ — represented either as compound
  cubes or via a chip palette next to the beaker). The pH meter swings
  toward 1-3. **H⁺ ions appear** as red glowing particles in the water.
- Drop in a base (NaOH or NH₃). pH swings toward 11-13. **OH⁻ ions
  appear** as blue glowing particles.
- Add an indicator (phenolphthalein, litmus, universal). The water
  changes color according to the indicator's response curve to the
  current pH.
- Mix an acid and base: the H⁺ and OH⁻ particles annihilate visibly
  ("neutralization"), forming water molecules. pH drifts toward 7.

A pH strip on the side shows the 0-14 scale with color zones and a
needle. Labels: "Stomach acid", "Lemon juice", "Coffee", "Milk", "Sea
water", "Ammonia", "Bleach" at their real pH values.

## Files

```
Assets/
  Resources/
    AcidsBases.json
  Scripts/
    Acids/
      AcidBaseCatalog.cs
      Beaker.cs
      BeakerBindings.cs
      PhMeter.cs
      IndicatorCurves.cs
      AcidsController.cs
      AcidsPrefabFactory.cs
```

## `AcidsBases.json`

Strong/weak acids and bases with their typical concentrations and pKa:

```json
{
  "acids": {
    "HCl":  {"name": "Hydrochloric Acid", "strength": "strong", "pKa": -7,    "color": "#FFD55B"},
    "H2SO4":{"name": "Sulfuric Acid",      "strength": "strong", "pKa": -3,    "color": "#FFB347"},
    "HNO3": {"name": "Nitric Acid",        "strength": "strong", "pKa": -1.4,  "color": "#FFA040"},
    "CH3COOH":{"name":"Acetic Acid (Vinegar)","strength":"weak", "pKa": 4.76,  "color": "#FFD89A"}
  },
  "bases": {
    "NaOH": {"name": "Sodium Hydroxide (Lye)", "strength": "strong", "pKb": -1, "color": "#5B9CFF"},
    "KOH":  {"name": "Potassium Hydroxide",     "strength": "strong", "pKb": -1, "color": "#5B9CFF"},
    "NH3":  {"name": "Ammonia",                 "strength": "weak",   "pKb": 4.75,"color": "#A0C8FF"}
  },
  "indicators": {
    "phenolphthalein": {"pH_low": 8.2, "pH_high": 10.0, "color_low": "#FFFFFFAA", "color_high": "#FF66CC"},
    "litmus":          {"pH_low": 4.5, "pH_high": 8.3,  "color_low": "#FF6060",   "color_high": "#6060FF"},
    "universal":       {"gradient": true}
  },
  "common_substances": [
    {"name": "Stomach acid",   "pH": 1.5},
    {"name": "Lemon juice",    "pH": 2.0},
    {"name": "Coffee",         "pH": 5.0},
    {"name": "Milk",           "pH": 6.5},
    {"name": "Pure water",     "pH": 7.0},
    {"name": "Sea water",      "pH": 8.1},
    {"name": "Baking soda",    "pH": 9.0},
    {"name": "Ammonia",        "pH": 11.5},
    {"name": "Bleach",         "pH": 12.5}
  ]
}
```

## `Beaker.cs`

A world-space cylinder mesh with a transparent water shader. Inside it:
- A pool of "water molecule" sprites flowing with gentle Perlin-noise
  motion.
- H⁺ ions added when acid is dropped (visible as red glowing spheres).
- OH⁻ ions added when base is dropped (visible as blue glowing spheres).
- When H⁺ meets OH⁻, particle-particle collision triggers an
  annihilation: both vanish, a water molecule sprite appears.
- The water itself tints according to the active indicator's response
  to the current pH.

## `PhMeter.cs`

Displays a numeric pH (one decimal) and a horizontal pH strip with a
moving needle. Annotated with the `common_substances` markers above the
strip.

## Acid/base input

Two ways to add reagents:
1. **Compound palette** — six chips next to the beaker (HCl, H₂SO₄, NaOH,
   NH₃, CH₃COOH, water-to-dilute). Tap to add a "drop".
2. **Cube grab** — if the user grabs Na + Cl + H + O cubes, they can
   assemble NaOH or HCl in a separate compound-builder pop-up. This is
   the bridge to Feature 6 (Bond Builder).

Each "drop" adds a fixed quantity (configurable; default ~0.01 mol/L
equivalent). pH recomputes after every drop.

## pH calculation

For strong acids: `pH = -log10([H+])`.
For weak acids: `[H+] = sqrt(Ka * C)` (good enough at this level).
For mixtures: track total [H+] and [OH-] from all dissociations,
account for autoionization (Kw = 10⁻¹⁴), and clamp to 0-14.

Don't try to be a real chemistry simulator. Get within ±0.3 pH of the
correct answer for the curated reagents and the student won't care.

## Tutor tools

```
add_to_beaker(compound: string)         // "HCl", "NaOH", "phenolphthalein", etc.
empty_beaker()
explain_neutralization()
```

"What happens if I mix vinegar and baking soda?" → tutor adds both,
explains the neutralization producing acetate + CO₂.

## Test checklist

- [ ] Wheel → pH → beaker spawns, pH = 7.0, water is clear.
- [ ] Tap HCl chip → red H⁺ particles appear, pH drops to ~2.
- [ ] Tap NaOH chip → blue OH⁻ particles appear, watch them collide with
      H⁺ particles, both annihilate, pH rises toward 7.
- [ ] Add phenolphthalein → water turns pink only when pH > 8.2.
- [ ] pH meter needle tracks correctly along the labeled strip.
- [ ] Tutor "show me a strong acid" → adds HCl AND explains.

---

# Part 4 — Orbital Viewer (real s/p/d/f)

## What the user does

Grab any element cube. Existing flow → Bohr model appears. **New:** the
Bohr panel now has a toggle "Bohr / Real Orbitals". Tapping "Real
Orbitals" replaces the Bohr concentric rings with the actual quantum-
mechanical electron probability clouds for that element's configuration.

The user can:
- Walk around the orbitals in AR (they float at table height).
- Tap an orbital to see its label (e.g. "2p_z", "3d_xy").
- Toggle "Show all" vs. "Show only filled" vs. "Show only valence".
- Adjust opacity with a slider — orbitals as wireframes vs. solid clouds.

A small ribbon along the bottom shows the electron configuration
notation in spdf format: `1s² 2s² 2p⁶ 3s² 3p⁶ 4s²` etc., highlighted to
match the rendered orbitals.

## Files

```
Assets/
  Scripts/
    Orbitals/
      OrbitalShapes.cs                  // procedural mesh generation
      OrbitalRenderer.cs
      ElectronConfigCalculator.cs
      OrbitalController.cs
      OrbitalPrefabFactory.cs
```

No JSON for this one — orbital shapes are generated procedurally from
the wavefunctions, and electron configurations are derivable from
atomic number.

## `OrbitalShapes.cs`

Procedural mesh generation. The hydrogen-like wavefunctions for s, p, d,
f orbitals have known closed-form expressions; you can sample them on a
3D grid and either:
- Use marching cubes to extract an isosurface at a chosen probability
  threshold (e.g. 90% containment), OR
- Render as a volumetric point cloud with density proportional to |ψ|².

For an AR app on a phone, marching cubes is fast enough and produces
better-looking results. Cache the meshes — they don't depend on the
element, only on the orbital type.

Meshes to generate:
- **s** (1 shape): sphere.
- **p** (3 shapes): three dumbbells along x, y, z axes.
- **d** (5 shapes): four clover-leafs plus one dumbbell with a torus.
- **f** (7 shapes): more complex; pre-generate them once.

Optional: skip f orbitals if you're short on time. Most students never
need them.

## `ElectronConfigCalculator.cs`

Given an atomic number, return the electron configuration as a list of
(shell, subshell, electron_count) tuples. Use the aufbau order
(1s, 2s, 2p, 3s, 3p, 4s, 3d, 4p, 5s, 4d, 5p, 6s, 4f, 5d, 6p, 7s, 5f,
6d, 7p) with the known exceptions (Cr, Cu, Nb, Mo, Ru, Rh, Pd, Ag, Pt,
Au, etc. — there are 19 of them; bake the list in).

## `OrbitalRenderer.cs`

Given a configuration, instantiate the right orbital meshes at the right
positions/orientations around a central nucleus point. Color by shell
(use a consistent palette: n=1 → red, n=2 → orange, n=3 → yellow, n=4 →
green, n=5 → blue, n=6 → purple, n=7 → pink).

Filter modes:
- "All filled" — every occupied orbital.
- "Valence only" — outermost shell's orbitals only.

Use semi-transparent emissive materials so multiple overlapping orbitals
remain legible.

## `OrbitalController.cs`

Hooks into the existing Bohr-model panel. When the user toggles "Real
Orbitals", swap the Bohr visualization for the orbital renderer.

```csharp
public void ShowOrbitals(string symbol, OrbitalFilter filter);
public void HideOrbitals();
```

`AppStateProvider` adds: `orbitalViewActive`, `orbitalViewElement`.

## Tutor tools

```
show_orbitals(symbol: string, filter: string)   // "all" | "valence"
explain_orbital(orbital_label: string)          // e.g. "2p_x"
```

"Why does carbon form four bonds?" → tutor explains AND shows carbon's
orbitals AND highlights the four sp³ hybrid bonds (you can render
hybrids as a 5th orbital filter mode if you want; not required).

## Test checklist

- [ ] Grab carbon cube → Bohr appears → toggle "Real Orbitals" → 1s²
      2s² 2p² renders as: a small inner sphere, a larger sphere, two
      perpendicular dumbbells.
- [ ] Walk around in AR — orbitals remain locked in 3D space.
- [ ] Tap an orbital → label "2p_x" appears.
- [ ] Switch to "Valence only" → only the 2s and 2p orbitals show.
- [ ] Configuration ribbon at the bottom highlights "2p²" while p
      orbitals are being viewed.
- [ ] Grab iron → 3d⁶ orbitals render correctly (4 cloverleafs + 1
      torus-dumbbell + the s orbital, all colored "n=3").

---

# Part 5 — Equilibrium & Kinetics Bench

## What the user does

Wheel → **Equilibrium**. A reaction chamber spawns, similar to the phase
chamber but optimized for showing two competing reactions:

```
A + B ⇌ C + D
```

The user picks a reaction from a small catalog (default: N₂ + 3H₂ ⇌ 2NH₃
— the Haber process — because it's the standard textbook example).

Inside the chamber, particles of A, B, C, D bounce around with proper
colors. Collisions between A and B occasionally produce C and D; the
reverse also happens. Over ~30 seconds the system finds equilibrium —
visible as roughly stable particle counts of each species.

Controls below the chamber:
- **Temperature slider** — high T speeds up both directions, shifts
  equilibrium per Le Chatelier's principle (depends on whether the
  reaction is exothermic).
- **Pressure slider** — squeezes the chamber, shifts equilibrium toward
  fewer moles of gas.
- **Add more A / B / C / D** — buttons to add 5 particles of each.
  Watch equilibrium re-establish.

A live graph below shows concentrations of all four species over time.
The graph makes the shift after a perturbation obvious.

## Files

```
Assets/
  Resources/
    Equilibria.json
  Scripts/
    Equilibrium/
      EquilibriumCatalog.cs
      ReactionChamber.cs
      ParticleSim.cs                     // shares lineage with Phase's MoleculeSim
      ConcentrationGraph.cs
      EquilibriumController.cs
      EquilibriumPrefabFactory.cs
```

## `Equilibria.json`

```json
{
  "reactions": [
    {
      "id": "haber",
      "equation": "N₂ + 3H₂ ⇌ 2NH₃",
      "Keq_at_298": 1.6e3,
      "delta_H_kJ": -92,
      "species": {
        "N2":  {"color": "#5B6BFF", "stoich_reactant": 1, "stoich_product": 0},
        "H2":  {"color": "#E0E0E0", "stoich_reactant": 3, "stoich_product": 0},
        "NH3": {"color": "#7FE0FF", "stoich_reactant": 0, "stoich_product": 2}
      }
    },
    {
      "id": "n2o4",
      "equation": "N₂O₄ ⇌ 2NO₂",
      "Keq_at_298": 4.6e-3,
      "delta_H_kJ": 57,
      "species": {
        "N2O4": {"color": "#A0A0A0", "stoich_reactant": 1, "stoich_product": 0},
        "NO2":  {"color": "#FF8033", "stoich_reactant": 0, "stoich_product": 2}
      }
    }
  ]
}
```

Include 4–6 reactions. Beyond Haber and N₂O₄: H₂ + I₂ ⇌ 2HI (the
classic textbook reaction), CO + 2H₂ ⇌ CH₃OH (methanol synthesis),
2SO₂ + O₂ ⇌ 2SO₃ (contact process).

## `ParticleSim.cs`

Like the phase sim, but with reaction rules. Each tick:
- Move particles per gas-phase rules.
- For each particle-particle collision, check if reactants and apply
  forward rate (probability per collision based on T and species).
- For each existing product, apply reverse rate (probability of
  decomposition per tick).

The actual rate constants don't need to be physically real — they need
to produce a Keq that matches the JSON at 298 K. Calibrate empirically
in the editor.

## Tutor tools

```
show_equilibrium(reaction_id: string)
perturb_equilibrium(action: string)     // "add_N2" | "raise_T" | "increase_P"
explain_le_chatelier()
```

"What happens if I add more nitrogen to the Haber process?" → tutor
sets up the chamber, perturbs it, narrates the shift.

## Test checklist

- [ ] Wheel → Equilibrium → chamber spawns with Haber by default.
- [ ] Particles of N₂ + H₂ collide, occasionally produce NH₃.
- [ ] Graph shows concentrations stabilizing over ~30 seconds.
- [ ] "Add more N₂" → 5 N₂ particles appear, system shifts toward more
      NH₃ in the graph.
- [ ] Raise temperature → since Haber is exothermic, equilibrium shifts
      back toward reactants. Graph confirms.
- [ ] Switch reaction to N₂O₄ ⇌ 2NO₂ → orange brown NO₂ particles
      visibly more concentrated at higher temperature.

---

# Part 6 — Bond Builder

## What the user does

Wheel → **Bond Builder**. An empty workspace appears 60 cm in front of
the user. The user grabs element cubes (existing flow) and **snaps**
them together. The app:

- Calculates valid bonds based on each atom's valence electrons.
- Snaps atoms to physically correct bond angles (109.5° for sp³, 120°
  for sp², 180° for sp).
- Shows bond lines (single = thin, double = medium, triple = thick).
- Updates a formula readout in real time: "C + 4H = CH₄, methane".
- Computes formal charges; if the molecule is unstable, shows a red
  warning indicator.
- When the user finishes (taps Done), evaluates the molecule and shows
  its name, geometry, and a 1-sentence description.

A "Challenge" mode: tutor (or a preset) asks "build water" or "build
methane" or "build a molecule with a triple bond". Correct answer
triggers a celebration; incorrect prompts feedback.

## Files

```
Assets/
  Resources/
    MoleculeRecipes.json                 // for "challenge" answers
  Scripts/
    BondBuilder/
      AtomNode.cs
      BondLine.cs
      ValenceCalculator.cs
      GeometryResolver.cs
      MoleculeNamer.cs
      BondBuilderController.cs
      BondBuilderPrefabFactory.cs
```

## `ValenceCalculator.cs`

Given a symbol, return:
- Valence electron count (column number for main-group; special cases
  for transition metals — for the bond builder, restrict to main-group
  to keep it simple).
- Typical bonding behavior (C → 4 bonds, N → 3, O → 2, H → 1, halogens
  → 1).

## `GeometryResolver.cs`

VSEPR rules in code. Given a central atom and its bonded neighbors plus
lone pairs, return target bond angles. Main shapes only:
- Linear (180°)
- Trigonal planar (120°)
- Tetrahedral (109.5°)
- Trigonal pyramidal (107°, like NH₃)
- Bent (104.5°, like H₂O)

Skip seesaw, T-shape, square planar, octahedral. They matter for
transition metals you're not supporting.

## `MoleculeNamer.cs`

For the curated recipe list (H₂O, NH₃, CH₄, CO₂, C₂H₄, C₂H₂, CH₃OH,
HCl, H₂SO₄, NaCl, etc. — ~30 molecules), match the built atom graph
against the recipe and return the name. For molecules not in the
catalog, return "unknown molecule" gracefully — don't try to be a real
naming engine.

## Snapping mechanics

This is the hardest part of the feature. When the user grabs atom B
and brings it within ~5 cm of atom A:
- If a bond is valid (A has unfilled valence AND B has unfilled valence),
  show a ghost bond preview.
- On release, lock B to A at the correct distance and angle.
- Atoms become draggable as a unit afterward.

Use a simple graph data structure under the hood (atoms = nodes, bonds
= edges). Re-resolve geometry whenever the graph changes.

## Tutor tools

```
challenge_build(target: string)           // "water" | "methane" | "ammonia"
evaluate_molecule()                       // returns current molecule's name/status
explain_geometry(molecule_name: string)
```

"Build a water molecule" → tutor sets up a Challenge prompt → user
snaps H + O + H → tutor confirms or corrects.

## Test checklist

- [ ] Wheel → Bond Builder → empty workspace, no atoms.
- [ ] Grab oxygen, place it. Grab hydrogen, bring near O → ghost bond
      preview. Release → bond snaps at correct distance.
- [ ] Grab another H → second O-H bond at 104.5°. Formula readout shows
      "H₂O, water".
- [ ] Grab another H, try to bond to O → ghost preview shows red (O is
      full).
- [ ] Tutor challenge "build methane" → user assembles C + 4H → tutor
      says "correct, that's methane, CH₄".

---

# Part 7 — Half-Life Simulator

## What the user does

Wheel → **Half-Life**. A bench spawns showing 1000 small spheres
representing atoms of a chosen isotope. A control strip lets the user:
- Pick the isotope from a list (C-14, U-238, U-235, K-40, I-131, Tc-99,
  H-3 tritium, Po-210).
- Hit "Run" — time begins flowing at the user's chosen rate (1×, 100×,
  1000×, or "to completion").
- Watch atoms decay one by one — each fades to a darker color (the
  daughter isotope) at a probabilistic rate matching the half-life.

A live counter shows: "Original: 437. Decayed: 563. Time elapsed:
12,340 years." A logarithmic graph of remaining-parent vs. time tracks
the curve.

A "Use Case" panel below shows what this isotope is used for. C-14 →
archaeological dating, with a slider to "date" a sample. U-235 →
nuclear fuel + weapons explanation. K-40 → radiometric dating of rocks.
I-131 → thyroid medicine.

## Files

```
Assets/
  Resources/
    Isotopes.json
  Scripts/
    HalfLife/
      IsotopeCatalog.cs
      AtomCloud.cs
      DecayGraph.cs
      HalfLifeController.cs
      HalfLifePrefabFactory.cs
```

## `Isotopes.json`

```json
{
  "isotopes": [
    {
      "id": "C-14",
      "parent_symbol": "C", "mass_number": 14,
      "half_life_seconds": 1.808e11,
      "half_life_display": "5,730 years",
      "decay_mode": "β⁻",
      "daughter": "N-14",
      "use": "Radiocarbon dating",
      "story": "Living things constantly exchange carbon with the atmosphere, so the ratio of C-14 to C-12 stays roughly constant. When something dies, the C-14 starts decaying. Measuring the remaining ratio tells you how long ago death occurred."
    },
    {
      "id": "U-238",
      "parent_symbol": "U", "mass_number": 238,
      "half_life_seconds": 1.41e17,
      "half_life_display": "4.5 billion years",
      "decay_mode": "α (chain to Pb-206)",
      "daughter": "Pb-206",
      "use": "Dating Earth & rocks",
      "story": "U-238's half-life is roughly the age of the Earth. Half of all the U-238 that existed when Earth formed is still here. The other half has decayed to lead."
    },
    {
      "id": "I-131",
      "parent_symbol": "I", "mass_number": 131,
      "half_life_seconds": 691200,
      "half_life_display": "8.02 days",
      "decay_mode": "β⁻",
      "daughter": "Xe-131",
      "use": "Thyroid medicine",
      "story": "I-131 concentrates in the thyroid. Doctors use it to image thyroid function or to destroy overactive thyroid tissue. Its short half-life means it disappears from the body in weeks."
    }
  ]
}
```

Include 8–10 isotopes.

## `AtomCloud.cs`

1000 instanced spheres. Each frame:
- For each parent atom, decay probability per tick = `dt * ln(2) /
  half_life` (rescaled by the time-speed multiplier).
- On decay, swap material to "daughter" color and continue.

Don't run 1000 individual coroutines. One loop over the array per frame.

## Tutor tools

```
simulate_half_life(isotope_id: string)
date_sample(isotope_id: string, fraction_remaining: float)
```

"How does carbon dating work?" → tutor sets up C-14, runs it, then
demonstrates a "date this bone" interaction with a sample that's 50%
decayed (= one half-life ≈ 5,730 years old).

## Test checklist

- [ ] Wheel → Half-Life → 1000 C-14 atoms appear in a cube.
- [ ] Hit Run at 1000× → atoms slowly fade to darker color (N-14).
- [ ] Counter and graph update in real time.
- [ ] After ~5,730 simulated years, ~500 atoms remain as C-14.
- [ ] Switch to U-238 → essentially nothing decays in human-observable
      time at 1000× → "to completion" mode jumps directly to 50%
      remaining at 4.5 billion years.
- [ ] Tutor "date this bone, 25% C-14 remaining" → calculates ~11,460
      years old AND shows the chart at that point.

---

# Part 8 — Element of the Day

## What the user does

Opens the app. Above the wheel (when collapsed), a small "Today's
Element" card slides in once per session per day. Shows the element
symbol, name, and a one-sentence hook. Tap to expand into a 60-second
read: history, where it's found, why it matters.

A streak counter ("3 days in a row!") appears in the corner. Streaks
reset if the user misses a day. No punishments, just gentle reinforcement.

The 365-day rotation is deterministic — same element on the same
calendar day every year, so two students checking on March 14 both
see the same element (π element, atomic number 314… kidding;
mathematically pick something like atomic number 1 + (day_of_year * 17)
mod 118, then map through a curated rotation if you want hand-picking
for "good story days" like Jan 1 = Hydrogen, Aug 6 = Uranium for
Hiroshima anniversary, etc.).

## Files

```
Assets/
  Resources/
    ElementOfTheDay.json
  Scripts/
    DailyElement/
      DailyElementSelector.cs
      DailyElementCard.cs
      DailyElementBindings.cs
      StreakTracker.cs
      DailyElementController.cs
```

## `ElementOfTheDay.json`

A list of 118 hand-curated stories — one per element. Same shape as the
existing element data plus a `daily_story` field per element. Reuse
existing element info; just add the story.

```json
{
  "stories": {
    "H":  {"hook": "The atom that started everything.", "story": "Nearly all the hydrogen in the universe — and in every glass of water you've ever drunk — was made in the first three minutes after the Big Bang. Stars are still fusing it to power the universe."},
    "Cu": {"hook": "The first metal humans worked, around 9000 BC.", "story": "Copper was the bridge between the Stone Age and the Bronze Age. Today it's the second-best electrical conductor (after silver) and naturally antimicrobial — door handles made of it actually reduce disease transmission."}
  }
}
```

Cover all 118.

## `StreakTracker.cs`

Stored in PlayerPrefs:
- `daily_last_seen_iso` — the date of the user's last "open".
- `daily_streak` — current streak length.
- `daily_record` — best streak ever.

On app start:
- Read last seen. If yesterday (UTC): increment streak. If today: do
  nothing. If older: reset streak to 1 (today counts).

## Tutor integration

When the user opens the daily element, push to `AppStateProvider`:
`todaysElement = "Cu"`. The tutor's system prompt knows; if the user
asks anything copper-related, it can reference "you noticed today's
element is copper — here's how it relates to your question".

## Test checklist

- [ ] First launch of the day → daily card slides up above the wheel.
- [ ] Tap to expand → 60-second story panel.
- [ ] Dismiss → card stays dismissed for the rest of the day.
- [ ] Re-open the app same day → card doesn't reappear.
- [ ] Re-open next day → new element card.
- [ ] Streak counter shows "1 day", "2 days", "3 days" as you return.
- [ ] Skip a day → streak resets cleanly without negative messaging.

---

# Part 9 — Famous Experiments Mode

## What the user does

Wheel → **Experiments**. A menu of historical experiments. Each is a
short interactive AR scene with narration:

- **Rutherford's Gold Foil (1909):** A gold foil sheet appears in AR.
  Tap "Fire" to launch alpha particles at it. Most pass through; ~1 in
  8000 deflects dramatically. The tutor (or a Rutherford "narrator")
  explains what this proved (atoms are mostly empty with a tiny dense
  nucleus).
- **Thomson's Cathode Ray (1897):** A cathode ray tube apparatus. User
  adjusts the magnetic field and watches the beam bend. Demonstrates the
  electron has mass and charge.
- **Millikan's Oil Drop (1909):** Oil droplets fall. User adjusts the
  electric field to balance one. Measure its charge; quantize it. Find
  e ≈ 1.6×10⁻¹⁹ C.
- **Mendeleev's Predictions (1869):** The user sees Mendeleev's original
  table with gaps. Tap a gap → see what Mendeleev predicted (mass,
  density, properties). Then watch the gap fill as Ga (1875), Sc (1879),
  Ge (1886) are discovered.
- **Curies & Radium (1898):** A pile of pitchblende rendered in AR. The
  user "extracts" radium across several steps; the chemistry is glossed
  but the scale is the point — tons of ore for a few mg of radium.
- **Bohr & the Hydrogen Spectrum (1913):** Connects to the spectroscopy
  bench. Bohr's energy-level diagram appears alongside an electron
  jumping between shells, emitting photons that match the Balmer lines.

Each experiment ends with a "What this means" panel and a short tutor
prompt encouraging further questions.

## Files

```
Assets/
  Resources/
    Experiments.json
  Scripts/
    Experiments/
      ExperimentBase.cs                  // shared narration + flow
      RutherfordExperiment.cs
      ThomsonExperiment.cs
      MillikanExperiment.cs
      MendeleevExperiment.cs
      CurieExperiment.cs
      BohrExperiment.cs
      ExperimentsController.cs
      ExperimentsPrefabFactory.cs
```

Each experiment is essentially a small scripted scene. Don't over-engineer
a generic "experiment framework" — write each one straightforwardly.
They share narration UI but not much else.

## Tutor tools

```
run_experiment(experiment_id: string)
```

"How did we figure out atoms have a nucleus?" → tutor calls
`run_experiment("rutherford")` AND explains alongside the animation.

## Test checklist

- [ ] Wheel → Experiments → menu of 6 experiments with thumbnails.
- [ ] Tap Rutherford → gold foil scene spawns in AR. Tap Fire repeatedly
      → particles pass through with occasional dramatic deflection.
- [ ] After ~30 fires, narration concludes: atoms must have a tiny dense
      core.
- [ ] Tap Millikan → balance an oil drop, read off the charge. Quantum
      of charge derivable after a few trials.
- [ ] Tap Mendeleev → original 1869 table appears with gaps.
- [ ] Tap a gap (eka-silicon, where Ge later was found) → Mendeleev's
      predictions appear → tap "Discovery" → gap fills with Ge.

---

# Part 10 — Planetary Abundance Overlay

## What the user does

This is the easiest feature. Wheel → **Abundance**. Opens a small chooser:
- Universe
- Earth's crust
- Ocean
- Atmosphere
- Human body
- Sun's photosphere
- Jupiter

Pick one. The table re-colors by abundance — log scale, viridis gradient
(like Trends from v2). The story flips depending on what you pick:
universe is overwhelmingly H + He; Earth's crust is O + Si + Al + Fe;
human body is O + C + H + N. The same table tells different stories.

## Files

```
Assets/
  Resources/
    Abundances.json
  Scripts/
    Abundance/
      AbundanceCatalog.cs
      AbundanceOverlayController.cs
```

This reuses 90% of v2's `HeatmapOverlayController` machinery. The
abundance overlay is just another property, with a different data
source for each context.

## `Abundances.json`

Mass fraction per element per context. Use Lodders 2003 / Cox 2000 /
SSEN-style published values:

```json
{
  "contexts": {
    "universe":     {"H": 0.74, "He": 0.24, "O": 0.01, "C": 0.005, "...": "..."},
    "earth_crust":  {"O": 0.461, "Si": 0.282, "Al": 0.082, "Fe": 0.056, "...": "..."},
    "ocean":        {"O": 0.857, "H": 0.108, "Cl": 0.019, "Na": 0.011, "...": "..."},
    "atmosphere":   {"N": 0.755, "O": 0.232, "Ar": 0.013, "...": "..."},
    "human_body":   {"O": 0.65,  "C": 0.18,  "H": 0.10,  "N": 0.03, "Ca": 0.014, "...": "..."},
    "sun":          {"H": 0.74,  "He": 0.25, "...": "..."},
    "jupiter":      {"H": 0.75,  "He": 0.24, "...": "..."}
  }
}
```

Fill in for all 118; elements with negligible abundance get 1e-15 or
similar.

## Tutor tools

```
show_abundance(context: string)
clear_abundance()
```

"Why is the universe mostly hydrogen?" → tutor explains AND switches
to the universe overlay.

## Test checklist

- [ ] Wheel → Abundance → context picker appears.
- [ ] Pick "Universe" → H and He glow brightest, everything else dim.
- [ ] Pick "Earth's crust" → O, Si, Al, Fe stand out; H is dim.
- [ ] Pick "Human body" → O, C, H, N glow; Si is dim.
- [ ] Side-by-side comparison: switching between Earth's crust and
      human body shows the dramatic Si vs. C inversion.

---

# Part 11 — Compound Scanner (OCR)

## What the user does

Wheel → **Compound Scan**. The camera goes into a "text mode" — point
at any chemical formula or ingredient label and the app reads it.

Examples:
- Point at a vitamin bottle label → "C₆H₈O₆ (Ascorbic acid / Vitamin C)
  detected. Made of C, H, O. Used as an antioxidant."
- Point at a textbook formula `H₂SO₄` → identifies sulfuric acid,
  explains composition.
- Point at a periodic table in a book → identifies which element
  you're pointing at and suggests grabbing it in AR.

Architecture:
1. Same JPEG capture as the existing scanner.
2. POST to a new endpoint: `https://tpu.gonzalezerik.com/ocr` (you'll
   need to add this to the server — see Server Addition section below).
3. Server OCRs the image (using Tesseract or PaddleOCR — Tesseract is
   simpler and good enough for chemical formulas).
4. Server extracts candidate formulas via regex (`[A-Z][a-z]?\d*` etc.)
   and looks them up in a small compound database.
5. Server returns identified compounds + their composition + an
   explanation.

## Server addition

Add to `coral-detect-server`:

```python
# additional endpoint in server.py
import re
import pytesseract

@app.route("/ocr", methods=["POST"])
def ocr_route():
    if "image" not in request.files:
        return jsonify({"error": "missing image"}), 400
    raw = request.files["image"].read()
    img = Image.open(io.BytesIO(raw))
    text = pytesseract.image_to_string(img)
    # Extract chemical formulas
    formulas = re.findall(r'(?:[A-Z][a-z]?\d*)+', text)
    return jsonify({"text": text, "formulas": formulas})
```

Add `tesseract-ocr` and `python3-pytesseract` to the Dockerfile.
Rebuild + redeploy.

## Unity files

```
Assets/
  Resources/
    Compounds.json
  Scripts/
    CompoundScan/
      CompoundCatalog.cs
      OcrClient.cs
      CompoundScanController.cs
      CompoundResultPopup.cs
```

## `Compounds.json`

Curated lookup table mapping formulas to names and contexts:

```json
{
  "compounds": {
    "H2O":     {"name": "Water",            "category": "common",   "story": "..."},
    "NaCl":    {"name": "Sodium Chloride",  "category": "common",   "story": "..."},
    "C6H8O6":  {"name": "Ascorbic Acid (Vitamin C)", "category": "vitamin", "story": "..."},
    "C8H10N4O2":{"name": "Caffeine",         "category": "drug",     "story": "..."},
    "CH3COOH": {"name": "Acetic Acid (Vinegar)", "category": "household","story": "..."}
  }
}
```

Cover ~200 common compounds.

## Tutor integration

Add tool:
```
identify_compound(formula: string)
```

The tutor can take an OCR result and produce a richer explanation than
the lookup alone.

## Test checklist

- [ ] Wheel → Compound Scan → camera enters OCR mode.
- [ ] Point at "NaCl" on paper → popup identifies "Sodium Chloride
      (Table Salt)" with composition.
- [ ] Point at "H₂SO₄" → identifies sulfuric acid.
- [ ] Point at unrecognized formula → "Couldn't identify this; try
      asking the tutor about it."
- [ ] Add `text` field for non-formula OCR results — point at "vitamin
      C 500mg" and the tutor can use the raw text to explain.

---

# Part 12 — Contextual Quizzing

## What the user does

Wheel → **Quiz Me**. The tutor (Qwen) generates a question on the spot
based on what the user has been doing in this session:

- If the user just scanned a laptop → "I see you found copper in that
  laptop. Why is copper used in wires instead of, say, aluminum?"
- If the user is viewing the electronegativity heatmap → "Looking at
  the colors, can you tell me which two elements would form the most
  polar bond?"
- If the user grabbed sodium recently → "You looked at sodium. What
  happens when you drop sodium in water, and why?"

User answers via text or voice (voice not implemented this pack; text
only). The tutor evaluates and either confirms, corrects, or asks a
follow-up.

This is a thin wrapper around the existing tutor — the new bits are:
- A "Quiz Me" wheel action.
- A specialized system prompt that switches the tutor into Socratic
  mode.
- A streak/score tracker (optional but motivating).

## Files

```
Assets/
  Scripts/
    Quiz/
      QuizController.cs
      QuizPromptBuilder.cs
      QuizScoreTracker.cs
```

## `QuizPromptBuilder.cs`

Generates a session-aware system prompt:

```
You are a Socratic chemistry tutor. The student just took a quiz
break. Generate one question based on what they've been exploring.

Recent session context:
{AppStateProvider.DescribeForPrompt()}
Last 5 things they interacted with:
- Held element: Cu, Cu, Na
- Scanned: laptop, banana
- Active heatmap: electronegativity

Ask one open-ended question that probes understanding of one of
these. Don't ask multiple-choice questions; ask questions that
require the student to reason. After they answer, evaluate
honestly — correct → congratulate, partial → fill in the gap,
wrong → guide them toward the answer without giving it away
immediately.
```

## Tutor tools

The quiz controller doesn't add tools — it uses the existing tutor
infrastructure with a specialized prompt.

## Test checklist

- [ ] After grabbing a few elements and scanning an object, wheel →
      Quiz Me → tutor asks a question grounded in what was just done.
- [ ] User answers → tutor evaluates fairly (not just "great!").
- [ ] After 5 correct in a row, small "5 streak!" celebration.

---

# Part 13 — Lab Safety / Dangerous Combos

## What the user does

Wheel → **Safety**. Two surfaces:

1. A reference list of common dangerous chemistry combinations every
   household has but most people don't know about: bleach + ammonia,
   bleach + vinegar, hydrogen peroxide + vinegar, drain cleaner + many
   things, etc. Each entry explains:
   - Why it's dangerous (the chemistry)
   - What gas/result is produced
   - Severity
   - First aid
2. In the existing Mix bench, if the user attempts a dangerous combo,
   the bench refuses to react and instead shows a Safety card explaining
   the danger. This is implemented as additional entries in `Reactions.json`
   with a new `"dangerous": true` flag.

This is light on code, heavy on content. The content is the feature.

## Files

```
Assets/
  Resources/
    DangerousCombos.json
  Scripts/
    Safety/
      SafetyCatalog.cs
      SafetyCard.cs
      SafetyController.cs
```

## `DangerousCombos.json`

```json
{
  "combos": [
    {
      "id": "bleach_ammonia",
      "ingredients": ["bleach (NaClO)", "ammonia (NH₃)"],
      "result": "Chloramine gas (NH₂Cl)",
      "severity": "severe",
      "story": "Mixing bleach with ammonia produces chloramine gas — a respiratory irritant that can cause lung damage. Both are common cleaners; many people don't know they shouldn't be mixed.",
      "first_aid": "Move to fresh air immediately. Seek medical attention for severe exposure."
    },
    {
      "id": "bleach_acid",
      "ingredients": ["bleach (NaClO)", "any acid (vinegar, descaler)"],
      "result": "Chlorine gas (Cl₂)",
      "severity": "severe",
      "story": "Bleach plus acidic cleaners (vinegar, lime descaler, toilet bowl cleaner) releases chlorine gas — the same chemical weapon used in WWI. Even small amounts irritate lungs and eyes.",
      "first_aid": "Move to fresh air immediately. Severe exposure requires hospital care."
    },
    {
      "id": "h2o2_vinegar",
      "ingredients": ["hydrogen peroxide", "vinegar"],
      "result": "Peracetic acid",
      "severity": "moderate",
      "story": "Some recipes online suggest mixing 3% hydrogen peroxide and vinegar as a 'natural' disinfectant — but the resulting peracetic acid is corrosive to skin, eyes, and lungs.",
      "first_aid": "Rinse skin/eyes thoroughly with water if exposed."
    }
  ]
}
```

Add 8–10 entries. Don't lecture; just be informative.

## Mix bench integration

Add a "dangerous" reaction type to v2's `Reactions.json`:

```json
{
  "id": "bleach_ammonia_danger",
  "reactants": ["bleach", "ammonia"],
  "dangerous": true,
  "combo_id": "bleach_ammonia"
}
```

In `ReactionController.OnIgnitePressed`, if the matched reaction has
`dangerous: true`, show the Safety card instead of the normal reaction
animation.

## Tutor tools

```
show_safety_card(combo_id: string)
```

Tutor primed to recognize unsafe combinations in conversation:
"Can I mix bleach and vinegar to clean the bathroom?" → tutor refuses
to recommend it AND surfaces the Safety card.

## Test checklist

- [ ] Wheel → Safety → list of dangerous combos.
- [ ] Tap an entry → card with story + first aid.
- [ ] In Mix bench, attempt bleach + ammonia → reaction refuses, Safety
      card surfaces instead.
- [ ] Ask tutor "is it safe to mix bleach and vinegar?" → clear "no,
      because chlorine gas" answer.

---

# Part 14 — Offline Mode

## What the user does

Settings → toggle **Offline Mode** ON. The app now:
- Hides the Scan, Compound Scan, Tutor, and Quiz Me wheel items.
- All other features (Trends, Origin, Scale, Mix, Phase, Spectrum, pH,
  Orbitals, Equilibrium, Bond Builder, Half-Life, Element of the Day,
  Experiments, Abundance, Household, Safety) work fully.
- Status pill shows a small "Offline" indicator.

Auto-detect network: if a scan call times out twice in a row, prompt
"Looks like the network is down. Switch to Offline Mode?".

## Files

```
Assets/
  Scripts/
    Offline/
      OfflineModeController.cs
      NetworkSentinel.cs
```

This is sweeping work: every controller that requires network must
check `OfflineModeController.IsOffline` at the wheel-registration step
and hide its item if true. The status pill picks up the offline state
on launch.

## Test checklist

- [ ] Settings → Offline Mode ON → Scan, Compound Scan, Tutor, Quiz Me
      disappear from the wheel.
- [ ] All offline features still work.
- [ ] Settings → Offline Mode OFF → buttons return without restart.
- [ ] Disable wifi, attempt to scan twice → app prompts to switch to
      offline mode.

---

# Cross-cutting work

## Tutor tool registry — final v3 list

After v3 the registry has roughly these (existing v1/v2 + new):

```
// v1/v2
highlight_elements, focus_element, clear_highlights, show_bohr_model,
compare_elements, filter_table_by, suggest_reaction, show_heatmap,
clear_heatmap, show_origin_overlay, clear_origin_overlay,
show_atom_scale, show_mole, where_do_i_find

// v3
show_phase, explain_phase_transition,
ignite_element, show_stellar_spectrum, explain_emission_line,
add_to_beaker, empty_beaker, explain_neutralization,
show_orbitals, explain_orbital,
show_equilibrium, perturb_equilibrium, explain_le_chatelier,
challenge_build, evaluate_molecule, explain_geometry,
simulate_half_life, date_sample,
run_experiment,
show_abundance, clear_abundance,
identify_compound,
show_safety_card
```

That's ~35 tools. Qwen handles this fine but the model gets slower at
selecting from very large tool lists. Two mitigations:

1. **Tool scoping by app state.** Only send tools relevant to the
   current context. If the user is in the phase bench, send phase tools
   + general tools, not equilibrium tools. Cuts the list to ~12 at any
   moment.
2. **A `general_search` fallback tool.** If Qwen can't find a specific
   tool, it can call `general_search` with a string and get back a
   description of what tools exist matching it. Lets the model
   self-discover without inflating the per-call payload.

## `AppStateProvider` final shape

```csharp
public class AppStateProvider : MonoBehaviour {
    // Existing
    public string heldElementSymbol;
    public string lastScannedObject;
    public List<string> lastScannedElements;
    public List<string> currentlyHighlighted;
    public string lastReactionProduct;
    public string activeHeatmap;
    public bool   originOverlayActive;
    public string activeScaleMode;

    // v3 additions
    public string activePhaseSubstance;     // "H2O", "CO2", null
    public string currentPhase;             // "solid", "liquid", "gas", null
    public List<string> activeFlameElements;
    public string spectrumMode;             // "emission", "absorption"
    public string beakerState;              // freeform: "acidic", "neutral", "basic"
    public float  currentPh;
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

    public string DescribeForPrompt();      // emits one line per non-null field
}
```

## Performance budget

- The table still has 118 cubes. Heatmaps + Origin + Abundance all
  share the same recoloring path; reuse v2's caching.
- Phase, Spectrum, pH, Equilibrium each spawn a chamber/beaker. Allow
  only **one bench open at a time** — opening a new bench closes any
  previous one. Otherwise the AR scene fills up and frame rate suffers.
- Orbitals are expensive (procedural meshes). Cache the shape meshes
  globally; only the per-element configuration changes between
  invocations.
- Half-life sim with 1000 instanced spheres is fine. Don't go to
  10,000 unless GPU-instanced.

## Test checklist — end-to-end on real device

After v3 is fully built:

1. Launch app → camera fills screen, FAB + Tutor button bottom-anchored,
   nothing on top.
2. Tap FAB → wheel fans up, 3 pages, 18 actions across them.
3. Page 1 → Place table works. Trends, Origin, Abundance work.
4. Page 2 → Scan a laptop (existing flow still works through the new
   wheel). Compound Scan reads a label. Safety entry opens.
5. Page 3 → Open Phase bench, drag temperature, water freezes. Close
   bench. Open Spectrum bench, drop hydrogen → Balmer lines. Close.
   Open pH bench, add HCl → red ions. Close. Open Equilibrium, run
   Haber. Close. Open Bond Builder, build water on tutor's challenge.
   Close. Open Half-Life, run C-14 at 1000×.
6. Open Tutor (bottom right). Ask "why is gold so unreactive?" → tutor
   answers and maybe calls `show_orbitals("Au", "valence")` to
   illustrate.
7. Force airplane mode → after 2 timeouts, app prompts offline mode.
   Accept → Scan/Tutor/etc disappear. All other features still work.
8. Reconnect → Settings → Offline OFF → everything returns.
9. Next morning → open app → Element of the Day card slides up, streak
   counter shows "2 days".

If all of that works, ship it.

---

## Shipping order (estimated calendar time)

If you have one engineer working full-time:

- **Week 1:** Part 0 (UI shell), Part 8 (Element of Day), Part 10
  (Abundance), Part 7 (Half-Life). These are all small to medium and
  give visible wins fast.
- **Week 2:** Part 2 (Spectroscopy), Part 4 (Orbitals). The two
  flagship "wow" features.
- **Week 3:** Part 1 (Phase), Part 3 (pH). The two big simulator
  benches.
- **Week 4:** Part 5 (Equilibrium), Part 9 (Experiments). More benches
  and content.
- **Week 5:** Part 6 (Bond Builder), Part 11 (Compound Scanner + server
  rebuild). The two highest-lift items.
- **Week 6:** Part 12 (Quiz), Part 13 (Safety), Part 14 (Offline). The
  finishing layer.

Total: ~6 weeks for a single engineer, or 3 weeks for two engineers
splitting by feature.

If shipping faster is the priority, cut Parts 6, 11, 12 — they're the
ones whose absence the user won't immediately notice.
