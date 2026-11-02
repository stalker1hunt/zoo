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

To add a movement, derive from `AnimalMovement`, put its settings class in `Animals/Settings`, and add its default settings case to the Editor-only `MovementSettingsAuthoring`. Movement state stays on the component; configuration stays in the definition. `ResetState` restores the component for pool reuse.

## Code

Start with `Assets/ZooWorld/Runtime/ZooBootstrap.cs`. It is the scene's VContainer scope: it connects services and scene references and owns their lifetime. VContainer **1.19.0** is pinned to the [official package](https://github.com/hadashiA/VContainer/releases/tag/1.19.0).

See [ARCHITECTURE.md](ARCHITECTURE.md) for the main responsibilities and lifecycle.
