using UnityEngine;
using Unity.Entities;
using minescape.init;
using minescape.ESC.components;

namespace minescape.ESC.systems
{
    [UpdateAfter(typeof(CreateChunkSystem))]
    public partial struct PopulateChunkSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var cb = state.World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer();
            foreach (var (blockMap, _chunk, entity) in SystemAPI.Query<RefRO<NeedsBlockMap>, RefRW<Chunk>>().WithEntityAccess())
            {
                var job = new PopulateChunkJob()
                {
                    chunk = _chunk.ValueRW
                };
                job.Schedule();
                cb.RemoveComponent<NeedsBlockMap>(entity);
                cb.AddComponent<NeedsRendering>(entity);
            }
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
                    var noise = Noise.Get2DPerlin(new Vector2(chunk.coord.x + x, chunk.coord.z + z), 0, 0.2f);
                    var terrainHeight = Mathf.FloorToInt(128 * noise + 16);
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