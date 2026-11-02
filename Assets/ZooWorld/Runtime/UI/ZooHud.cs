using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace ZooWorld
{
    public class ZooHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI preyCounter;
        [SerializeField] private TextMeshProUGUI predatorCounter;
        [SerializeField] private TextMeshProUGUI populationCounter;
        [SerializeField] private TastyView viewPrefab;
        [SerializeField] private RectTransform viewRoot;
        private IAnimalPopulation population;
        private IAnimalCollisions collisions;
        private Camera worldCamera;
        private ObjectPool<TastyView> viewPool;
        private readonly HashSet<TastyView> activeViews = new HashSet<TastyView>();
        private bool subscribed;
        private bool needsRefresh;

        public void ValidateConfiguration()
        {
            if (preyCounter == null || predatorCounter == null || populationCounter == null || viewPrefab == null || viewRoot == null)
                throw new InvalidOperationException("ZooHud needs its counters, popup prefab and popup container assigned.");
            viewPrefab.ValidateConfiguration();
        }

        public void Initialize(IAnimalPopulation populationService, IAnimalCollisions collisionService, Camera camera)
        {
            Shutdown();
            ValidateConfiguration();
            population = populationService;
            collisions = collisionService;
            worldCamera = camera;
            viewPool = new ObjectPool<TastyView>(() =>
            {
                TastyView popup = Instantiate(viewPrefab, viewRoot);
                popup.gameObject.SetActive(false);
                return popup;
            }, actionOnDestroy: popup => { if (popup != null) Destroy(popup.gameObject); },
                collectionCheck: true, defaultCapacity: 8, maxSize: 256);
            if (isActiveAndEnabled) Subscribe();
        }

        private void OnEnable() => Subscribe();
        private void OnDisable()
        {
            Unsubscribe();
            HideViews();
        }
        private void OnDestroy() => Shutdown();
        private void LateUpdate() { if (needsRefresh && population != null) Refresh(); }

        public void Shutdown()
        {
            Unsubscribe();
            HideViews();
            viewPool?.Dispose();
            viewPool = null;
            population = null;
            collisions = null;
            worldCamera = null;
            needsRefresh = false;
        }

        private void Subscribe()
        {
            if (subscribed || population == null) return;
            population.Changed += MarkDirty;
            collisions.AnimalEaten += ShowTasty;
            subscribed = true;
            Refresh();
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            population.Changed -= MarkDirty;
            collisions.AnimalEaten -= ShowTasty;
            subscribed = false;
        }

        private void MarkDirty() => needsRefresh = true;

        private void Refresh()
        {
            needsRefresh = false;
            preyCounter.text = $"PREY LOST     {population.DeadPrey:000}";
            predatorCounter.text = $"PREDATORS LOST     {population.DeadPredators:000}";
            populationCounter.text = $"{population.Alive:000}  ANIMALS ALIVE";
        }

        private void ShowTasty(AnimalActor eater)
        {
            TastyView popup = viewPool.Get();
            activeViews.Add(popup);
            popup.Show(eater, worldCamera, viewRoot, ReleaseView);
        }

        private void ReleaseView(TastyView view)
        {
            if (activeViews.Remove(view)) viewPool.Release(view);
        }

        private void HideViews()
        {
            var remaining = new TastyView[activeViews.Count];
            activeViews.CopyTo(remaining);
            foreach (TastyView view in remaining)
            {
                if (view == null)
                {
                    activeViews.Remove(view);
                    continue;
                }
                try { view.Hide(); }
                catch (Exception error) { Debug.LogException(error, view); }
                finally { ReleaseView(view); }
            }
        }
    }
}
