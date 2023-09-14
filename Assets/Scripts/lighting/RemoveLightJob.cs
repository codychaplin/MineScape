using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using minescape.block;
using minescape.world.chunk;

namespace minescape.lighting
{
    [BurstCompile]
    public struct RemoveLightJob : IJob
    {
        [ReadOnly] public int3 position;
        [ReadOnly] public NativeArray<byte> blockMap;

        public NativeArray<byte> lightMap;
        public NativeArray<byte> northMap;
        public NativeArray<byte> northEastMap;
        public NativeArray<byte> eastMap;
        public NativeArray<byte> southEastMap;
        public NativeArray<byte> southMap;
        public NativeArray<byte> southWestMap;
        public NativeArray<byte> westMap;
        public NativeArray<byte> northWestMap;

        [WriteOnly] public NativeReference<bool> northIsDirty;
        [WriteOnly] public NativeReference<bool> northEastIsDirty;
        [WriteOnly] public NativeReference<bool> eastIsDirty;
        [WriteOnly] public NativeReference<bool> southEastIsDirty;
        [WriteOnly] public NativeReference<bool> southIsDirty;
        [WriteOnly] public NativeReference<bool> southWestIsDirty;
        [WriteOnly] public NativeReference<bool> westIsDirty;
        [WriteOnly] public NativeReference<bool> northWestIsDirty;

        public NativeQueue<FloodFillNode> bfsRemovalQueue;
        public NativeQueue<FloodFillNode> bfsQueue;

        public void Execute()
        {
            // add initial node
            int index = Utils.ConvertToIndex(position);
            byte lightLevel = lightMap[index];
            bfsRemovalQueue.Enqueue(new FloodFillNode(position, lightLevel));
            lightMap[index] = 0;

            while (bfsRemovalQueue.Count > 0)
            {
                var node = bfsRemovalQueue.Dequeue();

                for (int i = 0; i < 6; i++)
                {
                    // get neighbour position and check in in chunk
                    int3 neighbourPosition = node.Pos + VoxelData.faceCheck[i];

                    // get neighbour info
                    if (!TryGetNeighbourLightMap(neighbourPosition, out int neighbourIndex, out var neighbourLightMap))
                        continue;
                    byte neighbourLightLevel = neighbourLightMap[neighbourIndex];

                    // if neighbour is not 15, add to removal queue
                    if (neighbourLightLevel > 0 && neighbourLightLevel < 15 || (node.LightLevel == 15 && i == 3))
                    {
                        neighbourLightMap[neighbourIndex] = 0;
                        bfsRemovalQueue.Enqueue(new FloodFillNode(neighbourPosition, neighbourLightLevel));
                    }
                    else if (neighbourLightLevel == 15) // if neighbour is 15, add to queue
                    {
                        bfsQueue.Enqueue(new FloodFillNode(neighbourPosition, neighbourLightLevel));
                    }
                }
            }

            bfsRemovalQueue.Clear();
        }

        bool TryGetNeighbourLightMap(int3 pos, out int neighbourIndex, out NativeArray<byte> neighbourLightmap)
        {
            neighbourIndex = 0;
            neighbourLightmap = default;

            if (Utils.IsBlockInChunk(pos.x, pos.y, pos.z)) // in chunk
            {
                neighbourIndex = Utils.ConvertToIndex(pos);
                neighbourLightmap = lightMap;
                return true;
            }

            if (pos.y >= Constants.ChunkHeight || pos.y < 0) // above/below chunk
                return false;

            if (pos.z >= Constants.ChunkWidth)
            {
                if (pos.x < 0) // northwest
                {
                    neighbourIndex = Utils.ConvertToIndex(pos.x + 16, pos.y, pos.z - 16);
                    neighbourLightmap = northWestMap;
                    northWestIsDirty.Value = true;
                    return true;
                }
                if (pos.x >= Constants.ChunkWidth) // northeast
                {
                    neighbourIndex = Utils.ConvertToIndex(pos.x - 16, pos.y, pos.z - 16);
                    neighbourLightmap = northEastMap;
                    northEastIsDirty.Value = true;
                    return true;
                }
                else // north
                {
                    neighbourIndex = Utils.ConvertToIndex(pos.x, pos.y, pos.z - 16);
                    neighbourLightmap = northMap;
                    northIsDirty.Value = true;
                    return true;
                }
            }
            else if (pos.z < 0)
            {
                if (pos.x < 0) // southwest
                {
                    neighbourIndex = Utils.ConvertToIndex(pos.x + 16, pos.y, pos.z + 16);
                    neighbourLightmap = southWestMap;
                    southWestIsDirty.Value = true;
                    return true;
                }
                if (pos.x >= Constants.ChunkWidth) // southeast
                {
                    neighbourIndex = Utils.ConvertToIndex(pos.x - 16, pos.y, pos.z + 16);
                    neighbourLightmap = southEastMap;
                    southEastIsDirty.Value = true;
                    return true;
                }
                else // south
                {
                    neighbourIndex = Utils.ConvertToIndex(pos.x, pos.y, pos.z + 16);
                    neighbourLightmap = southMap;
                    southIsDirty.Value = true;
                    return true;
                }
            }
            else
            {
                if (pos.x < 0) // west
                {
                    neighbourIndex = Utils.ConvertToIndex(pos.x + 16, pos.y, pos.z);
                    neighbourLightmap = westMap;
                    westIsDirty.Value = true;
                    return true;
                }
                if (pos.x >= Constants.ChunkWidth) // east
                {
                    neighbourIndex = Utils.ConvertToIndex(pos.x - 16, pos.y, pos.z);
                    neighbourLightmap = eastMap;
                    eastIsDirty.Value = true;
                    return true;
                }
                else
                    return false;
            }
        }
    }
}