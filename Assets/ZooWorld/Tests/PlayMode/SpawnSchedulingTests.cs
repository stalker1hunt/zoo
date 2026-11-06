#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ZooWorld.Tests
{
    public class SpawnSchedulingTests
    {
        private Random.State savedRandomState;
        private RecordingFactory factory;
        private AnimalSpawnService spawner;

        [SetUp]
        public void SetUp()
        {
            savedRandomState = Random.state;
            Random.InitState(2026);
            factory = new RecordingFactory();
            var database = AssetDatabase.LoadAssetAtPath<AnimalDatabase>("Assets/ZooWorld/AnimalDatabase.asset");
            spawner = new AnimalSpawnService(database, new FixedBounds(), factory);
        }

        [TearDown]
        public void TearDown()
        {
            spawner.Dispose();
            Random.state = savedRandomState;
        }

        [Test]
        public void EachAnimalSpawnsAfterOneAndByTwoSeconds()
        {
            for (int expected = 1; expected <= 8; expected++)
            {
                spawner.Tick(0.999f);
                Assert.That(factory.Spawns, Is.EqualTo(expected - 1), "No animal before the minimum interval.");
                spawner.Tick(1.002f);
                Assert.That(factory.Spawns, Is.EqualTo(expected), "An animal must appear by the maximum interval.");
                Assert.That(factory.LastDefinition, Is.Not.Null);
                Assert.That(factory.LastPosition, Is.EqualTo(new Vector3(3f, 0.5f, -2f)));
                Assert.That(factory.LastDirection.magnitude, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(factory.LastDirection.y, Is.Zero);
            }
        }

        [Test]
        public void ALongFrameProducesOneAnimalAndStartsANewInterval()
        {
            spawner.Tick(30f);
            Assert.That(factory.Spawns, Is.EqualTo(1));
            spawner.Tick(0f);
            spawner.Tick(0.999f);
            Assert.That(factory.Spawns, Is.EqualTo(1), "A stall must not produce a burst of overdue animals.");
            spawner.Tick(1.002f);
            Assert.That(factory.Spawns, Is.EqualTo(2));
        }

        [Test]
        public void DisposingSchedulerStopsSpawningWithoutDisposingSharedFactory()
        {
            spawner.Dispose();
            spawner.Dispose();
            spawner.Tick(100f);
            Assert.That(factory.Spawns, Is.Zero);
            Assert.That(factory.Disposed, Is.False);
        }

        private sealed class FixedBounds : IWorldBounds
        {
            public void Refresh() { }
            public Vector3 RandomPosition(float margin = 1.2f) => new Vector3(3f, 0.5f, -2f);
            public void Constrain(ref Vector3 position, ref Vector3 direction, float radius) { }
        }

        private sealed class RecordingFactory : IAnimalFactory
        {
            public int Spawns { get; private set; }
            public AnimalDefinition LastDefinition { get; private set; }
            public Vector3 LastPosition { get; private set; }
            public Vector3 LastDirection { get; private set; }
            public bool Disposed { get; private set; }

            public AnimalActor Spawn(AnimalDefinition definition, Vector3 position, Vector3 direction)
            {
                Spawns++;
                LastDefinition = definition;
                LastPosition = position;
                LastDirection = direction;
                return null;
            }

            public void FlushReturns() { }
            public void Dispose() => Disposed = true;
        }
    }
}
#endif
