using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using minescape.init;
using minescape.structures;

namespace minescape.jobs
{
    [BurstCompile]
    public struct GenerateStructuresJob : IJob
    {
        [ReadOnly] public NativeList<Structure> structures;

        public NativeArray<byte> blockMap;
        public NativeArray<byte> northMap;
        public NativeArray<byte> northEastMap;
        public NativeArray<byte> eastMap;
        public NativeArray<byte> southEastMap;
        public NativeArray<byte> southMap;
        public NativeArray<byte> southWestMap;
        public NativeArray<byte> westMap;
        public NativeArray<byte> northWestMap;

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

        void TryPlaceBlock(int x, int y, int z, byte blockID)
        {
            if (Utils.IsBlockInChunk(x, y, z))
            {
                PlaceBlock(x, y, z, blockMap, blockID);
            }
            else
            {
                if (z > 15) // north
                {
                    if (x < 0) // northwest
                        PlaceBlock(x + 16, y, z - 16, northWestMap, blockID);
                    else if (x > 15) // northeast
                        PlaceBlock(x - 16, y, z - 16, northEastMap, blockID);
                    else // north
                        PlaceBlock(x, y, z - 16, northMap, blockID);
                }
                else if (z < 0) // south
                {
                    if (x < 0) // southwest
                        PlaceBlock(x + 16, y, z + 16, southWestMap, blockID);
                    else if (x > 15) // southeast
                        PlaceBlock(x - 16, y, z + 16, southEastMap, blockID);
                    else // south
                        PlaceBlock(x, y, z + 16, southMap, blockID);
                }
                else
                {
                    if (x > 15) // east
                        PlaceBlock(x - 16, y, z, eastMap, blockID);
                    else if (x < 0) // west
                        PlaceBlock(x + 16, y, z, westMap, blockID);
                }
            }
        }

        void PlaceBlock(int x, int y,int z, NativeArray<byte> map, byte blockID)
        {
            if (map.Length < 1)
                return;

            int index = Utils.ConvertToIndex(x, y, z);
            if (map[index] == BlockIDs.AIR)
                map[index]= blockID;
        }

        void GenerateTree(int x, int y, int z, Random rand, byte radius)
        {
            int height = rand.NextInt(4, 7);

            blockMap[Utils.ConvertToIndex(x, y - 1, z)] = BlockIDs.DIRT;
            for (int yy = y; yy < y + height; yy++)
                blockMap[Utils.ConvertToIndex(x, yy, z)] = BlockIDs.WOOD;

            bool flag = false;
            for (int yy = y + height - 2; yy <= y + height + 1; yy++)
            {
                for (int xx = x - radius; xx <= x + radius; xx++)
                    for (int zz = z - radius; zz <= z + radius; zz++)
                        TryPlaceBlock(xx, yy, zz, BlockIDs.LEAVES);

                if (flag)
                    radius -= 1;
                flag = !flag;
                if (radius < 0)
                    break;
            }
        }

        void GenerateCactus(int x, int y, int z, Random rand)
        {
            int height = rand.NextInt(2, 6);
            for (int yy = y; yy <= y + height; yy++)
            {
                if (blockMap[Utils.ConvertToIndex(x + 1, yy, z)] != BlockIDs.AIR ||
                    blockMap[Utils.ConvertToIndex(x - 1, yy, z)] != BlockIDs.AIR     ||
                    blockMap[Utils.ConvertToIndex(x, yy, z + 1)] != BlockIDs.AIR     ||
                    blockMap[Utils.ConvertToIndex(x, yy, z - 1)] != BlockIDs.AIR)
                    break;

                blockMap[Utils.ConvertToIndex(x, yy, z)] = BlockIDs.CACTUS;
            }
        }
    }

    // dummy job to deallocate temp arrays
    struct DeallocateFillerJob : IJob
    {
        [DeallocateOnJobCompletion] public NativeArray<byte> tempNorth;
        [DeallocateOnJobCompletion] public NativeArray<byte> tempNorthEast;
        [DeallocateOnJobCompletion] public NativeArray<byte> tempEast;
        [DeallocateOnJobCompletion] public NativeArray<byte> tempSouthEast;
        [DeallocateOnJobCompletion] public NativeArray<byte> tempSouth;
        [DeallocateOnJobCompletion] public NativeArray<byte> tempSouthWest;
        [DeallocateOnJobCompletion] public NativeArray<byte> tempWest;
        [DeallocateOnJobCompletion] public NativeArray<byte> tempNorthWest;

        public void Execute()
        {
            
        }
    }
}