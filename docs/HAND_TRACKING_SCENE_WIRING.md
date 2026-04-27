# Hand Tracking — Scene Wiring

These steps wire the Realtime Hand integration into `Assets/Scenes/MainARScene.unity`
and must be performed once in the Unity Editor. The scripts themselves are in
source control; only the scene references and a few Inspector defaults remain to
be set by hand. All steps are LiDAR-iPhone-only at runtime — on non-LiDAR devices
the capability probe flips the system off and nothing in this wiring has any
effect.

## 1. AROcclusionManager on the Main Camera

Path: `XR Origin (AR Rig) > Camera Offset > Main Camera`.

- **Add Component** → `AROcclusionManager`.
- Set **Environment Depth Mode** = `Fastest`.
- Set **Temporal Smoothing Requested** = `false` (off).

Leave Human Segmentation / Human Stencil at their defaults — the package uses
whatever the device exposes.

## 2. HandTrackingSystem GameObject

At the scene root:

- **GameObject > Create Empty**, rename it `HandTrackingSystem`.
- Position / rotation are irrelevant; leave at identity.
- Add three components in this order:
  1. `HandCapabilityProbe`
  2. `RealtimeHandManager`  (from the Realtime Hand package, namespace `RTHand`)
  3. `HandGrabController`

## 3. Wire RealtimeHandManager

On the `HandTrackingSystem` object's `RealtimeHandManager`:

| Inspector slot       | Drag in                                                                 |
| -------------------- | ----------------------------------------------------------------------- |
| `Session`            | The scene-root `AR Session` GameObject                                  |
| `Occlusion Manager`  | `XR Origin > Camera Offset > Main Camera` (carries the new AROcclusionManager) |
| `Camera Manager`     | `XR Origin > Camera Offset > Main Camera` (carries the existing ARCameraManager) |

## 4. Wire HandCapabilityProbe

On the same `HandTrackingSystem` object:

| Inspector slot       | Drag in                                                                 |
| -------------------- | ----------------------------------------------------------------------- |
| `Occlusion Manager`  | `XR Origin > Camera Offset > Main Camera`                               |

## 5. Wire HandGrabController

On the same `HandTrackingSystem` object:

| Inspector slot       | Drag in                                                                 |
| -------------------- | ----------------------------------------------------------------------- |
| `Hand Manager`       | `HandTrackingSystem` (self — the RealtimeHandManager on this object)    |
| `Capability Probe`   | `HandTrackingSystem` (self — the HandCapabilityProbe on this object)    |
| `Tap To Place`       | `XR Origin (AR Rig)` (the object that carries the TapToPlace component) |

Threshold defaults are already set in the component (2.5 cm pinch-down / 4 cm
release / 5 cm grab reach / 0.5 confidence) — leave them alone for the first
device test; tune later if needed.

## 6. iOS deployment target

`Edit > Project Settings > Player > iOS tab > Other Settings`:

- **Target minimum iOS Version** = `14.0` (or higher). Realtime Hand's Vision
  framework calls require iOS 14.

## 7. Apply CubeHandGrabbable to all element prefabs

Run once from the top-level Unity menu:

```
Tools > AR Periodic Table > Add HandGrabbable To Element Cubes
```

This iterates every prefab under `Assets/Prefabs/cubes/` and adds a
`CubeHandGrabbable` component to each. Expect a console line like
`[AddHandGrabbableToElementCubes] Scanned 118 prefabs. Updated 118.`.
Re-running is a no-op (idempotent).

## 8. Save and build

- Save the scene (Cmd/Ctrl+S).
- Build target: iOS, iPhone Pro (LiDAR required).
- First launch will prompt for camera permission (already configured from the
  ARKit setup); grant it.

## 9. What to expect on device

- App launches, camera feed appears, periodic table spawns in front of the camera
  via the existing TapToPlace behavior.
- When you bring your free hand into the camera view, within ~2 seconds you
  should see:
  ```
  [HandCapabilityProbe] LiDAR environment depth: Supported
  [HandGrabController] Hand tracking enabled.
  ```
  in the device log.
- Reach your index fingertip within 5 cm of a cube, then pinch index + thumb:
  the cube detaches, follows your fingertip, and prints
  `[HandGrabController] Grabbed <Element> at distance 0.0xx m`.
- Release the pinch — the cube falls under gravity.
- When the cube collides with a detected AR plane it vanishes and respawns
  in its original table slot after the configured delay (1 s by default).

On non-LiDAR devices the log will say
`[HandCapabilityProbe] LiDAR environment depth: NotSupported` and the hand
system stays dormant; the existing touchscreen XRGrabInteractable path remains
fully functional.
