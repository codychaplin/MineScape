using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using minescape.biome;

namespace minescape.init
{
    public class Biomes
    {
        public Biome TUNDRA = new(0, BlockIDs.GRASS, BlockIDs.DIRT,
            new Color32(128, 180, 151, 255), 0.9f, 0.86f);
        public Biome PLAINS = new(1, BlockIDs.GRASS, BlockIDs.DIRT,
            new Color32(145, 189, 89, 255), 0.91f, 0.77f);
        public Biome SAVANNA = new(2, BlockIDs.GRASS, BlockIDs.DIRT,
            new Color32(191, 183, 85, 255), 0.92f, 0.87f);
        public Biome DESERT = new(3, BlockIDs.SAND_DESERT, BlockIDs.SAND_DESERT,
            new Color32(245, 179, 82, 255), 0.91f, 0.91f);
        public Biome BOREAL_FOREST = new(4, BlockIDs.GRASS, BlockIDs.DIRT,
            new Color32(138, 182, 137, 255), 0.85f, 0.86f);
        public Biome TAIGA = new(5, BlockIDs.GRASS, BlockIDs.DIRT,
            new Color32(134, 183, 131, 255), 0.9f, 0.88f);
        public Biome SHRUBLAND = new(6, BlockIDs.GRASS, BlockIDs.DIRT,
            new Color32(136, 187, 103, 255), 0.9f, 0.77f);
        public Biome TEMPERATE_FOREST = new(7, BlockIDs.GRASS, BlockIDs.DIRT,
            new Color32(121, 192, 90, 255), 0.81f, 0.84f);
        public Biome SWAMP = new(8, BlockIDs.GRASS, BlockIDs.DIRT,
            new Color32(106, 112, 57, 255), 0.9f, 0.86f);
        public Biome SEASONAL_FOREST = new (9, BlockIDs.GRASS, BlockIDs.DIRT,
            new Color32(80, 122, 50, 255), 0.83f, 0.82f);
        public Biome TROPICAL_FOREST = new(10, BlockIDs.GRASS, BlockIDs.DIRT,
            new Color32(89, 201, 60, 255), 0.8f, 0.78f);
        public Biome BEACH = new(11, BlockIDs.SAND, BlockIDs.SAND,
            new Color32(145, 189, 89, 255), 1f, 1f);
        public Biome COLD_OCEAN = new(12, BlockIDs.STONE, BlockIDs.STONE,
            new Color32(145, 189, 89, 255), 1f, 1f);
        public Biome OCEAN = new(13, BlockIDs.GRAVEL, BlockIDs.GRAVEL,
            new Color32(145, 189, 89, 255), 1f, 1f);
        public Biome WARM_OCEAN = new(14, BlockIDs.SAND, BlockIDs.SAND,
            new Color32(145, 189, 89, 255), 1f, 1f);

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