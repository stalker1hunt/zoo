using UnityEngine;

namespace ZooWorld
{
    public class WorldBoundsService : IWorldBounds
    {
        private readonly Camera camera;
        private Vector2 center;
        private Vector2 halfSize;

        public WorldBoundsService(Camera worldCamera)
        {
            camera = worldCamera;
            Refresh();
        }

        public void Refresh()
        {
            halfSize = new Vector2(camera.orthographicSize * camera.aspect, camera.orthographicSize);
            center = new Vector2(camera.transform.position.x, camera.transform.position.z);
        }

        public Vector3 RandomPosition(float margin = 1.2f)
        {
            Vector2 extent = Vector2.Max(halfSize - Vector2.one * margin, Vector2.zero);
            return new Vector3(center.x + Random.Range(-extent.x, extent.x), 0.5f, center.y + Random.Range(-extent.y, extent.y));
        }

        public void Constrain(ref Vector3 position, ref Vector3 direction, float radius)
        {
            Vector2 extent = Vector2.Max(halfSize - Vector2.one * (radius + 0.1f), Vector2.zero);
            float left = center.x - extent.x;
            float right = center.x + extent.x;
            float bottom = center.y - extent.y;
            float top = center.y + extent.y;
            if (position.x < left) { position.x = left; direction.x = Mathf.Abs(direction.x); }
            if (position.x > right) { position.x = right; direction.x = -Mathf.Abs(direction.x); }
            if (position.z < bottom) { position.z = bottom; direction.z = Mathf.Abs(direction.z); }
            if (position.z > top) { position.z = top; direction.z = -Mathf.Abs(direction.z); }
        }
    }
}
