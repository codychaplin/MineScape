using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using minescape.init;
using minescape.world.chunk;

namespace minescape.jobs
{
    public struct SetBlockDataJob : IJob
    {   
        [ReadOnly] public int minTerrainheight;
        [ReadOnly] public int maxTerrainheight;

        [ReadOnly] public int3 position;
        [WriteOnly] public NativeArray<byte> blockMap;
        [WriteOnly] public NativeArray<byte> biomeMap;

        public void Execute()
        {
            for (int x = 0; x < Constants.ChunkWidth; x++)
            {
                for (int z = 0; z < Constants.ChunkWidth; z++)
                {
                    var pos = new float2(position.x + x, position.z + z);
                    float elevation = Noise.GetTerrainNoise(pos, 0, 0.1f, 2, 0.5f, 2f, 15, 2.35f, TerrainNoise.Elevation);
                    int terrainHeight = (int)math.floor(minTerrainheight + elevation * maxTerrainheight);

                    /*float temperature = Noise.GetClamped2DNoise(pos, 0, 0.06f, false);
                    float humidity = Noise.GetClamped2DNoise(pos, 0, 0.15f, false);
                    byte biomeID = GetBiome(land, temperature, humidity);
                    int biomeIndex = x + z * Constants.ChunkWidth;
                    biomeMap[biomeIndex] = biomeID;
                    var biome = Biomes.biomes[biomeID];*/

                    // set blocks
                    for (int y = 0; y < Constants.ChunkHeight; y++)
                    {
                        int index = Chunk.ConvertToIndex(x, y, z);
                        if (y == 0)
                            blockMap[index] = Blocks.BEDROCK.ID;
                        else if (y <= terrainHeight)
                            blockMap[index] = Blocks.STONE.ID;
                        /*else if (y == terrainHeight)
                            blockMap[index] = biome.SurfaceBlock.ID;*/
                        else if (y > terrainHeight && y == Constants.WaterLevel)
                            blockMap[index] = Blocks.WATER.ID;
                        else if (y > terrainHeight && y > Constants.WaterLevel)
                            break;
                    }
                }
            }
        }

        byte GetBiome(float land, float temperature, float humidity)
        {
            if (land < 0.4)
            {
                if (temperature >= 0 && temperature < 0.33)
                    return Biomes.COLD_OCEAN.ID;
                if (temperature >= 0.33 && temperature < 0.66)
                    return Biomes.OCEAN.ID;
                if (temperature >= 0.66 && temperature <= 1)
                    return Biomes.WARM_OCEAN.ID;
            }
            else if (land >= 0.4 && land < 0.42)
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