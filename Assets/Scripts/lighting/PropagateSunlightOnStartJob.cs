using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using minescape.init;
using minescape.block;
using minescape.world.chunk;

namespace minescape.lighting
{
    [BurstCompile]
    public struct PropagateSunlightOnStartJob : IJob
    {
        [ReadOnly] public ChunkCoord coord;

        [ReadOnly] public NativeArray<byte> blockMap;
        [ReadOnly] public NativeArray<byte> northBlockMap;
        [ReadOnly] public NativeArray<byte> northEastBlockMap;
        [ReadOnly] public NativeArray<byte> eastBlockMap;
        [ReadOnly] public NativeArray<byte> southEastBlockMap;
        [ReadOnly] public NativeArray<byte> southBlockMap;
        [ReadOnly] public NativeArray<byte> southWestBlockMap;
        [ReadOnly] public NativeArray<byte> westBlockMap;
        [ReadOnly] public NativeArray<byte> northWestBlockMap;

        public NativeArray<byte> lightMap;
        public NativeArray<byte> northLightMap;
        public NativeArray<byte> northEastLightMap;
        public NativeArray<byte> eastLightMap;
        public NativeArray<byte> southEastLightMap;
        public NativeArray<byte> southLightMap;
        public NativeArray<byte> southWestLightMap;
        public NativeArray<byte> westLightMap;
        public NativeArray<byte> northWestLightMap;

        public NativeQueue<FloodFillNode> bfsQueue;

        [ReadOnly] public bool onGenerate;

        public void Execute()
        {
            if (onGenerate)
            {
                for (int x = 0; x < Constants.ChunkWidth; x++)
                    for (int z = 0; z < Constants.ChunkWidth; z++)
                        for (int y = Constants.ChunkHeight - 1; y >= 0; y--)
                        {
                            int index = Chunk.ConvertToIndex(x, y, z);
                            if (lightMap[index] == 15)
                                bfsQueue.Enqueue(new FloodFillNode(new int3(x, y, z), 15));
                            else
                                break;
                        }
            }

            while (bfsQueue.Count > 0)
            {
                var node = bfsQueue.Dequeue();

                for (int i = 0; i < 6; i++)
                {
                    // get neighbour position
                    int3 neighbourPosition = node.Pos + VoxelData.faceCheck[i];

                    // get neighbour info
                    if (!TryGetNeighbourLightMap(neighbourPosition, out int neighbourIndex, out var neighbourBlockMap, out var neighbourLightMap))
                        continue;
                    int neighbourBlockID = neighbourBlockMap[neighbourIndex];
                    byte neighbourLightLevel = neighbourLightMap[neighbourIndex];

                    // if propagating downwards, light doesn't decay
                    byte propagatedLightLevel = (i != 3) ? (byte)math.max(0, node.LightLevel - 1) : node.LightLevel;

                    // if not transparent or neighbour light level is higher than light decay
                    bool isTransparent = neighbourBlockID == BlockIDs.AIR || neighbourBlockID == BlockIDs.WATER || neighbourBlockID == BlockIDs.GRASS_PLANT;
                    bool neighbourLightLevelIsHigher = neighbourLightLevel >= propagatedLightLevel;
                    if (!isTransparent || neighbourLightLevelIsHigher)
                        continue;


                    // update neighbour light level and add to queue
                    neighbourLightMap[neighbourIndex] = propagatedLightLevel;
                    bfsQueue.Enqueue(new FloodFillNode(neighbourPosition, propagatedLightLevel));
                }
            }

            bfsQueue.Clear();
        }

        bool TryGetNeighbourLightMap(int3 pos, out int neighbourIndex, out NativeArray<byte> neighbourBlockMap, out NativeArray<byte> neighbourLightmap)
        {
            neighbourIndex = 0;
            neighbourBlockMap = default;
            neighbourLightmap = default;

            if (Chunk.IsBlockInChunk(pos.x, pos.y, pos.z)) // in chunk
            {
                neighbourIndex = Chunk.ConvertToIndex(pos);
                neighbourBlockMap = blockMap;
                neighbourLightmap = lightMap;
                return true;
            }

            if (pos.y >= Constants.ChunkHeight || pos.y < 0) // above/below chunk
                return false;

            if (pos.z >= Constants.ChunkWidth)
            {
                if (pos.x < 0) // northwest
                {
                    neighbourIndex = Chunk.ConvertToIndex(pos.x + 16, pos.y, pos.z - 16);
                    neighbourBlockMap = northWestBlockMap;
                    neighbourLightmap = northWestLightMap;
                    return true;
                }
                if (pos.x >= Constants.ChunkWidth) // northeast
                {
                    neighbourIndex = Chunk.ConvertToIndex(pos.x - 16, pos.y, pos.z - 16);
                    neighbourBlockMap = northEastBlockMap;
                    neighbourLightmap = northEastLightMap;
                    return true;
                }
                else // north
                {
                    neighbourIndex = Chunk.ConvertToIndex(pos.x, pos.y, pos.z - 16);
                    neighbourBlockMap = northBlockMap;
                    neighbourLightmap = northLightMap;
                    return true;
                }
            }
            else if (pos.z < 0)
            {
                if (pos.x < 0) // southwest
                {
                    neighbourIndex = Chunk.ConvertToIndex(pos.x + 16, pos.y, pos.z + 16);
                    neighbourBlockMap = southWestBlockMap;
                    neighbourLightmap = southWestLightMap;
                    return true;
                }
                if (pos.x >= Constants.ChunkWidth) // southeast
                {
                    neighbourIndex = Chunk.ConvertToIndex(pos.x - 16, pos.y, pos.z + 16);
                    neighbourBlockMap = southEastBlockMap;
                    neighbourLightmap = southEastLightMap;
                    return true;
                }
                else // south
                {
                    neighbourIndex = Chunk.ConvertToIndex(pos.x, pos.y, pos.z + 16);
                    neighbourBlockMap = southBlockMap;
                    neighbourLightmap = southLightMap;
                    return true;
                }
            }
            else
            {
                if (pos.x < 0) // west
                {
                    neighbourIndex = Chunk.ConvertToIndex(pos.x + 16, pos.y, pos.z);
                    neighbourBlockMap = westBlockMap;
                    neighbourLightmap = westLightMap;
                    return true;
                }
                if (pos.x >= Constants.ChunkWidth) // east
                {
                    neighbourIndex = Chunk.ConvertToIndex(pos.x - 16, pos.y, pos.z);
                    neighbourBlockMap = eastBlockMap;
                    neighbourLightmap = eastLightMap;
                    return true;
                }
                else
                    return false;
            }
        }
    }
}