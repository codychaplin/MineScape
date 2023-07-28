using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using minescape.block;

namespace minescape.ESC.components
{
    public struct Chunk : IComponentData
    {
        public NativeArray<byte> BlockMap; // get index: x + z * 16 + y * 256
        public ChunkCoord coord; // coordinates in chunk size
        public int3 position; // position in world

        public Chunk(ChunkCoord _coord)
        {
            coord = _coord;
            position = new(coord.x * Constants.ChunkWidth, 0, coord.z * Constants.ChunkWidth);
            BlockMap = new NativeArray<byte>(65536, Allocator.Persistent); // 65536 = 16x16x256 (x,z,y)
        }

        public static int ConvertToIndex(int3 pos)
        {
            return pos.x + pos.z * Constants.ChunkWidth + pos.y * Constants.ChunkHeight;
        }

        public void SetBlock(int x, int y, int z, byte block)
        {
            int index = x + z * Constants.ChunkWidth + y * Constants.ChunkHeight;
            BlockMap[index] = block;
        }

        public byte GetBlock(int x, int y, int z)
        {
            int index = x + z * Constants.ChunkWidth + y * Constants.ChunkHeight;
            return BlockMap[index];
        }

        public static bool IsBlockInChunk(int x, int y, int z)
        {
            if (x < 0 || x >= BlockData.ChunkWidth ||
                y < 0 || y >= BlockData.ChunkHeight ||
                z < 0 || z >= BlockData.ChunkWidth)
                return false;
            else
                return true;
        }

        public void Dispose()
        {
            BlockMap.Dispose();
        }

        public override string ToString()
        {
            return $"chunk({coord.x},{coord.z})";
        }
    }
}
