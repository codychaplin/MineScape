using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using minescape.init;
using minescape.world.chunk;
using minescape.splines;

namespace minescape.jobs
{
    public struct SetMapBlockDataJob : IJob
    {
        [ReadOnly] public int seed;

        [ReadOnly] public int minTerrainheight;
        [ReadOnly] public int maxTerrainheight;

        [ReadOnly] public float elevationScale;
        [ReadOnly] public int elevationOctaves;
        [ReadOnly] public float reliefScale;
        [ReadOnly] public int reliefOctaves;
        [ReadOnly] public float topographyScale;
        [ReadOnly] public int topographyOctaves;
        [ReadOnly] public float persistance;
        [ReadOnly] public float lacunarity;

        [ReadOnly] public int2 position;
        [WriteOnly] public NativeArray<byte> biomeMap;

        public void Execute()
        {
            for (int x = 0; x < Constants.MapChunkWidth; x++)
            {
                for (int z = 0; z < Constants.MapChunkWidth; z++)
                {
                    var pos = new float2(position.x + x, position.y + z);
                    float elevationX = Noise.GetTerrainNoise(pos, seed, 1, elevationScale, elevationOctaves, persistance, lacunarity, 15, 2.4f);
                    float elevationY = Noise.GetTerrainNoise(pos, seed, 1, elevationScale * 10, elevationOctaves, persistance, lacunarity, 10, 1f);
                    float smooth = math.lerp(elevationX, elevationY, math.clamp(elevationX * 10, -1, 1));
                    float relief = Noise.GetTerrainNoise(pos, seed, -10000, reliefScale, reliefOctaves, persistance, lacunarity, 12, 3f);
                    float normalizedRelief = (relief + 1f) / 2f;
                    int terrainHeight = 63 + (int)math.floor((elevationX * normalizedRelief * 96) + smooth * 16);
                    float temperature = Noise.GetBiomeNoise(pos, seed, 1, 0.06f, true);
                    float humidity = Noise.GetBiomeNoise(pos, seed, 1, 0.15f, true);

                    // set biome for x/z coordinates in chunk
                    byte biomeID = GetBiome(elevationX, temperature, humidity);
                    var index = MapChunk.ConvertToIndex(x, z);
                    biomeMap[index] = biomeID;
                }
            }
        }

        byte GetBiome(float elevation, float temperature, float humidity)
        {
            // ocean biomes
            if (elevation < -0.03)
            {
                if (temperature < 0.33) return Biomes.COLD_OCEAN.ID;
                if (temperature < 0.66) return Biomes.OCEAN.ID;
                return Biomes.WARM_OCEAN.ID;
            }

            // beach biome
            if (elevation >= -0.03 && elevation < 0.025) return Biomes.BEACH.ID;

            // land biomes
            if (temperature >= 0 && temperature < 0.2 && humidity >= 0 && humidity < 0.6)
                return Biomes.TUNDRA.ID;
            if (temperature >= 0.2 && temperature < 0.6 && humidity >= 0 && humidity < 0.4)
                return Biomes.PLAINS.ID;
            if (temperature >= 0.6 && temperature < 0.8 && humidity >= 0 && humidity < 0.4)
                return Biomes.SAVANNA.ID;
            if (temperature >= 0.8 && temperature <= 1 && humidity >= 0 && humidity < 0.6)
                return Biomes.DESERT.ID;
            if (temperature >= 0 && temperature < 0.2 && humidity >= 0.6 && humidity <= 1)
                return Biomes.BOREAL_FOREST.ID;
            if (temperature >= 0.2 && temperature < 0.4 && humidity >= 0.4 && humidity <= 1)
                return Biomes.TAIGA.ID;
            if (temperature >= 0.4 && temperature < 0.8 && humidity >= 0.4 && humidity < 0.6)
                return Biomes.SHRUBLAND.ID;
            if (temperature >= 0.4 && temperature < 0.8 && humidity >= 0.6 && humidity < 0.8)
                return Biomes.TEMPERATE_FOREST.ID;
            if (temperature >= 0.4 && temperature < 0.6 && humidity >= 0.8 && humidity <= 1)
                return Biomes.SWAMP.ID;
            if (temperature >= 0.8 && temperature <= 1 && humidity >= 0.6 && humidity < 0.8)
                return Biomes.SEASONAL_FOREST.ID;
            if (temperature >= 0.6 && temperature <= 1 && humidity >= 0.8 && humidity <= 1)
                return Biomes.TROPICAL_FOREST.ID;

            return Biomes.PLAINS.ID;
        }
    }
}