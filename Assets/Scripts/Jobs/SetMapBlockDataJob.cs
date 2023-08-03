using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using minescape.init;
using minescape.world.chunk;

namespace minescape.jobs
{
    public struct SetMapBlockDataJob : IJob
    {
        [ReadOnly] public float temperatureScale;
        [ReadOnly] public float temperatureOffset;
        [ReadOnly] public float temperatureBorderWeight;
        [ReadOnly] public float temperatureBorderScale;

        [ReadOnly] public float humidityScale;
        [ReadOnly] public float humidityOffset;
        [ReadOnly] public float humidityBorderWeight;
        [ReadOnly] public float humidityBorderScale;

        [ReadOnly] public float landOffset;
        [ReadOnly] public float landScale;
        [ReadOnly] public float landBorderWeight;
        [ReadOnly] public float landBorderScale;

        [ReadOnly] public int2 position;
        [WriteOnly] public NativeArray<byte> biomeMap;

        public void Execute()
        {
            for (int x = 0; x < Constants.MapChunkWidth; x++)
            {
                for (int z = 0; z < Constants.MapChunkWidth; z++)
                {
                    var pos = new float2(position.x + x, position.y + z);

                    // land and sea
                    float land1 = Noise.GetPerlin(pos, landOffset, landScale);
                    float land2 = Noise.GetPerlin(pos, landOffset, landScale * landBorderScale);
                    float land = (land1 + land2 / landBorderWeight) / ((1 / landBorderWeight) + 1);

                    // temperature
                    float temperature1 = Noise.GetPerlin(pos, temperatureOffset, temperatureScale);
                    float temperature2 = Noise.GetSimplex(pos, temperatureOffset, temperatureScale * temperatureBorderScale);
                    float temperature = (temperature1 + temperature2 / temperatureBorderWeight) / ((1 / temperatureBorderWeight) + 1);

                    // humidity
                    float humidity1 = Noise.GetPerlin(pos, humidityOffset, humidityScale);
                    float humidity2 = Noise.GetSimplex(pos, humidityOffset, humidityScale * humidityBorderScale);
                    float humidity = (humidity1 + humidity2 / humidityBorderWeight) / ((1 / humidityBorderWeight) + 1);

                    // set biome for x/z coordinates in chunk
                    byte biomeID = GetBiome(land, temperature, humidity);
                    var index = MapChunk.ConvertToIndex(x, z);
                    biomeMap[index] = biomeID;
                }
            }
        }

        byte GetBiome(float land, float temperature, float humidity)
        {
            if (land < 0.3)
            {
                if (temperature >= 0 && temperature < 0.33)
                    return Biomes.COLD_OCEAN.ID;
                if (temperature >= 0.33 && temperature < 0.66)
                    return Biomes.OCEAN.ID;
                if (temperature >= 0.66 && temperature <= 1)
                    return Biomes.WARM_OCEAN.ID;
            }
            else if (land >= 0.3 && land < 0.32)
                return Biomes.BEACH.ID;

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