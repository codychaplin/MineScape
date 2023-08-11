﻿using Unity.Jobs;
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

                    // elevation
                    int terrainHeight = 0;
                    float elevationX = Noise.GetTerrainNoise(pos, 0, elevationScale, elevationOctaves, persistance, lacunarity, 15, 2.4f);
                    if (elevationX < 0f)
                    {
                        int depth = GetY(Splines.Elevation, elevationX);
                        float elevationY = Noise.GetTerrainNoise(pos, 0, elevationScale * 10, elevationOctaves, persistance, lacunarity, 10, 1f);
                        float ratio = math.lerp(elevationX, elevationY, math.clamp(elevationX * 10, -1, 1));
                        terrainHeight = math.clamp(depth + (int)math.floor(ratio * 8), 32, 65);
                    }
                    else
                    {
                        float relief = Noise.GetTerrainNoise(pos, -10000, reliefScale, reliefOctaves, persistance, lacunarity, 12, 3f);
                        float normalizedRelief = (relief + 1f) / 2f;
                        terrainHeight = 63 + (int)math.floor(elevationX * normalizedRelief * 64);
                    }

                    /*int elevationY = GetY(Splines.Elevation, elevationX);
                    int terrainHeight = elevationY;*/

                    // relief
                    /*float relief = Noise.GetTerrainNoise(pos, -10000, reliefScale, reliefOctaves, persistance, lacunarity, 12, 2.4f);
                    float normalizedRelief = (relief + 1f) / 2f;

                    // topography
                    float topographyX = Noise.GetTerrainNoise(pos, 10000, topographyScale, topographyOctaves, persistance, lacunarity, 8, 1.7f);
                    topographyX *= normalizedRelief;
                    int topographyY = GetY(Splines.Topography, topographyX);*/

                    // final terrain height
                    //int terrainHeight = (int)math.round(elevationY + topographyY);

                    float temperature = Noise.GetBiomeNoise(pos, 0, 0.06f, true);
                    float humidity = Noise.GetBiomeNoise(pos, 0, 0.15f, true);
                    byte biomeID = GetBiome(elevationX, temperature, humidity);
                    int biomeIndex = x + z * Constants.ChunkWidth;
                    biomeMap[biomeIndex] = biomeID;
                    var biome = Biomes.biomes[biomeID];

                    // set blocks
                    for (int y = 0; y < Constants.ChunkHeight; y++)
                            {
                                int index = Chunk.ConvertToIndex(x, y, z);
                                if (y == 0)
                                    blockMap[index] = Blocks.BEDROCK.ID;
                                else if (y < terrainHeight - 4)
                                    blockMap[index] = Blocks.STONE.ID;
                                else if (y <= terrainHeight)
                                    blockMap[index] = biome.SurfaceBlock.ID;
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

        byte GetBiome(float elevation, float temperature, float humidity)
        {
            if (elevation < -0.03)
            {
                if (temperature >= 0 && temperature < 0.33)
                    return Biomes.COLD_OCEAN.ID;
                if (temperature >= 0.33 && temperature < 0.66)
                    return Biomes.OCEAN.ID;
                if (temperature >= 0.66 && temperature <= 1)
                    return Biomes.WARM_OCEAN.ID;
            }
            else if (elevation >= -0.03 && elevation < 0.025)
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