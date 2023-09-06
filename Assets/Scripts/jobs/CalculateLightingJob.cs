using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using minescape.init;
using minescape.block;
using minescape.world.chunk;

namespace minescape.jobs
{
    [BurstCompile]
    public struct CalculateLightingJob : IJob
    {
        [ReadOnly] public NativeArray<byte> blockMap;
        public NativeArray<byte> lightMap;

        public NativeQueue<FloodFillNode> bfsQueue;

        public void Execute()
        {
            for (int x = 0; x < Constants.ChunkWidth; x++)
                for (int z = 0; z < Constants.ChunkWidth; z++)
                    for (int y = Constants.ChunkHeight - 1; y >= 0; y--)
                    {
                        int index = Chunk.ConvertToIndex(x, y, z);
                        if (blockMap[index] == BlockIDs.AIR || blockMap[index] == BlockIDs.WATER)
                        {
                            lightMap[index] = 15;
                            bfsQueue.Enqueue(new FloodFillNode(new int3(x, y, z), 15));
                        }
                        else
                            break;
                    }

            while (bfsQueue.Count > 0)
            {
                var node = bfsQueue.Dequeue();

                for (int i = 0; i < 6; i++)
                {
                    // get neighbour position and check in in chunk
                    int3 neighbour = node.Pos + VoxelData.faceCheck[i];
                    if (!Chunk.IsBlockInChunk(neighbour.x, neighbour.y, neighbour.z))
                        continue;

                    // get neighbour info
                    int neighbourIndex = Chunk.ConvertToIndex(neighbour);
                    int neighbourBlockID = blockMap[neighbourIndex];
                    byte neighbourLightLevel = lightMap[neighbourIndex];

                    // if not transparent or neighbour light level is higher than light decay
                    bool isTransparent = neighbourBlockID == BlockIDs.AIR || neighbourBlockID == BlockIDs.WATER;
                    bool neighbourLightLevelIsHigher = neighbourLightLevel >= node.LightLevel - 1;
                    if (!isTransparent || neighbourLightLevelIsHigher)
                        continue;

                    // if propagating downwards, light doesn't decay
                    byte newNeighbourLightLevel = (i != 3) ? (byte)(node.LightLevel - 1) : node.LightLevel;

                    // update neighbour light level and add to queue
                    lightMap[neighbourIndex] = newNeighbourLightLevel;
                    bfsQueue.Enqueue(new FloodFillNode(neighbour, newNeighbourLightLevel));
                }
            }

            bfsQueue.Clear();
        }
    }
}