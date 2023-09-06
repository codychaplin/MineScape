using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using minescape.block;
using minescape.world.chunk;

namespace minescape.jobs
{
    [BurstCompile]
    public struct RemoveLightJob : IJob
    {
        [ReadOnly] public int3 position;
        [ReadOnly] public NativeArray<byte> blockMap;
        public NativeArray<byte> lightMap;

        public NativeQueue<FloodFillNode> bfsRemovalQueue;

        public void Execute()
        {
            bfsRemovalQueue.Enqueue(new FloodFillNode(position, 0));
            while (bfsRemovalQueue.Count > 0)
            {
                var node = bfsRemovalQueue.Dequeue();

                for (int i = 0; i < 6; i++)
                {
                    // get neighbour position and check in in chunk
                    int3 neighbour = node.Pos + VoxelData.faceCheck[i];
                    if (!Chunk.IsBlockInChunk(neighbour.x, neighbour.y, neighbour.z))
                        continue;

                    // get neighbour info
                    int neighbourIndex = Chunk.ConvertToIndex(neighbour);
                    byte neighbourLightLevel = lightMap[neighbourIndex];

                    // if 0 or 15 and not checking down, skip
                    if (neighbourLightLevel == 0 || (neighbourLightLevel == 15 && i != 3))
                        continue;

                    // update neighbour light level and add to queue
                    lightMap[neighbourIndex] = 0;
                    bfsRemovalQueue.Enqueue(new FloodFillNode(neighbour, 0));
                }
            }

            bfsRemovalQueue.Clear();
        }
    }
}