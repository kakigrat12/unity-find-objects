# Find Objects — Spatial Algorithms in Unity/C#

Find Objects is a small Unity prototype built around placing many differently sized objects inside a bounded search area. The portfolio focus is the custom circle-packing implementation and its spatial hash, supported by a lightweight camera/input architecture.

## Engineering highlights

- **Custom spatial hash.** `FastGrid` maps occupied grid cells to circle indices with packed integer coordinates, multiplicative hashing, linear probing, and preallocated linked-entry arrays.
- **Local collision queries.** Candidate validation inspects nearby grid cells rather than scanning every placed circle, reducing the practical cost of dense placement.
- **Heuristic circle packing.** `UltraCirclePacker` generates tangent candidates around recent anchors, adds deterministic angular phases and distance jitter, tries uniform random fallback candidates, and selects the valid point nearest the center.
- **Allocation-conscious hot path.** Reusable buffers, value types, preallocated arrays, squared-distance checks, and aggressive inlining keep the inner collision loop compact.
- **Algorithm/gameplay boundary.** The packer operates on `System.Numerics.Vector2` and radii; `RandomSpawner` adapts Unity colliders and instantiation to that algorithm.
- **Platform-specific input composition.** The bootstrap selects desktop or mobile camera input and drives `ITickable` objects from one update loop.

## Packing pipeline

```text
prefab radii
    ↓
tangent and random candidate generation
    ↓
bounds check → spatial-hash neighborhood lookup → exact circle test
    ↓
lowest-distance valid position
    ↓
grid insertion and Unity instantiation
```

## Suggested code tour

| Area | Representative code | What it demonstrates |
|---|---|---|
| Packing algorithm | [`CirclePacker.cs`](Assets/Game/Scripts/Helpers/CirclePacker.cs) | Hash grid, candidate heuristics, collision queries |
| Unity adapter | [`RandomSpawner.cs`](Assets/Game/Scripts/RandomSpawner.cs) | Translating collider radii into algorithm input and instantiating results |
| Composition | [`GamePlayBootStrap.cs`](Assets/Game/Scripts/BootStarps/GamePlayBootStrap.cs) | Platform-aware input selection and update orchestration |
| Camera boundary | [`CameraController.cs`](Assets/Game/Scripts/CameraMovement/CameraController.cs), [`ICameraInput.cs`](Assets/Game/Scripts/CameraMovement/ICameraInput.cs) | Input abstraction separated from camera behavior |

## Repository scope

This is a source showcase rather than a playable Unity distribution. Art, scenes, prefabs, `.meta` files, generated folders, and vendor packages are intentionally excluded. See [`SOURCE_SCOPE.md`](SOURCE_SCOPE.md).

The original project targets Unity `6000.0.47f1`; its package manifest is retained for technical context.

