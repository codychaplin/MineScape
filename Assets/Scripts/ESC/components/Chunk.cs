using Unity.Entities;
using Unity.Collections;

namespace minescape.ESC.components
{
    public struct Chunk : IComponentData
    {
        public NativeArray<byte> BlockMap; // get index: x + z * 16 + y * 256
        public ChunkCoord coord; // coordinates in chunk size

        public Chunk(ChunkCoord _coord)
        {
            // 65536 = 16x16x256 (x,z,y)
            BlockMap = new NativeArray<byte>(65536, Allocator.Persistent);
            coord = _coord;
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

        public override string ToString()
        {
            return $"Chunk: {coord.x},{coord.z}";
        }
    }
}
