# Architecture

`ZooBootstrap` is the single composition root and a VContainer `LifetimeScope`. It validates the scene, registers the database and camera, and creates one instance of each service. Services receive dependencies through constructors; actors and HUD receive them during initialization. Only the composition root resolves services from the container.

| System | Responsibility |
| --- | --- |
| `AnimalDatabase` | Species definitions, role defaults and optional pair exceptions |
| `WorldBoundsService` | Camera bounds and return direction |
| `AnimalPopulationService` | Live membership and loss counters |
| `AnimalCollisionService` | Contact outcomes and eating events |
| `AnimalSpawnService` | Spawn timing, species selection, position and direction |
| `PooledAnimalFactory` | Instance creation, activation, rollback and per-species pools |
| `ZooHud` | Counters and pooled Tasty labels |

`ZooBootstrap.Update` refreshes bounds, flushes deferred returns and advances spawning. Each animal runs its assigned movement in `FixedUpdate`. Physical impulses remain in its velocity and gradually recover toward normal movement. Frog height is visual; collision physics stays on the ground plane. The world uses an axis-aligned top-down orthographic camera.

Role defaults cover prey/prey, predator/prey and predator/predator. Optional species-pair rules override those defaults. After validation, an ordered-pair dictionary stores both query directions; absent keys use role defaults. Duplicate unordered pairs are rejected with a HashSet. Validation/indexing takes expected O(S + E), each contact lookup expected O(1), and the index uses O(E) memory. Editor edits and Undo invalidate it; each Play session gets a fresh cache even with domain reload disabled. Eating removes a victim once and counts the victim's role. Paired collision callbacks cannot count or release it twice.

Each `AnimalDefinition` stores movement configuration inline through `SerializeReference`. Named settings classes live in `Animals/Settings`; timers and other mutable state live on movement components. Existing behavior can serve many species without extra service code. The requirement for 1000 different species is a catalog/extensibility requirement, not a measured budget for 1000 simultaneous physics bodies. The catalog references prefabs directly; asset streaming would need separate requirements and memory measurements.

## Creation and lifetime

The scheduler depends on `IAnimalFactory`, so timing can be tested without constructing animals. The factory is a data-driven creation boundary combined with Unity's `ObjectPool<T>`; it does not use the inheritance-based GoF Factory Method pattern or a separate factory per species. Pools are created lazily, with storage initially sized for four references and at most 32 inactive instances retained per species. This is a retention policy, not an active-population limit or a prewarm count.

The factory instantiates under an inactive parent. `AnimalActor.Activate` assigns dependencies, initializes and resets movement, and prepares physics before enabling the instance. Custom `OnEnable` code can use initialized movement configuration; it must not assume population registration is already complete. Keep `Awake` limited to local setup: initialization must not depend on whether Unity has called it yet.

Death immediately removes population membership and disables the actor, matching the task's disappearance rule. Actual return to the reusable pool waits for the next `ZooBootstrap.Update`, after the current physics callback batch. Calling `Spawn` from an event does not flush that queue. Tasty labels track the actor's generation so they cannot follow a reused animal. Activation failures return the rented instance; movement reset failures are logged without abandoning release.

On scene teardown, the HUD first unsubscribes and clears its labels. VContainer disposes the scheduler and factory; the factory clears live animals, releases pending returns and disposes its pools. Nested `finally` blocks continue resource cleanup if population notification throws. Repeated disposal is harmless.

## Extending the catalog

For another species with an existing movement:

1. Duplicate a supplied animal prefab and replace its visuals. Preserve the root actor, movement, dynamic Rigidbody and centered SphereCollider references.
2. Use **Zoo World > Animal Database > Add > Create new species**, select the prefab and role, then edit its movement parameters.
3. Add a pair exception only if the default prey/predator rules do not describe the interaction. Run **Validate** and the test suites.

For a new movement algorithm:

1. Add a serializable `AnimalMovementSettings` subclass with configuration validation.
2. Derive a component from `AnimalMovement`. Implement `SupportsSettings`, `Initialize` and `Tick`; keep mutable state on the component. `Tick` returns planar speed and may change the direction; the actor owns Rigidbody movement.
3. Implement `ResetState` for reuse and safe cleanup after partial initialization. Override `ValidateConfiguration` for required visual references.
4. Add the default settings construction to the Editor-only `MovementSettingsAuthoring` switch and assign the component to a prefab. No factory, scheduler or collision-service change is needed.
5. Add tests for the behavior's contract and reuse. Flight at arbitrary altitude, swimming volumes or runtime movement transitions would require an explicit expansion of the current planar contract.

## Pattern choices

- **Strategy:** the actor delegates movement to its assigned component; species do not form a growing inheritance tree.
- **Factory and Object Pool:** creation and reuse are owned in one place, independently of spawn scheduling.
- **Observer:** population and collision events drive the HUD, with balanced subscriptions. The HUD batches counter refreshes until `LateUpdate`.
- **Shared configuration:** species assets hold data shared by their instances; runtime state remains per animal.

Builder, State, a global event bus and extra factory subclasses have no current requirement. The project uses no global service locator or ECS. The existing composition root and small services remain the organizing structure.

## References

These references informed the responsibilities above; the implementation is adapted to this task rather than copied as a complete framework.

- [Unity 6: Factory](https://learn.unity.com/tutorial/how-to-use-the-factory-pattern-for-object-creation-at-runtime) and [Object Pool](https://learn.unity.com/tutorial/use-object-pooling-to-boost-performance-of-c-scripts-in-unity): separate creation and combine it with reuse.
- [Unity 6: Strategy](https://learn.unity.com/tutorial/strategy-pattern?version=6.0) and [ScriptableObject architecture](https://unity.com/resources/create-modular-game-architecture-scriptableobjects-unity-6): compose behavior and share authored configuration.
- [Refactoring.Guru: factory terminology](https://refactoring.guru/design-patterns/factory-comparison), [Strategy](https://refactoring.guru/design-patterns/strategy) and [Observer](https://refactoring.guru/design-patterns/observer): distinguish the creation boundary from Factory Method and describe behavior/subscription contracts.
- [Unity Test Framework: EditMode and PlayMode](https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/manual/edit-mode-vs-play-mode-tests.html): separate rules and authoring checks from runtime integration tests.
