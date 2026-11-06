#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;

namespace ZooWorld.Tests
{
    public class ZooSceneTests
    {
        private Scene loadedScene;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (loadedScene.IsValid() && loadedScene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(loadedScene);
        }

        [UnityTest]
        public IEnumerator ShippedSceneBootsSpawnsAndUpdatesItsUguiCounters()
        {
            yield return SceneManager.LoadSceneAsync("Assets/ZooWorld/Scenes/ZooWorld.unity", LoadSceneMode.Additive);
            loadedScene = SceneManager.GetSceneByPath("Assets/ZooWorld/Scenes/ZooWorld.unity");
            ZooBootstrap bootstrap = loadedScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ZooBootstrap>(true)).Single();
            Assert.That(bootstrap.Container, Is.Not.Null);
            IAnimalPopulation population = bootstrap.Container.Resolve<IAnimalPopulation>();
            ZooHud hud = loadedScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ZooHud>(true)).Single();
            Assert.DoesNotThrow(() => hud.ValidateConfiguration());

            yield return new WaitForSeconds(2.1f);
            yield return null;

            Assert.That(population.Alive, Is.GreaterThanOrEqualTo(1));
            string[] labels = hud.GetComponentsInChildren<TextMeshProUGUI>(true).Select(label => label.text).ToArray();
            Assert.That(labels, Does.Contain($"{population.Alive:000}  ANIMALS ALIVE"));
            Assert.That(labels, Does.Contain($"PREY LOST     {population.DeadPrey:000}"));
            Assert.That(labels, Does.Contain($"PREDATORS LOST     {population.DeadPredators:000}"));

            // Disabling/re-enabling the HUD must restore subscriptions without duplicating them.
            hud.gameObject.SetActive(false);
            hud.gameObject.SetActive(true);
            yield return null;
            Assert.That(hud.GetComponentsInChildren<TextMeshProUGUI>(true).Select(label => label.text),
                Does.Contain($"{population.Alive:000}  ANIMALS ALIVE"));
        }
    }
}
#endif
