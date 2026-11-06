using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZooWorld.Editor;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    public class AuthoredAssetsTests
    {
        [TestCase("Frog")]
        [TestCase("Snake")]
        [TestCase("Beetle")]
        public void ShippedSpeciesHasValidMovementAndPhysicsPrefab(string name)
        {
            var species = AssetDatabase.LoadAssetAtPath<AnimalDefinition>($"Assets/ZooWorld/Species/{name}.asset");
            Assert.That(species, Is.Not.Null);
            Assert.DoesNotThrow(() => species.ValidateConfiguration());
            Assert.DoesNotThrow(() => AnimalPrefabValidation.Validate(species.Prefab));
        }

        [Test]
        public void ShippedDatabaseContainsRequiredPreyAndPredator()
        {
            var database = AssetDatabase.LoadAssetAtPath<AnimalDatabase>("Assets/ZooWorld/AnimalDatabase.asset");
            Assert.That(database, Is.Not.Null);
            Assert.DoesNotThrow(() => database.ValidateConfiguration());
            var frog = AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/ZooWorld/Species/Frog.asset");
            var snake = AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/ZooWorld/Species/Snake.asset");
            Assert.That(database.Definitions, Does.Contain(frog));
            Assert.That(database.Definitions, Does.Contain(snake));
            Assert.That(frog.Role, Is.EqualTo(AnimalRole.Prey));
            Assert.That(snake.Role, Is.EqualTo(AnimalRole.Predator));
            Assert.That(database.GetInteraction(snake, frog), Is.EqualTo(AnimalInteraction.FirstEatsSecond));
        }

        [Test]
        public void TriggerColliderIsRejectedBecauseFoodChainRequiresPhysicalContacts()
        {
            GameObject instance = Object.Instantiate(AnimalTestData.Prefab("Snake").gameObject);
            try
            {
                instance.GetComponent<SphereCollider>().isTrigger = true;
                Assert.Throws<InvalidOperationException>(() => instance.GetComponent<AnimalActor>().ValidateConfiguration());
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}
