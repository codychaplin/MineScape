using Unity.Burst;
using Unity.Collections;
using minescape.structures;

namespace minescape.init
{
    public class Structures
    {
        public Structure TREE = new(0, 2, Type.Tree);
        public Structure CACTUS = new(1, 0, Type.Cactus);

        public NativeHashMap<byte, Structure> structures;

        public Structures()
        {
            structures = new(2, Allocator.Persistent)
            {
                { TREE.ID, TREE },
                { CACTUS.ID, CACTUS }
            };
        }
    }

    [BurstCompile]
    public struct StructureIDs
    {
        public const byte TREE = 0;
        public const byte CACTUS = 1;
    }
}