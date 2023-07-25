using minescape.ESC.components;
using Unity.Collections;
using Unity.Entities;

namespace minescape.ESC.entities
{
    public struct Chunk : IComponentData
    {
        NativeArray<byte> BlockMap; // get index: x + z * 16 + y * 256
        public ChunkCoord coord; // coordinates in chunk size

        public Chunk(ChunkCoord _coord)
        {
            // 65536 = 16x16x256 (x,z,y)
            BlockMap = new NativeArray<byte>(65536, Allocator.Persistent);
            coord = _coord;
        }
    }
}
