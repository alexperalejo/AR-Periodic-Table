# AR HUD — Scene Wiring

One-time Editor steps to hook up the screen-space HUD (Place/Remove button,
placement instruction, and 10 placeholder category buttons) in
`Assets/Scenes/MainARScene.unity`.

## 1. Generate the HUD prefab

From the top-level Unity menu:

```
Tools > AR Periodic Table > Build AR HUD
```

That writes `Assets/Prefabs/UI/ARHudCanvas.prefab`. The generator is
idempotent — re-running overwrites the prefab cleanly.

## 2. Drop the prefab into the scene

- Drag `ARHudCanvas.prefab` under the scene root.
- Leave its RectTransform at identity; the Canvas is Screen Space Overlay so
  world position is irrelevant.

## 3. Add ARPlaneDetectionWatcher

The HUD asks a watcher component whether any horizontal plane is currently
tracked. Put it wherever you like — a sibling of the AR Session is fine.

- Select **`AR Session`** in the Hierarchy → **Add Component** →
  `ARPlaneDetectionWatcher`.
- Drag the scene's **ARPlaneManager** (on `XR Origin`) into the watcher's
  `Plane Manager` slot.

## 4. Wire TapToPlace's new Inspector slots

`TapToPlace` now needs `ARRaycastManager` in addition to its existing slots
(it does tap raycasts against detected planes rather than auto-spawning).
Find the TapToPlace component (on `XR Origin` in the existing scene) and fill:

| Slot               | Value                                                  |
| ------------------ | ------------------------------------------------------ |
| `Place Prefab`     | unchanged — `pTableGroup`                              |
| `Xr Origin`        | unchanged — `XR Origin (AR Rig)`                       |
| `Ar Camera`        | unchanged — Main Camera                                |
| `Raycast Manager`  | **new** — the ARRaycastManager on `XR Origin`          |
| `Absolute Scale`   | unchanged — 0.3                                        |

## 5. Wire ARHudController

Select the prefab instance `ARHudCanvas` in the scene. Its `ARHudController`
has two external Inspector slots that must be filled (internal slots —
`Place Button`, `Place Button Label`, `Instruction Root`, `Instruction Label`
— are pre-wired by the generator):

| Slot           | Value                                               |
| -------------- | --------------------------------------------------- |
| `Tap To Place` | the `TapToPlace` component on `XR Origin (AR Rig)`  |
| `Plane Watcher`| the `ARPlaneDetectionWatcher` from step 3           |

## 6. Save the scene

`Ctrl/Cmd + S`.

## 7. Runtime behavior to expect

| App state                              | Place button                               | On press               |
| -------------------------------------- | ------------------------------------------ | ---------------------- |
| Launch, no plane detected yet          | Greyed out, label "Place"                  | Nothing (disabled)     |
| First horizontal plane detected        | Enabled, label "Place"                     | Arms placement         |
| Armed (instruction overlay visible)    | Enabled, label "Place"                     | Cancels (re-idles)     |
| Armed + user taps on a detected plane  | Switches to label "X" on successful spawn  | —                      |
| Placed                                 | Enabled, label "X"                         | Destroys the table; goes back to Idle |

The 10 category buttons are placeholder visuals only — no `onClick` wiring.
Connect them to `CategoryFilterController` methods
(`OnAlkaliMetals`, `OnHalogens`, …) when you're ready.
