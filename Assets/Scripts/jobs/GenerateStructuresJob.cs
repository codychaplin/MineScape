using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using minescape.init;
using minescape.structures;
using minescape.world.chunk;

namespace minescape.jobs
{
    public struct GenerateStructuresJob : IJob
    {
        [ReadOnly] public NativeList<Structure> structures;

        public NativeArray<byte> blockMap;

        public void Execute()
        {
            foreach (Structure structure in structures)
            {
                int x = structure.LocalPosition.x;
                int y = structure.LocalPosition.y;
                int z = structure.LocalPosition.z;
                var rand = new Random((uint)(x * 79 + y * 557 + z * 991));
                byte radius = structure.Radius;

                switch (structure.Type)
                {
                    case Type.Tree:
                        GenerateTree(x, y, z, rand, radius);
                        break;
                    case Type.Cactus:
                        GenerateCactus(x, y, z, rand);
                        break;
                    case Type.Building:
                        break;
                    default:
                        break;
                }       
            }
        }

        void GenerateTree(int x, int y, int z, Random rand, byte radius)
        {
            int index = 0;
            int height = rand.NextInt(4, 7);

            blockMap[Chunk.ConvertToIndex(x, y - 1, z)] = Blocks.DIRT.ID;
            for (int yy = y; yy < y + height; yy++)
                blockMap[Chunk.ConvertToIndex(x, yy, z)] = Blocks.WOOD.ID;

            for (int xx = x - radius; xx <= x + radius; xx++)
                for (int zz = z - radius; zz <= z + radius; zz++)
                    for (int yy = y + height - 2; yy <= y + height + 2; yy++)
                    {
                        if (Chunk.IsBlockInChunk(xx, yy, zz))
                        {
                            index = Chunk.ConvertToIndex(xx, yy, zz);
                            if (blockMap[index] == Blocks.AIR.ID)
                                blockMap[index] = Blocks.LEAVES.ID;
                        }
                    }
        }

        void GenerateCactus(int x, int y, int z, Random rand)
        {
            int height = rand.NextInt(2, 6);
            for (int yy = y; yy <= y + height; yy++)
            {
                if (blockMap[Chunk.ConvertToIndex(x + 1, yy, z)] != Blocks.AIR.ID ||
                    blockMap[Chunk.ConvertToIndex(x - 1, yy, z)] != Blocks.AIR.ID ||
                    blockMap[Chunk.ConvertToIndex(x, yy, z + 1)] != Blocks.AIR.ID ||
                    blockMap[Chunk.ConvertToIndex(x, yy, z - 1)] != Blocks.AIR.ID)
                    break;

                blockMap[Chunk.ConvertToIndex(x, yy, z)] = Blocks.CACTUS.ID;
            }
        }
    }
}