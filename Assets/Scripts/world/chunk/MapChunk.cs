using Unity.Collections;
using Unity.Mathematics;
using minescape.init;
using minescape.block;

namespace minescape.world.chunk
{
    public struct MapChunk
    {
        public ChunkCoord coord;
        public NativeArray<byte> BlockMap;
        public int2 position;
        //byte[,] Biomes = new byte[Constants.ChunkWidth, Constants.ChunkWidth];


        public MapChunk(ChunkCoord _coord)
        {
            coord = _coord;
            position = new int2(coord.x * Constants.MapChunkWidth, coord.z * Constants.MapChunkWidth);
            BlockMap = new(262144, Allocator.Persistent);
        }

        public static int ConvertToIndex(int x, int z)
        {
            return x + z * Constants.MapChunkWidth;
        }

        /// <summary>
        /// Sets block ID in chunk.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <param name="block"></param>
        public void SetBlock(int x, int z, byte block)
        {
            int index = ConvertToIndex(x, z);
            BlockMap[index] = block;
        }

        /// <summary>
        /// Gets Block at coordinates in Chunk.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns>Block object at coordinates</returns>
        public Block GetBlock(int x, int z)
        {
            int index = ConvertToIndex(x, z);
            return Blocks.blocks[BlockMap[index]];
        }

        public void Dispose()
        {
            BlockMap.Dispose();
        }
    }
}