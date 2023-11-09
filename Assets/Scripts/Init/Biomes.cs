using Unity.Burst;
using Unity.Collections;
using minescape.biome;

namespace minescape.init
{
    public class Biomes
    {                                                                                                   // R  G  B
        public Biome TUNDRA = new(0, BlockIDs.GRASS, BlockIDs.DIRT, 0.9f, 0.86f, 34194);                // 16 44 18
        public Biome PLAINS = new(1, BlockIDs.GRASS, BlockIDs.DIRT, 0.91f, 0.77f, 38379);               // 18 47 11
        public Biome SAVANNA = new(2, BlockIDs.GRASS, BlockIDs.DIRT, 0.92f, 0.87f, 48554);              // 23 45 10
        public Biome DESERT = new(3, BlockIDs.SAND_DESERT, BlockIDs.SAND_DESERT, 0.91f, 0.91f, 62858);  // 30 44 10
        public Biome BOREAL_FOREST = new(4, BlockIDs.GRASS, BlockIDs.DIRT, 0.85f, 0.86f, 36273);        // 17 45 17
        public Biome TAIGA = new(5, BlockIDs.GRASS, BlockIDs.DIRT, 0.9f, 0.88f, 34224);                 // 16 45 16
        public Biome SHRUBLAND = new(6, BlockIDs.GRASS, BlockIDs.DIRT, 0.9f, 0.77f, 36301);             // 17 46 13
        public Biome TEMPERATE_FOREST = new(7, BlockIDs.GRASS, BlockIDs.DIRT, 0.81f, 0.84f, 32235);     // 15 47 11
        public Biome SWAMP = new(8, BlockIDs.GRASS, BlockIDs.DIRT, 0.9f, 0.86f, 27527);                 // 13 28 7
        public Biome SEASONAL_FOREST = new (9, BlockIDs.GRASS, BlockIDs.DIRT, 0.83f, 0.82f, 21446);     // 10 30 6
        public Biome TROPICAL_FOREST = new(10, BlockIDs.GRASS, BlockIDs.DIRT, 0.8f, 0.78f, 24135);      // 11 50 7
        public Biome BEACH = new(11, BlockIDs.SAND, BlockIDs.SAND, 1f, 1f, 38379);                      // 18 47 11
        public Biome COLD_OCEAN = new(12, BlockIDs.STONE, BlockIDs.STONE, 1f, 1f, 38379);               // 18 47 11
        public Biome OCEAN = new(13, BlockIDs.GRAVEL, BlockIDs.GRAVEL, 1f, 1f, 38379);                  // 18 47 11
        public Biome WARM_OCEAN = new(14, BlockIDs.SAND, BlockIDs.SAND, 1f, 1f, 38379);                 // 18 47 11

        public NativeHashMap<byte, Biome> biomes;

        public Biomes()
        {
            biomes = new(15, Allocator.Persistent)
            {
                { TUNDRA.ID, TUNDRA },
                { PLAINS.ID, PLAINS },
                { SAVANNA.ID, SAVANNA },
                { DESERT.ID, DESERT },
                { BOREAL_FOREST.ID, BOREAL_FOREST },
                { TAIGA.ID, TAIGA },
                { SHRUBLAND.ID, SHRUBLAND },
                { TEMPERATE_FOREST.ID, TEMPERATE_FOREST },
                { SWAMP.ID, SWAMP },
                { SEASONAL_FOREST.ID, SEASONAL_FOREST },
                { TROPICAL_FOREST.ID, TROPICAL_FOREST },
                { BEACH.ID, BEACH },
                { COLD_OCEAN.ID, COLD_OCEAN },
                { OCEAN.ID, OCEAN },
                { WARM_OCEAN.ID, WARM_OCEAN }
            };
        }
    }

    [BurstCompile]
    public struct BiomesIDs
    {
        public const byte TUNDRA = 0;
        public const byte PLAINS = 1;
        public const byte SAVANNA = 2;
        public const byte DESERT = 3;
        public const byte BOREAL_FOREST = 4;
        public const byte TAIGA = 5;
        public const byte SHRUBLAND = 6;
        public const byte TEMPERATE_FOREST = 7;
        public const byte SWAMP = 8;
        public const byte SEASONAL_FOREST = 9;
        public const byte TROPICAL_FOREST = 10;
        public const byte BEACH = 11;
        public const byte COLD_OCEAN = 12;
        public const byte OCEAN = 13;
        public const byte WARM_OCEAN = 14;
    }
}