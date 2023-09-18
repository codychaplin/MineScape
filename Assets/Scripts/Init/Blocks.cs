using Unity.Burst;
using Unity.Collections;
using minescape.block;

namespace minescape.init
{
    public class Blocks
    {
        public Block AIR = new(0, 255, 255, 255, 255, 255, 255, false, true, false);
        public Block BEDROCK = new(1, 0, 0, 0, 0, 0, 0 );
        public Block STONE = new(2, 1, 1, 1, 1, 1, 1 );
        public Block COBBLESTONE = new(3, 17, 17, 17, 17, 17, 17);
        public Block STONE_BRICKS = new(4, 2, 2, 2, 2, 2, 2);
        public Block GRASS = new(5, 19, 19, 35, 3, 19, 19, true);
        public Block DIRT = new(6, 3, 3, 3, 3, 3, 3, true );
        public Block SAND = new(7, 4, 4, 4, 4, 4, 4 );
        public Block SAND_DESERT = new(8, 20, 20, 20, 20, 20, 20);
        public Block WOOD = new(9, 5, 5, 21, 21, 5, 5);
        public Block PLANKS = new(10, 6, 6, 6, 6, 6, 6);
        public Block GRAVEL = new(11, 7, 7, 7, 7, 7, 7);
        public Block BRICKS = new(12, 8, 8, 8, 8, 8, 8);
        public Block LEAVES = new(13, 9, 9, 9, 9, 9, 9, true);
        public Block CACTUS = new(14, 10, 10, 26, 26, 10, 10);
        public Block WATER = new(15, 16, 16, 16, 16, 16, 16, false, true);
        public Block GRASS_PLANT = new(16, 11, 11, 11, 11, 11, 11, true, true, true, true);
        // ores
        public Block COAL_ORE = new(17, 48, 48, 48, 48, 48, 48 );
        public Block IRON_ORE = new(18, 49, 49, 49, 49, 49, 49 );
        public Block COPPER_ORE = new(19, 50, 50, 50, 50, 50, 50);
        public Block ZINC_ORE = new(20, 51, 51, 51, 51, 51, 51);
        public Block TIN_ORE = new(21, 52, 52, 52, 52, 52, 52);
        public Block TITANIUM_ORE = new(22, 53, 53, 53, 53, 53, 53);
        public Block TUNGSTEN_ORE = new(23, 54, 54, 54, 54, 54, 54);
        public Block SILVER_ORE = new(24, 55, 55, 55, 55, 55, 55);
        public Block GOLD_ORE = new(25, 56, 56, 56, 56, 56, 56);
        public Block DIAMOND_ORE = new(26, 57, 57, 57, 57, 57, 57);


        public NativeHashMap<byte, Block> blocks;

        public Blocks()
        {
            blocks = new(27, Allocator.Persistent)
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
                { GRAVEL.ID, GRAVEL },
                { BRICKS.ID, BRICKS },
                { LEAVES.ID, LEAVES },
                { CACTUS.ID, CACTUS },
                { WATER.ID, WATER },
                { GRASS_PLANT.ID, GRASS_PLANT },
                { COAL_ORE.ID, COAL_ORE },
                { IRON_ORE.ID, IRON_ORE },
                { COPPER_ORE.ID, COPPER_ORE },
                { ZINC_ORE.ID, ZINC_ORE },
                { TIN_ORE.ID, TIN_ORE },
                { TITANIUM_ORE.ID, TITANIUM_ORE },
                { TUNGSTEN_ORE.ID, TUNGSTEN_ORE },
                { SILVER_ORE.ID, SILVER_ORE },
                { GOLD_ORE.ID, GOLD_ORE },
                { DIAMOND_ORE.ID, DIAMOND_ORE }
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
        public const byte COAL_ORE = 17;
        public const byte IRON_ORE = 18;
        public const byte COPPER_ORE = 19;
        public const byte ZINC_ORE = 20;
        public const byte TIN_ORE = 21;
        public const byte TITANIUM_ORE = 22;
        public const byte TUNGSTEN_ORE = 23;
        public const byte SILVER_ORE = 24;
        public const byte GOLD_ORE = 25;
        public const byte DIAMOND_ORE = 26;
    }
}