using Unity.Burst;
using Unity.Collections;
using minescape.block;

namespace minescape.init
{
    public class Blocks
    {
        public Block AIR = new(0, 255, 255, 255, 255, 255, 255, true, false);
        public Block BEDROCK = new(1, 0, 0, 0, 0, 0, 0 );
        public Block STONE = new(2, 1, 1, 1, 1, 1, 1 );
        public Block COBBLESTONE = new(3, 2, 2, 2, 2, 2, 2 );
        public Block STONE_BRICKS = new(4, 3, 3, 3, 3, 3, 3);
        public Block GRASS = new(5, 4, 4, 4, 4, 4, 4);
        public Block DIRT = new(6, 5, 5, 5, 5, 5, 5 );
        public Block SAND = new(7, 6, 6, 6, 6, 6, 6 );
        public Block SAND_DESERT = new(8, 7, 7, 7, 7, 7, 7);
        public Block WOOD = new(9, 8, 8, 9, 9, 8, 8);
        public Block PLANKS = new(10, 10, 10, 10, 10, 10, 10);
        public Block BRICKS = new(11, 11, 11, 11, 11, 11, 11);
        public Block GRAVEL = new(12, 12, 12, 12, 12, 12, 12);
        public Block LEAVES = new(13, 13, 13, 13, 13, 13, 13);
        public Block CACTUS = new(14, 14, 14, 15, 15, 14, 14);
        public Block WATER = new(15, 16, 16, 16, 16, 16, 16, true);
        public Block GRASS_PLANT = new(16, 17, 17, 17, 17, 17, 17, true, true, true);
        /*public Block GRASS_TUNDRA = new(6, 9, 9, 9, 9, 9, 9 );
        public Block GRASS_PLAINS = new(7, 20, 20, 20, 20, 20, 20 );   
        public Block GRASS_SAVANNA = new(8, 22, 22, 22, 22, 22, 22 );
        public Block GRASS_BOREAL_FOREST = new(9, 22, 22, 22, 22, 22, 22 );
        public Block GRASS_TAIGA = new(20, 25, 25, 25, 25, 25, 25);
        public Block GRASS_SHRUBLAND = new(22, 23, 23, 23, 23, 23, 23);
        public Block GRASS_TEMPERATE_FOREST = new(22, 25, 25, 25, 25, 25, 25);
        public Block GRASS_SWAMP = new(25, 26, 26, 26, 26, 26, 26);
        public Block GRASS_SEASONAL_FOREST = new(23, 27, 27, 27, 27, 27, 27);
        public Block GRASS_TROPICAL_FOREST = new(25, 28, 28, 28, 28, 28, 28);*/

        public NativeHashMap<byte, Block> blocks;

        public Blocks()
        {
            blocks = new(17, Allocator.Persistent)
            {
                { AIR.ID, AIR },
                { BEDROCK.ID, BEDROCK },
                { STONE.ID, STONE },
                { COBBLESTONE.ID, COBBLESTONE },
                { STONE_BRICKS.ID, STONE_BRICKS },
                { GRASS.ID, GRASS },
                { DIRT.ID, DIRT },
                { SAND.ID, SAND },
                { SAND_DESERT.ID, SAND_DESERT },
                { WOOD.ID, WOOD },
                { PLANKS.ID, PLANKS },
                { BRICKS.ID, BRICKS },
                { GRAVEL.ID, GRAVEL },
                { LEAVES.ID, LEAVES },
                { CACTUS.ID, CACTUS },
                { WATER.ID, WATER },
                { GRASS_PLANT.ID, GRASS_PLANT }
                /*{ GRASS_TUNDRA.ID, GRASS_TUNDRA },
                { GRASS_PLAINS.ID, GRASS_PLAINS },
                { GRASS_SAVANNA.ID, GRASS_SAVANNA },
                { GRASS_BOREAL_FOREST.ID, GRASS_BOREAL_FOREST },
                { GRASS_SHRUBLAND.ID, GRASS_SHRUBLAND },
                { GRASS_TEMPERATE_FOREST.ID, GRASS_TEMPERATE_FOREST },
                { GRASS_SWAMP.ID, GRASS_SWAMP },
                { GRASS_SEASONAL_FOREST.ID, GRASS_SEASONAL_FOREST },
                { GRASS_TROPICAL_FOREST.ID, GRASS_TROPICAL_FOREST },*/
            };
        }
    }

    [BurstCompile]
    public struct BlockIDs
    {
        public const byte AIR = 0;
        public const byte BEDROCK = 1;
        public const byte STONE = 2;
        public const byte COBBLESTONE = 3;
        public const byte STONE_BRICKS = 4;
        public const byte GRASS = 5;
        public const byte DIRT = 6;
        public const byte SAND = 7;
        public const byte SAND_DESERT = 8;
        public const byte WOOD = 9;
        public const byte PLANKS = 10;
        public const byte BRICKS = 11;
        public const byte GRAVEL = 12;
        public const byte LEAVES = 13;
        public const byte CACTUS = 14;
        public const byte WATER = 15;
        public const byte GRASS_PLANT = 16;
        /*public const byte GRASS_TUNDRA = 6;
        public const byte GRASS_PLAINS = 7;
        public const byte GRASS_SAVANNA = 8;
        public const byte GRASS_BOREAL_FOREST = 9;
        public const byte GRASS_TAIGA = 20;
        public const byte GRASS_SHRUBLAND = 22;
        public const byte GRASS_TEMPERATE_FOREST = 22;
        public const byte GRASS_SWAMP = 25;
        public const byte GRASS_SEASONAL_FOREST = 23;
        public const byte GRASS_TROPICAL_FOREST = 25;*/
    }
}