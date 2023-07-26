using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections; 
using minescape.ESC.components;

namespace minescape.ESC.systems
{
    public partial class CreateChunkSystem : SystemBase
    {
        bool isCreated;
        BeginInitializationEntityCommandBufferSystem cbs;
        EntityQuery Query;

        protected override void OnCreate()
        {
            cbs = World.GetOrCreateSystemManaged<BeginInitializationEntityCommandBufferSystem>();
            Query = GetEntityQuery(ComponentType.ReadWrite<Chunk>());
        }

        protected override void OnUpdate()
        {
            if (!isCreated)
            {
                for (int x = 0; x < 2; x++)
                    for (int z = 0; z < 2; z++)
                        CreateChunk(x, z);
            }
        }


        protected override void OnDestroy()
        {
            NativeArray<Entity> entities = Query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var chunk = World.EntityManager.GetComponentData<Chunk>(entity);
                chunk.BlockMap.Dispose();
            }
        }

        void CreateChunk(int x, int z)
        {
            var cb = cbs.CreateCommandBuffer();
            Entity entity = cb.CreateEntity();
            ChunkCoord coord = new(x, z);
            Chunk chunk = new(coord);
            cb.SetName(entity, chunk.ToString());
            var transform = new LocalTransform()
            {
                Position = new float3(coord.x * Constants.ChunkWidth, 0f, coord.z * Constants.ChunkWidth)
            };
            cb.AddComponent(entity, chunk);
            cb.AddComponent(entity, transform);
            cb.AddComponent<NeedsBlockMap>(entity);

            isCreated = true;
        }
    }
}