# Zoo World

A small animal simulation made with Unity **6000.0.61f1**, URP, PhysX and uGUI.

Open the project in that Editor version, let Package Manager restore dependencies, then open `Assets/ZooWorld/Scenes/ZooWorld.unity` and press Play. Git must be installed for the VContainer package to download.

A random animal appears every 1–2 seconds. Prey bounce apart, predators eat prey, and two predators leave one survivor. Animals stay within the camera view. The HUD shows losses and a short **Tasty!** label below the eater.

## Editing animals

Open **Zoo World → Animal Database**. Select a species to edit its prefab, role and movement settings. All movement values belong to its `AnimalDefinition` asset.

- **Add → Existing species** registers an existing definition.
- **Add → Create new species** asks for a name, role and valid animal prefab. It creates matching movement settings automatically.
- **Remove** removes database membership; the asset stays on disk. Undo restores the entry. Remove any pair rules using that species first.

To reuse a movement, duplicate an animal prefab and change its visuals. Keep its root `AnimalActor`, movement, dynamic Rigidbody and SphereCollider references assigned. The supplied frog, snake and beetle show the expected setup. **Validate** checks definitions and prefab configuration.

To add a movement, derive from `AnimalMovement`, put its settings class in `Animals/Settings`, and add its default settings case to the Editor-only `MovementSettingsAuthoring`. Movement state stays on the component; configuration stays in the definition. `ResetState` restores the component for pool reuse. The [extension walkthrough](ARCHITECTURE.md#extending-the-catalog) explains the initialization contract.

## Code

Start with `Assets/ZooWorld/Runtime/ZooBootstrap.cs`. It is the scene's VContainer scope: it connects services and scene references and owns their lifetime. VContainer **1.19.0** is pinned to the [official package](https://github.com/hadashiA/VContainer/releases/tag/1.19.0).

`AnimalSpawnService` chooses when, what and where to spawn. `PooledAnimalFactory` creates and initializes animals and owns their pools. Movement is composed through `AnimalMovement`; adding another species using an existing movement requires assets, not another species-specific service or class.

See [ARCHITECTURE.md](ARCHITECTURE.md) for responsibilities, lifecycle, tradeoffs and source references.

## Tests

The tracked suites are under `Assets/ZooWorld/Tests`. Open **Window > General > Test Runner** and run both **EditMode** and **PlayMode**. EditMode covers interaction rules, configuration, authored assets and movement calculations. PlayMode covers scheduling, animal lifecycle, pooling and scene integration.

With the project closed in the Unity Editor, run both suites from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Test-Zoo.ps1
```

The runner uses the project's pinned Editor version and writes logs and XML results under `TestResults/`. Supply `-UnityPath "C:/path/to/Unity.exe"` for a nonstandard installation. Add `-BuildPlayer` to build a Windows player after both suites pass; the output is `Builds/Validation/Zoo.exe`.

A 1000-species catalog test verifies data and rule handling; it is not a performance benchmark for 1000 simultaneously active physics bodies.
