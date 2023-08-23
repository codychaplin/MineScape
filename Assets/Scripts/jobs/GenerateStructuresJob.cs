using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using minescape.structure;
using minescape.world.chunk;
using minescape.init;

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
                blockMap[Chunk.ConvertToIndex(x, y, z)] = Blocks.WOOD.ID;
                blockMap[Chunk.ConvertToIndex(x, y + 1, z)] = Blocks.WOOD.ID;
                blockMap[Chunk.ConvertToIndex(x, y + 2, z)] = Blocks.WOOD.ID;
                blockMap[Chunk.ConvertToIndex(x, y + 3, z)] = Blocks.WOOD.ID;
                blockMap[Chunk.ConvertToIndex(x, y + 4, z)] = Blocks.WOOD.ID;

                int index = 0;
                for (int i = x - structure.Radius; i <= x + structure.Radius; i++)
                    for (int j = z - structure.Radius; j <= z + structure.Radius; j++)
                        for (int k = y + 2; k <= y + 5; k++)
                        {
                            if (Chunk.IsBlockInChunk(i, k, j))
                            {
                                index = Chunk.ConvertToIndex(i, k, j);
                                if (blockMap[index] == Blocks.AIR.ID)
                                    blockMap[index] = Blocks.LEAVES.ID;
                            }
                        }
                        
            }
        }
    }
}