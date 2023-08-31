using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using minescape.init;
using minescape.world.chunk;

namespace minescape.jobs
{
    [BurstCompile]
    public struct CalculateLightingJob : IJob
    {
        [ReadOnly] public NativeArray<byte> blockMap;
        [ReadOnly] public NativeArray<byte> heightMap;
        [WriteOnly] public NativeArray<byte> lightMap;

        public void Execute()
        {
            for (int x = 0; x < Constants.ChunkWidth; x++)
                for (int z = 0; z < Constants.ChunkWidth; z++)
                {
                    byte terrainHeight = heightMap[Chunk.ConvertToIndex(x, z)];
                    for (int y = Constants.ChunkHeight - 1; y >= 0; y--)
                    {
                        if (y <= terrainHeight)
                            break;

                        int index = Chunk.ConvertToIndex(x, y, z);
                        if (blockMap[index] == BlockIDs.AIR || blockMap[index] == BlockIDs.WATER)
                            lightMap[index] = 15;
                        else
                            break;
                    }
                }
        }
    }
}