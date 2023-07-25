using minescape.ESC.components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace minescape.ESC
{
    public class ChunkManager : MonoBehaviour
    {
        EntityManager entityManager = new();

        private void Start()
        {
            Entity entity = entityManager.CreateEntity();
            ChunkCoord coord = new(0, 0);
            entityManager.SetName(entity, coord.ToString());
            var transform = new LocalTransform()
            {
                Position = new float3(coord.x * Constants.ChunkWidth, 0f, coord.z * Constants.ChunkWidth),
                Scale = 1f,
                Rotation = Quaternion.Euler(0, 0, 0)
            };
            entityManager.AddComponentData(entity, transform);
        }
    }
}