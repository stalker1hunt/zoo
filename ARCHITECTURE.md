# Architecture

`ZooBootstrap` is the single composition root and a VContainer `LifetimeScope`. It validates the scene, registers the database and camera, and creates one instance of each service. Services receive dependencies through constructors; actors and HUD receive them during initialization. Only the composition root resolves services from the container.

| System | Responsibility |
| --- | --- |
| `AnimalDatabase` | Species definitions, role defaults and optional pair exceptions |
| `WorldBoundsService` | Camera bounds and return direction |
| `AnimalPopulationService` | Live membership and loss counters |
| `AnimalCollisionService` | Contact outcomes and eating events |
| `AnimalSpawnService` | Spawn timing and per-species pools |
| `ZooHud` | Counters and pooled Tasty labels |

`ZooBootstrap.Update` refreshes bounds and advances spawning. Each animal runs its assigned movement in `FixedUpdate`. Physical impulses remain in its velocity and gradually recover toward normal movement. Frog height is visual; collision physics stays on the ground plane.

Role defaults cover prey/prey, predator/prey and predator/predator. Optional species-pair rules override those defaults. After validation, an ordered-pair dictionary stores both query directions; absent keys use role defaults. Duplicate unordered pairs are rejected with a HashSet. Validation/indexing takes expected O(S + E), each contact lookup expected O(1), and the index uses O(E) memory. Editor edits and Undo invalidate it; each Play session gets a fresh cache even with domain reload disabled. Eating removes a victim once and counts the victim's role. Paired collision callbacks cannot count or release it twice.

Each `AnimalDefinition` stores movement configuration inline through `SerializeReference`. Named settings classes live in `Animals/Settings`; timers and other mutable state live on movement components. Existing behavior can serve many species without extra service code. A larger food web could add diet groups when needed.

Rented actors reset movement, position and physics before use. Dead actors wait until the next spawn tick before returning to their pool, preventing reuse during the same collision callback batch. Tasty labels track the actor's generation so they cannot follow a reused animal.

On scene teardown, the HUD first unsubscribes and clears its labels. VContainer then disposes the spawner, which returns active animals and disposes its pools. Cleanup is idempotent. No global service locator or ECS is used.
