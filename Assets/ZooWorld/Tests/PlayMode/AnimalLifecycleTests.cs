#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    // Use the delivered species/prefabs, so tests also detect broken authoring references.
    public class AnimalLifecycleTests
    {
        private GameObject root;
        private AnimalDefinition frog;
        private AnimalDefinition snake;
        private AnimalPopulationService population;
        private AnimalCollisionService collisions;
        private PooledAnimalFactory factory;
        private AnimalDefinition transientDefinition;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Lifecycle test objects");
            frog = AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/ZooWorld/Species/Frog.asset");
            snake = AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/ZooWorld/Species/Snake.asset");
            var database = AssetDatabase.LoadAssetAtPath<AnimalDatabase>("Assets/ZooWorld/AnimalDatabase.asset");
            Assert.That(frog, Is.Not.Null);
            Assert.That(snake, Is.Not.Null);
            Assert.That(database, Is.Not.Null);
            database.ValidateConfiguration();
            population = new AnimalPopulationService();
            collisions = new AnimalCollisionService(database, population, 5.5f);
            factory = new PooledAnimalFactory(root.transform, new OpenBounds(), collisions, population);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            factory.Dispose();
            Object.Destroy(root);
            if (transientDefinition != null) Object.Destroy(transientDefinition);
            ActivationProbeMovement.ThrowOnNextInitialize = false;
            yield return null;
        }

        [Test]
        public void PairedPredatorPreyCallbacksCountOneDeathAndOneEatingEvent()
        {
            AnimalActor predator = Spawn(snake, -3f);
            AnimalActor prey = Spawn(frog, 3f);
            int events = 0;
            AnimalActor reportedEater = null;
            collisions.AnimalEaten += eater => { events++; reportedEater = eater; };

            collisions.Resolve(prey, predator);
            collisions.Resolve(predator, prey);

            Assert.That(population.Alive, Is.EqualTo(1));
            Assert.That(population.DeadPrey, Is.EqualTo(1));
            Assert.That(population.DeadPredators, Is.Zero);
            Assert.That(prey.IsAlive, Is.False);
            Assert.That(prey.gameObject.activeSelf, Is.False);
            Assert.That(predator.IsAlive, Is.True);
            Assert.That(events, Is.EqualTo(1));
            Assert.That(reportedEater, Is.SameAs(predator));
        }

        [Test]
        public void PredatorPairLeavesExactlyOneSurvivorForEitherCallbackOrder()
        {
            AnimalActor first = Spawn(snake, -3f);
            AnimalActor second = Spawn(snake, 3f);
            int events = 0;
            collisions.AnimalEaten += _ => events++;

            collisions.Resolve(second, first);
            collisions.Resolve(first, second);

            Assert.That(first.IsAlive ^ second.IsAlive, Is.True);
            Assert.That(population.Alive, Is.EqualTo(1));
            Assert.That(population.DeadPredators, Is.EqualTo(1));
            Assert.That(population.DeadPrey, Is.Zero);
            Assert.That(events, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ActualPhysicsContactEatsPreyWithoutDoubleCounting()
        {
            AnimalActor predator = factory.Spawn(snake, new Vector3(-0.2f, 0.5f, 0f), Vector3.right);
            AnimalActor prey = factory.Spawn(frog, new Vector3(0.2f, 0.5f, 0f), Vector3.left);
            Physics.SyncTransforms();
            for (int step = 0; step < 5 && prey.IsAlive; step++) yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(prey.IsAlive, Is.False, "The shipped solid colliders must produce OnCollisionEnter.");
            Assert.That(predator.IsAlive, Is.True);
            Assert.That(population.DeadPrey, Is.EqualTo(1));
            Assert.That(population.Alive, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PreyContactAddsOppositePhysicalMotionWithoutDeaths()
        {
            AnimalActor left = factory.Spawn(frog, new Vector3(-0.2f, 0.5f, 0f), Vector3.right);
            AnimalActor right = factory.Spawn(frog, new Vector3(0.2f, 0.5f, 0f), Vector3.left);
            Physics.SyncTransforms();
            for (int step = 0; step < 3; step++) yield return new WaitForFixedUpdate();

            Assert.That(left.Body.linearVelocity.x, Is.LessThan(0f));
            Assert.That(right.Body.linearVelocity.x, Is.GreaterThan(0f));
            Assert.That(population.Alive, Is.EqualTo(2));
            Assert.That(population.DeadPrey, Is.Zero);
        }

        [Test]
        public void RemovedAnimalCannotBeReusedUntilReturnsAreFlushed()
        {
            AnimalActor original = Spawn(snake, -6f);
            uint originalGeneration = original.Generation;
            original.Body.linearVelocity = Vector3.right * 20f;
            Assert.That(population.TryRemove(original, false), Is.True);

            AnimalActor beforeFlush = Spawn(snake, 6f);
            Assert.That(beforeFlush, Is.Not.SameAs(original), "Contact callbacks must not see the victim as a new animal.");
            factory.FlushReturns();
            AnimalActor reused = factory.Spawn(snake, new Vector3(1f, 0.5f, 2f), Vector3.forward);

            Assert.That(reused, Is.SameAs(original));
            Assert.That(reused.Generation, Is.EqualTo(originalGeneration + 1));
            Assert.That(reused.Body.linearVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(reused.Body.angularVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(reused.Body.position, Is.EqualTo(new Vector3(1f, 0.5f, 2f)));
            Assert.That(reused.Direction, Is.EqualTo(Vector3.forward));
            Assert.That(reused.Body.detectCollisions, Is.True);
            Assert.That(population.Alive, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator TastyLabelDoesNotFollowANewGenerationOfItsTarget()
        {
            AnimalActor target = Spawn(snake, 0f);
            var cameraObject = new GameObject("Popup camera", typeof(Camera));
            cameraObject.transform.SetParent(root.transform);
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 20f, 0f), Quaternion.Euler(90f, 0f, 0f));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            var canvasObject = new GameObject("Popup canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(root.transform);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var container = (RectTransform)canvasObject.transform;
            TastyView prefab = AssetDatabase.LoadAssetAtPath<TastyView>("Assets/ZooWorld/Prefabs/TastyView.prefab");
            TastyView popup = Object.Instantiate(prefab, container);
            int returns = 0;
            popup.Show(target, camera, container, _ => returns++);
            Assert.That(popup.IsShowing, Is.True);

            population.TryRemove(target, false);
            factory.FlushReturns();
            Assert.That(Spawn(snake, 2f), Is.SameAs(target));
            yield return null;
            yield return null;

            Assert.That(popup.IsShowing, Is.False);
            Assert.That(returns, Is.EqualTo(1));
            popup.Hide();
            Assert.That(returns, Is.EqualTo(1), "Repeated hide must not double-release a pooled label.");
        }

        [UnityTest]
        public IEnumerator DisposeDestroysRentedAndReturnedAnimalsAndIsIdempotent()
        {
            AnimalActor live = Spawn(snake, -4f);
            AnimalActor returned = Spawn(frog, 4f);
            population.TryRemove(returned, true);
            factory.Dispose();
            factory.Dispose();
            yield return null;

            Assert.That(population.Alive, Is.Zero);
            Assert.That(population.DeadPrey, Is.Zero);
            Assert.That(live == null, Is.True);
            Assert.That(returned == null, Is.True);
            Assert.That(root.GetComponentsInChildren<AnimalActor>(true), Is.Empty);
            Assert.Throws<ObjectDisposedException>(() => Spawn(snake, 0f));
        }

        [UnityTest]
        public IEnumerator ThrowingPopulationObserverCannotPreventFactoryCleanup()
        {
            AnimalActor live = Spawn(snake, 0f);
            Action brokenObserver = () => throw new InvalidOperationException("Test observer failed.");
            population.Changed += brokenObserver;
            try
            {
                Assert.Throws<InvalidOperationException>(() => factory.Dispose());
            }
            finally
            {
                population.Changed -= brokenObserver;
            }
            yield return null;

            Assert.That(live == null, Is.True);
            Assert.That(population.Alive, Is.Zero);
            Assert.That(root.GetComponentsInChildren<AnimalActor>(true), Is.Empty);
            Assert.DoesNotThrow(() => factory.Dispose());
        }

        [Test]
        public void CustomMovementEnablesOnlyAfterInitializationAndPhysicsReset()
        {
            AnimalDefinition custom = CreateProbeSpecies();
            AnimalActor animal = Spawn(custom, 0f);
            var probe = animal.GetComponent<ActivationProbeMovement>();
            Assert.That(probe.EnableCount, Is.EqualTo(1));
            Assert.That(probe.SawFullyInitializedActor, Is.True);

            animal.Body.linearVelocity = Vector3.right * 20f;
            population.TryRemove(animal, false);
            factory.FlushReturns();
            Assert.That(Spawn(custom, 2f), Is.SameAs(animal));
            Assert.That(probe.EnableCount, Is.EqualTo(2));
            Assert.That(probe.SawFullyInitializedActor, Is.True);
        }

        [Test]
        public void FailedCustomMovementInitializationRollsBackAndCanBeRetried()
        {
            AnimalDefinition custom = CreateProbeSpecies();
            ActivationProbeMovement.ThrowOnNextInitialize = true;
            Assert.Throws<InvalidOperationException>(() => Spawn(custom, 0f));
            Assert.That(population.Alive, Is.Zero);
            AnimalActor[] failedInstances = root.GetComponentsInChildren<AnimalActor>(true);
            Assert.That(failedInstances.Length, Is.EqualTo(2), "One template and one failed rental.");
            AnimalActor failed = failedInstances[0] == custom.Prefab ? failedInstances[1] : failedInstances[0];
            Assert.That(failed.IsAlive, Is.False);
            Assert.That(failed.gameObject.activeSelf, Is.False);
            Assert.That(failed.GetComponent<ActivationProbeMovement>().EnableCount, Is.Zero);

            factory.FlushReturns();
            AnimalActor retried = Spawn(custom, 2f);
            Assert.That(retried, Is.SameAs(failed), "The failed rental must be returned, not leaked.");
            Assert.That(population.Alive, Is.EqualTo(1));
            Assert.That(retried.GetComponent<ActivationProbeMovement>().SawFullyInitializedActor, Is.True);
            Assert.That(population.DeadPrey + population.DeadPredators, Is.Zero);
        }

        private AnimalDefinition CreateProbeSpecies()
        {
            var templates = new GameObject("Inactive test templates");
            templates.SetActive(false);
            templates.transform.SetParent(root.transform);
            AnimalActor template = Object.Instantiate(snake.Prefab, templates.transform);
            Object.DestroyImmediate(template.Movement);
            var movement = template.gameObject.AddComponent<ActivationProbeMovement>();
            var actorData = new SerializedObject(template);
            actorData.FindProperty("movement").objectReferenceValue = movement;
            actorData.ApplyModifiedPropertiesWithoutUndo();
            transientDefinition = Object.Instantiate(snake);
            var definitionData = new SerializedObject(transientDefinition);
            definitionData.FindProperty("prefab").objectReferenceValue = template;
            definitionData.ApplyModifiedPropertiesWithoutUndo();
            return transientDefinition;
        }

        private AnimalActor Spawn(AnimalDefinition definition, float x) =>
            factory.Spawn(definition, new Vector3(x, 0.5f, 0f), Vector3.forward);

        private sealed class OpenBounds : IWorldBounds
        {
            public void Refresh() { }
            public Vector3 RandomPosition(float margin = 1.2f) => new Vector3(0f, 0.5f, 0f);
            public void Constrain(ref Vector3 position, ref Vector3 direction, float radius) { }
        }
    }
}
#endif
