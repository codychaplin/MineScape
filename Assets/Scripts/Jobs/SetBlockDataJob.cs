using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using minescape.init;
using minescape.splines;
using minescape.world.chunk;

namespace minescape.jobs
{
    public struct SetBlockDataJob : IJob
    {   
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
                    float elevationX = Noise.GetTerrainNoise(pos, 0, elevationScale, elevationOctaves, persistance, lacunarity, 15, 2.35f, TerrainNoise.Elevation);
                    //int terrainHeight = (int)math.floor(minTerrainheight + (elevation * maxTerrainheight));
                    //UnityEngine.Debug.Log($"terrainHeight: {terrainHeight}, elevation:{elevation}");
                    int elevationY = GetY(Splines.Elevation, elevationX);
                    
                    float relief = Noise.GetTerrainNoise(pos, -10000, reliefScale, reliefOctaves, persistance, lacunarity, 12, 2.35f, TerrainNoise.Relief);
                    float normalizedRelief = (relief + 1f) / 2f;

                    float topographyX = Noise.GetTerrainNoise(pos, 10000, topographyScale, topographyOctaves, persistance, lacunarity, 8, 1.65f, TerrainNoise.Topography);
                    topographyX *= normalizedRelief;
                    int topographyY = GetY(Splines.Topography, topographyX);
                    int terrainHeight = elevationY + topographyY;

                    /*float topography = Noise.GetTerrainNoise(pos, 10000, topographyScale, topographyOctaves, persistance, lacunarity, 8, 1.65f, TerrainNoise.Topography);
                    int terrainHeight = (int)math.floor(minTerrainheight + elevation * maxTerrainheight);*/

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

        static int GetY(float2[] spline, float elevationX)
        {
            int lowerIndex = 0;
            int upperIndex = 1;
            for (int i = 1; i < spline.Length; i++)
            {
                if (elevationX <= spline[i].x)
                {
                    upperIndex = i;
                    break;
                }
                lowerIndex = i;
            }
            float x0 = spline[lowerIndex].x;
            float y0 = spline[lowerIndex].y;
            float x1 = spline[upperIndex].x;
            float y1 = spline[upperIndex].y;
            float t = (elevationX - x0) / (x1 - x0);
            float interpolatedHeight = CubicInterpolate(x0, y0, x1, y1, t);
            return (int)interpolatedHeight;
        }

        static float CubicInterpolate(float x0, float y0, float x1, float y1, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            float a = 2 * t3 - 3 * t2 + 1;
            float b = t3 - 2 * t2 + t;
            float c = -2 * t3 + 3 * t2;
            float d = t3 - t2;
            return a * y0 + b * (x1 - x0) + c * y1 + d * (x1 - x0);
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