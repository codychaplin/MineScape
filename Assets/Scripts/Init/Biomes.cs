using minescape.biomes;
using System.Collections.Generic;

namespace minescape.init
{
    public static class Biomes
    {
        public static Biome TUNDRA = new(0, 68, Blocks.GRASS_TUNDRA, Blocks.DIRT, 0.9f);
        public static Biome PLAINS = new(1, 66, Blocks.GRASS_PLAINS, Blocks.DIRT, 0.91f);
        public static Biome SAVANNA = new(2, 70, Blocks.GRASS_SAVANNA, Blocks.DIRT, 0.92f);
        public static Biome DESERT = new(3, 65, Blocks.SAND_DESERT, Blocks.SAND_DESERT, 0.92f);
        public static Biome BOREAL_FOREST = new(4, 80, Blocks.GRASS_BOREAL_FOREST, Blocks.DIRT, 0.85f);
        public static Biome TAIGA = new(5, 72, Blocks.GRASS_TAIGA, Blocks.DIRT, 0.9f);
        public static Biome SHRUBLAND = new(6, 74, Blocks.GRASS_SHRUBLAND, Blocks.DIRT, 0.9f);
        public static Biome TEMPERATE_FOREST = new(7, 78, Blocks.GRASS_TEMPERATE_FOREST, Blocks.DIRT, 0.81f);
        public static Biome SWAMP = new(8, 65, Blocks.GRASS_SWAMP, Blocks.DIRT, 0.9f);
        public static Biome SEASONAL_FOREST = new (9, 85, Blocks.GRASS_SEASONAL_FOREST, Blocks.DIRT, 0.83f);
        public static Biome TROPICAL_FOREST = new(10, 90, Blocks.GRASS_TROPICAL_FOREST, Blocks.DIRT, 0.8f);
        public static Biome BEACH = new(11, 64, Blocks.SAND, Blocks.SAND, 1f);
        public static Biome COLD_OCEAN = new(12, 32, Blocks.STONE, Blocks.STONE, 1f);
        public static Biome OCEAN = new(13, 48, Blocks.GRAVEL, Blocks.GRAVEL, 1f);
        public static Biome WARM_OCEAN = new(14, 56, Blocks.SAND, Blocks.SAND, 1f);

        public static Dictionary<byte, Biome> biomes = new()
        {
            { 0, TUNDRA },
            { 1, PLAINS },
            { 2, SAVANNA },
            { 3, DESERT },
            { 4, BOREAL_FOREST },
            { 5, TAIGA },
            { 6, SHRUBLAND },
            { 7, TEMPERATE_FOREST },
            { 8, SWAMP },
            { 9, SEASONAL_FOREST },
            { 10, TROPICAL_FOREST },
            { 11, BEACH },
            { 12, COLD_OCEAN },
            { 13, OCEAN },
            { 14, WARM_OCEAN }
        };
    }
}