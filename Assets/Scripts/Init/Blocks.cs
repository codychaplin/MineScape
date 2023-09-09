using Unity.Burst;
using Unity.Collections;
using minescape.block;

namespace minescape.init
{
    public class Blocks
    {
        public Block AIR = new(0, 0, 0, 0, 0, 0, 0, true, false);
        public Block BEDROCK = new(1, 0, 0, 0, 0, 0, 0 );
        public Block STONE = new(2, 1, 1, 1, 1, 1, 1 );
        public Block DIRT = new(3, 3, 3, 3, 3, 3, 3 );
        public Block SAND = new(4, 4, 4, 4, 4, 4, 4 );
        public Block WATER = new(5, 8, 8, 8, 8, 8, 8 , true);
        public Block GRASS_TUNDRA = new(6, 9, 9, 9, 9, 9, 9 );
        public Block GRASS_PLAINS = new(7, 10, 10, 10, 10, 10, 10 );   
        public Block GRASS_SAVANNA = new(8, 11, 11, 11, 11, 11, 11 );
        public Block GRASS_BOREAL_FOREST = new(9, 12, 12, 12, 12, 12, 12 );
        public Block GRASS_TAIGA = new(10, 13, 13, 13, 13, 13, 13);
        public Block GRASS_SHRUBLAND = new(11, 14, 14, 14, 14, 14, 14);
        public Block GRASS_TEMPERATE_FOREST = new(12, 15, 15, 15, 15, 15, 15);
        public Block GRASS_SWAMP = new(13, 16, 16, 16, 16, 16, 16);
        public Block GRASS_SEASONAL_FOREST = new(14, 17, 17, 17, 17, 17, 17);
        public Block GRASS_TROPICAL_FOREST = new(15, 18, 18, 18, 18, 18, 18);
        public Block SAND_DESERT = new(16, 19, 19, 19, 19, 19, 19);
        public Block GRAVEL = new(17, 20, 20, 20, 20, 20, 20);
        public Block WOOD = new(18, 6, 6, 6, 6, 6, 6);
        public Block LEAVES = new(19, 21, 21, 21, 21, 21, 21);
        public Block CACTUS = new(20, 22, 22, 22, 22, 22, 22);

        public NativeHashMap<byte, Block> blocks;

        public Blocks()
        {
            blocks = new(21, Allocator.Persistent)
            {
                { AIR.ID, AIR },
                { BEDROCK.ID, BEDROCK },
                { STONE.ID, STONE },
                { DIRT.ID, DIRT },
                { SAND.ID, SAND },
                { WATER.ID, WATER },
                { GRASS_TUNDRA.ID, GRASS_TUNDRA },
                { GRASS_PLAINS.ID, GRASS_PLAINS },
                { GRASS_SAVANNA.ID, GRASS_SAVANNA },
                { GRASS_BOREAL_FOREST.ID, GRASS_BOREAL_FOREST },
                { GRASS_SHRUBLAND.ID, GRASS_SHRUBLAND },
                { GRASS_TEMPERATE_FOREST.ID, GRASS_TEMPERATE_FOREST },
                { GRASS_SWAMP.ID, GRASS_SWAMP },
                { GRASS_SEASONAL_FOREST.ID, GRASS_SEASONAL_FOREST },
                { GRASS_TROPICAL_FOREST.ID, GRASS_TROPICAL_FOREST },
                { SAND_DESERT.ID, SAND_DESERT },
                { GRAVEL.ID, GRAVEL },
                { WOOD.ID, WOOD },
                { LEAVES.ID, LEAVES },
                { CACTUS.ID, CACTUS }
            };
        }
    }

    [BurstCompile]
    public struct BlockIDs
    {
        public const byte AIR = 0;
        public const byte BEDROCK = 1;
        public const byte STONE = 2;
        public const byte DIRT = 3;
        public const byte SAND = 4;
        public const byte WATER = 5;
        public const byte GRASS_TUNDRA = 6;
        public const byte GRASS_PLAINS = 7;
        public const byte GRASS_SAVANNA = 8;
        public const byte GRASS_BOREAL_FOREST = 9;
        public const byte GRASS_TAIGA = 10;
        public const byte GRASS_SHRUBLAND = 11;
        public const byte GRASS_TEMPERATE_FOREST = 12;
        public const byte GRASS_SWAMP = 13;
        public const byte GRASS_SEASONAL_FOREST = 14;
        public const byte GRASS_TROPICAL_FOREST = 15;
        public const byte SAND_DESERT = 16;
        public const byte GRAVEL = 17;
        public const byte WOOD = 18;
        public const byte LEAVES = 19;
        public const byte CACTUS = 20;
    }
}