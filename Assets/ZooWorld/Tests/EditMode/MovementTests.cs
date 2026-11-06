using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    public class MovementTests
    {
        private AnimalTestData data;

        [SetUp] public void SetUp() => data = new AnimalTestData();
        [TearDown] public void TearDown() => data.Dispose();

        [TestCase(0.02f)]
        [TestCase(0.07f)]
        [TestCase(0.5f)]
        public void HopTravelsConfiguredDistancePerCycleAcrossDifferentStepSizes(float step)
        {
            var actor = data.Own(Object.Instantiate(AnimalTestData.Prefab("Frog").gameObject));
            var movement = actor.GetComponent<HopMovement>();
            var settings = new HopMovementSettings();
            movement.Initialize(settings);
            movement.ResetState();
            Vector3 direction = Vector3.forward;
            const int cycles = 10;
            float remaining = cycles * (settings.Duration + settings.RestDuration);
            float travelled = 0f;
            while (remaining > 0.00001f)
            {
                float delta = Mathf.Min(step, remaining);
                travelled += movement.Tick(delta, ref direction) * delta;
                remaining -= delta;
            }
            Assert.That(travelled, Is.EqualTo(cycles * settings.Distance).Within(0.003f));
            Assert.That(direction, Is.EqualTo(Vector3.forward));
        }

        [Test]
        public void PoolResetRestartsHopAtTheBeginningOfItsCycle()
        {
            var actor = data.Own(Object.Instantiate(AnimalTestData.Prefab("Frog").gameObject));
            var movement = actor.GetComponent<HopMovement>();
            var settings = new HopMovementSettings();
            movement.Initialize(settings);
            movement.ResetState();
            Vector3 direction = Vector3.forward;
            float firstSpeed = movement.Tick(0.1f, ref direction);
            movement.Tick(settings.Duration, ref direction);
            Assert.That(movement.Tick(0.1f, ref direction), Is.Zero);

            movement.ResetState();

            Assert.That(movement.Tick(0.1f, ref direction), Is.EqualTo(firstSpeed).Within(0.0001f));
        }

        [Test]
        public void SnakeMaintainsConfiguredLinearSpeed()
        {
            var actor = data.Own(Object.Instantiate(AnimalTestData.Prefab("Snake").gameObject));
            var movement = actor.GetComponent<LinearMovement>();
            var settings = new LinearMovementSettings();
            movement.Initialize(settings);
            Vector3 direction = Vector3.right;
            Assert.That(movement.Tick(0.02f, ref direction), Is.EqualTo(settings.Speed));
            Assert.That(movement.Tick(0.5f, ref direction), Is.EqualTo(settings.Speed));
            Assert.That(direction, Is.EqualTo(Vector3.right));
        }

        [Test]
        public void SpeciesRejectsSettingsForAnotherMovement()
        {
            AnimalDefinition species = data.Species("Invalid movement", AnimalRole.Prey);
            using (var serialized = new SerializedObject(species))
            {
                serialized.FindProperty("movementSettings").managedReferenceValue = new HopMovementSettings();
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            Assert.Throws<InvalidOperationException>(() => species.ValidateConfiguration());
        }

        [TestCase("linear", "speed", 0f)]
        [TestCase("linear", "speed", float.NaN)]
        [TestCase("hop", "duration", 0f)]
        [TestCase("hop", "duration", 1e-12f)]
        [TestCase("hop", "distance", float.PositiveInfinity)]
        [TestCase("hop", "restDuration", -1f)]
        [TestCase("wander", "turnInterval", 0f)]
        [TestCase("wander", "maximumTurnAngle", 181f)]
        public void InvalidMovementParametersFailBeforeSimulation(string kind, string field, float value)
        {
            AnimalMovementSettings settings = kind == "linear" ? new LinearMovementSettings()
                : kind == "hop" ? (AnimalMovementSettings)new HopMovementSettings() : new WanderMovementSettings();
            AnimalDefinition species = data.Species("Settings fixture", AnimalRole.Prey);
            using (var serialized = new SerializedObject(species))
            {
                serialized.FindProperty("movementSettings").managedReferenceValue = settings;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                serialized.Update();
                serialized.FindProperty("movementSettings").FindPropertyRelative(field).floatValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            Assert.Throws<InvalidOperationException>(() => species.MovementSettings.ValidateConfiguration());
        }
    }
}
