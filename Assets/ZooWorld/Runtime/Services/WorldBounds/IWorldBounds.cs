using UnityEngine;

namespace ZooWorld
{
    public interface IWorldBounds
    {
        void Refresh();
        Vector3 RandomPosition(float margin = 1.2f);
        void Constrain(ref Vector3 position, ref Vector3 direction, float radius);
    }
}
