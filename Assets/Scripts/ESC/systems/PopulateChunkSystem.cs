using Unity.Entities;
using Unity.Mathematics;
using minescape.init;
using minescape.ESC.components;
using Unity.Jobs;

namespace minescape.ESC.systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct PopulateChunkSystem : ISystem
    {
        EntityQuery PopulateChunkQuery;

        public void OnCreate(ref SystemState state)
        {
            PopulateChunkQuery = state.GetEntityQuery(ComponentType.ReadOnly<NeedsBlockMap>());
            state.RequireForUpdate(PopulateChunkQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            var cbs = state.World.GetOrCreateSystemManaged<BeginInitializationEntityCommandBufferSystem>();
            var cb = cbs.CreateCommandBuffer();

            foreach (var (_, _chunk, entity) in SystemAPI.Query<RefRO<NeedsBlockMap>, RefRW<Chunk>>().WithEntityAccess())
            {
                new PopulateChunkJob() { chunk = _chunk.ValueRW }.Schedule();
                cb.RemoveComponent<NeedsBlockMap>(entity);
                cb.AddComponent<NeedsMeshData>(entity);
            }
        }

        public void OnDestroy(ref SystemState state)
        {

        }
    }

    public partial struct PopulateChunkJob : IJobEntity
    {
        public Chunk chunk;

        public void Execute()
        {
            for (int x = 0; x < Constants.ChunkWidth; x++)
            {
                for (int z = 0; z < Constants.ChunkWidth; z++)
                {
                    var noise = Noise.Get2DPerlin(new float2(chunk.position.x + x, chunk.position.z + z), 0, 0.2f);
                    var terrainHeight = (int)math.floor(128 * noise + 32);
                    for (int y = 0; y < Constants.ChunkHeight; y++)
                    {
                        if (y == 0)
                            chunk.SetBlock(x, y, z, Blocks.BEDROCK.ID);
                        else if (y <= terrainHeight)
                            chunk.SetBlock(x, y, z, Blocks.STONE.ID);
                        else if (y > terrainHeight && y == Constants.WaterLevel)
                            chunk.SetBlock(x, y, z, Blocks.WATER.ID);
                        else if (y > terrainHeight && y > Constants.WaterLevel)
                            break;
                    }
                }
            }
        }
    }
}