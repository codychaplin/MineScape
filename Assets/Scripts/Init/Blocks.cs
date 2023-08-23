using minescape.block;
using System.Collections.Generic;

namespace minescape.init
{
    public static class Blocks
    {
        public static Block AIR = new(0, "Air", new byte[6], true, false);
        public static Block BEDROCK = new(1, "Bedrock", new byte[] { 0, 0, 0, 0, 0, 0 });
        public static Block STONE = new(2, "Stone", new byte[] { 1, 1, 1, 1, 1, 1 });
        public static Block DIRT = new(3, "Dirt", new byte[] { 3, 3, 3, 3, 3, 3 });
        public static Block SAND = new(4, "Sand", new byte[] { 4, 4, 4, 4, 4, 4 });
        public static Block WATER = new(5, "Water", new byte[] { 8, 8, 8, 8, 8, 8 }, true);
        public static Block GRASS_TUNDRA = new(6, "Grass Tundra", new byte[] { 9, 9, 9, 9, 9, 9 });
        public static Block GRASS_PLAINS = new(7, "Grass Plains", new byte[] { 10, 10, 10, 10, 10, 10 });   
        public static Block GRASS_SAVANNA = new(8, "Grass Savanna", new byte[] { 11, 11, 11, 11, 11, 11 });
        public static Block GRASS_BOREAL_FOREST = new(9, "Grass Boreal Forest", new byte[] { 12, 12, 12, 12, 12, 12 });
        public static Block GRASS_TAIGA = new(10, "Grass Taiga", new byte[] { 13, 13, 13, 13, 13, 13 });
        public static Block GRASS_SHRUBLAND = new(11, "Grass Shrubland", new byte[] { 14, 14, 14, 14, 14, 14 });
        public static Block GRASS_TEMPERATE_FOREST = new(12, "Grass Temperate Forest", new byte[] { 15, 15, 15, 15, 15, 15 });
        public static Block GRASS_SWAMP = new(13, "Grass Swamp", new byte[] { 16, 16, 16, 16, 16, 16 });
        public static Block GRASS_SEASONAL_FOREST = new(14, "Grass Seasonal Forest", new byte[] {17, 17, 17, 17, 17, 17 });
        public static Block GRASS_TROPICAL_FOREST = new(15, "Grass Tropical Forest", new byte[] { 18, 18, 18, 18, 18, 18 });
        public static Block SAND_DESERT = new(16, "Sand Desert", new byte[] { 19, 19, 19, 19, 19, 19 });
        public static Block GRAVEL = new(17, "Gravel", new byte[] { 20, 20, 20, 20, 20, 20 });
        public static Block WOOD = new(18, "Wood", new byte[] { 6, 6, 5, 5, 6, 6 });
        public static Block LEAVES = new(19, "Leaves", new byte[] { 21, 21, 21, 21, 21, 21 }, true);

        public static Dictionary<byte, Block> blocks = new()
        {
            { 0, AIR },
            { 1, BEDROCK },
            { 2, STONE },
            { 3, DIRT },
            { 4, SAND },
            { 5, WATER },
            { 6, GRASS_TUNDRA },
            { 7, GRASS_PLAINS },
            { 8, GRASS_SAVANNA },
            { 9, GRASS_BOREAL_FOREST },
            { 10, GRASS_TAIGA },
            { 11, GRASS_SHRUBLAND },
            { 12, GRASS_TEMPERATE_FOREST },
            { 13, GRASS_SWAMP },
            { 14, GRASS_SEASONAL_FOREST },
            { 15, GRASS_TROPICAL_FOREST },
            { 16, SAND_DESERT },
            { 17, GRAVEL },
            { 18, WOOD },
            { 19, LEAVES }
        };
        
    }
}