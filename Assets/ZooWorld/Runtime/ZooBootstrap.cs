using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ZooWorld
{
    public class ZooBootstrap : LifetimeScope
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private AnimalDatabase database;
        [SerializeField] private Transform animalRoot;
        [SerializeField] private ZooHud hud;
        private IWorldBounds bounds;
        private IAnimalSpawner spawner;
        private IAnimalFactory factory;

        protected override void Configure(IContainerBuilder builder)
        {
            if (worldCamera == null || !worldCamera.orthographic || database == null || animalRoot == null || hud == null)
                throw new InvalidOperationException("ZooBootstrap needs an orthographic camera, database, animal root and HUD.");
            database.ValidateConfiguration();
            hud.ValidateConfiguration();

            builder.RegisterInstance<IAnimalDatabase>(database);
            builder.RegisterInstance(worldCamera);
            builder.Register<WorldBoundsService>(Lifetime.Singleton).As<IWorldBounds>();
            builder.Register<AnimalPopulationService>(Lifetime.Singleton).As<IAnimalLifecycle, IAnimalPopulation>();
            builder.Register<AnimalCollisionService>(Lifetime.Singleton)
                .As<IAnimalCollisions>().WithParameter("preyKnockbackSpeed", 5.5f);
            builder.Register<PooledAnimalFactory>(Lifetime.Singleton)
                .As<IAnimalFactory>().WithParameter("animalParent", animalRoot);
            builder.Register<AnimalSpawnService>(Lifetime.Singleton).As<IAnimalSpawner>();
            builder.RegisterBuildCallback(container =>
            {
                bounds = container.Resolve<IWorldBounds>();
                factory = container.Resolve<IAnimalFactory>();
                spawner = container.Resolve<IAnimalSpawner>();
                hud.Initialize(container.Resolve<IAnimalPopulation>(), container.Resolve<IAnimalCollisions>(), worldCamera);
            });
        }

        private void Update()
        {
            if (bounds == null) return;
            bounds.Refresh();
            factory.FlushReturns();
            spawner.Tick(Time.deltaTime);
        }

        protected override void OnDestroy()
        {
            try
            {
                if (hud != null) hud.Shutdown();
            }
            finally
            {
                base.OnDestroy();
            }
        }
    }
}
