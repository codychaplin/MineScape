using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using minescape.init;
using minescape.world.chunk;

namespace minescape.jobs
{
    public struct SetMapBlockDataJob : IJob
    {
        [ReadOnly] public int seed;

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
            seed *= 7919; // makes seed more unique

            for (int x = 0; x < Constants.MapChunkWidth; x++)
            {
                for (int z = 0; z < Constants.MapChunkWidth; z++)
                {
                    var pos = new float2(position.x + x, position.y + z);
                    float elevationX = Noise.GetTerrainNoise(pos, seed, 1, elevationScale, elevationOctaves, persistance, lacunarity, 15, 2.4f);
                    float temperature = Noise.GetBiomeNoise(pos, seed, 1, 0.06f, true);
                    float humidity = Noise.GetBiomeNoise(pos, seed, 1, 0.15f, true);
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
                if (temperature < 0.33) return BiomesIDs.COLD_OCEAN;
                if (temperature < 0.66) return BiomesIDs.OCEAN;
                return BiomesIDs.WARM_OCEAN;
            }

            // beach biome
            if (elevation >= -0.03 && elevation < 0.025) return BiomesIDs.BEACH;

            // land biomes
            if (temperature >= 0 && temperature < 0.2 && humidity >= 0 && humidity < 0.6)
                return BiomesIDs.TUNDRA;
            if (temperature >= 0.2 && temperature < 0.6 && humidity >= 0 && humidity < 0.4)
                return BiomesIDs.PLAINS;
            if (temperature >= 0.6 && temperature < 0.8 && humidity >= 0 && humidity < 0.4)
                return BiomesIDs.SAVANNA;
            if (temperature >= 0.8 && temperature <= 1 && humidity >= 0 && humidity < 0.6)
                return BiomesIDs.DESERT;
            if (temperature >= 0 && temperature < 0.2 && humidity >= 0.6 && humidity <= 1)
                return BiomesIDs.BOREAL_FOREST;
            if (temperature >= 0.2 && temperature < 0.4 && humidity >= 0.4 && humidity <= 1)
                return BiomesIDs.TAIGA;
            if (temperature >= 0.4 && temperature < 0.8 && humidity >= 0.4 && humidity < 0.6)
                return BiomesIDs.SHRUBLAND;
            if (temperature >= 0.4 && temperature < 0.8 && humidity >= 0.6 && humidity < 0.8)
                return BiomesIDs.TEMPERATE_FOREST;
            if (temperature >= 0.4 && temperature < 0.6 && humidity >= 0.8 && humidity <= 1)
                return BiomesIDs.SWAMP;
            if (temperature >= 0.8 && temperature <= 1 && humidity >= 0.6 && humidity < 0.8)
                return BiomesIDs.SEASONAL_FOREST;
            if (temperature >= 0.6 && temperature <= 1 && humidity >= 0.8 && humidity <= 1)
                return BiomesIDs.TROPICAL_FOREST;

            return BiomesIDs.PLAINS;
        }
    }
}