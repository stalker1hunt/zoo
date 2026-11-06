using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    public class WorldBoundsTests
    {
        private GameObject cameraObject;
        private Camera camera;
        private WorldBoundsService bounds;
        private Random.State previousRandomState;

        [SetUp]
        public void SetUp()
        {
            previousRandomState = Random.state;
            Random.InitState(2026);
            cameraObject = new GameObject("Bounds test camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.aspect = 2f;
            camera.transform.SetPositionAndRotation(new Vector3(3f, 30f, -7f), Quaternion.Euler(90f, 0f, 0f));
            bounds = new WorldBoundsService(camera);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(cameraObject);
            Random.state = previousRandomState;
        }

        [Test]
        public void SpawnPositionsKeepRequestedMarginInsideActualCameraView()
        {
            for (int i = 0; i < 100; i++)
                AssertInsideView(bounds.RandomPosition(1.2f), 1.2f);
        }

        [TestCase(-20f, -7f, -1f, 0f)]
        [TestCase(20f, -7f, 1f, 0f)]
        [TestCase(3f, -20f, 0f, -1f)]
        [TestCase(3f, 20f, 0f, 1f)]
        public void EachEdgeReturnsWholeAnimalInsideAndTurnsItInward(float x, float z, float dx, float dz)
        {
            var position = new Vector3(x, 0.5f, z);
            var direction = new Vector3(dx, 0f, dz);

            bounds.Constrain(ref position, ref direction, 0.6f);

            AssertInsideView(position, 0.6f);
            Vector3 towardCenter = new Vector3(camera.transform.position.x - position.x, 0f,
                camera.transform.position.z - position.z);
            Assert.That(Vector3.Dot(direction, towardCenter), Is.GreaterThan(0f));
            Assert.That(position.y, Is.EqualTo(0.5f));
        }

        [Test]
        public void RefreshTracksCameraPositionZoomAndAspect()
        {
            camera.transform.position = new Vector3(-40f, 30f, 80f);
            camera.orthographicSize = 3f;
            camera.aspect = 0.75f;

            bounds.Refresh();

            for (int i = 0; i < 20; i++)
                AssertInsideView(bounds.RandomPosition(0.5f), 0.5f);
            var position = new Vector3(100f, 0.5f, -100f);
            Vector3 direction = Vector3.back;
            bounds.Constrain(ref position, ref direction, 0.6f);
            AssertInsideView(position, 0.6f);
        }

        private void AssertInsideView(Vector3 center, float radius)
        {
            foreach (Vector3 offset in new[] { Vector3.left, Vector3.right, Vector3.forward, Vector3.back })
            {
                Vector3 viewport = camera.WorldToViewportPoint(center + offset * radius);
                Assert.That(viewport.x, Is.InRange(-0.0001f, 1.0001f));
                Assert.That(viewport.y, Is.InRange(-0.0001f, 1.0001f));
                Assert.That(viewport.z, Is.GreaterThan(0f));
            }
        }
    }
}
