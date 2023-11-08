using Unity.Collections;

namespace minescape.world.chunk
{
    public class ChunkSection
    {
        public NativeArray<byte> blocks;

        public ChunkSection()
        {
            blocks = new(4096, Allocator.Persistent);
        }
    }
}