using System;
using TMPro;
using UnityEngine;

namespace ZooWorld
{
    public class TastyView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private RectTransform rect;
        private AnimalActor target;
        private uint targetGeneration;
        private Camera worldCamera;
        private RectTransform parent;
        private Action<TastyView> returnToPool;
        private float remaining;
        public bool IsShowing { get; private set; }

        public void ValidateConfiguration()
        {
            if (label == null || rect == null || label.gameObject != gameObject || rect.gameObject != gameObject)
                throw new InvalidOperationException("TastyView needs its label and RectTransform assigned.");
        }

        public void Show(AnimalActor eater, Camera mainCamera, RectTransform container, Action<TastyView> release)
        {
            target = eater;
            targetGeneration = eater.Generation;
            worldCamera = mainCamera;
            parent = container;
            returnToPool = release;
            remaining = 1.1f;
            label.alpha = 1f;
            IsShowing = true;
            gameObject.SetActive(true);
            UpdatePosition();
        }

        public void Hide()
        {
            if (!IsShowing) return;
            IsShowing = false;
            target = null;
            worldCamera = null;
            parent = null;
            remaining = 0f;
            if (label != null) label.alpha = 1f;
            if (rect != null) rect.anchoredPosition = Vector2.zero;
            Action<TastyView> release = returnToPool;
            returnToPool = null;
            gameObject.SetActive(false);
            release(this);
        }

        private void LateUpdate()
        {
            if (!IsShowing) return;
            remaining -= Time.deltaTime;
            if (target == null || !target.IsAlive || target.Generation != targetGeneration || remaining <= 0f)
            {
                Hide();
                return;
            }
            label.alpha = Mathf.Clamp01(remaining * 3f);
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Vector2 screen = worldCamera.WorldToScreenPoint(target.transform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out Vector2 point);
            rect.anchoredPosition = point + Vector2.down * 44f;
        }
    }
}
