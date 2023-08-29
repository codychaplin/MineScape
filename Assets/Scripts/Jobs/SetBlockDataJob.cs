using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using minescape.init;
using minescape.splines;
using minescape.structures;
using minescape.world.chunk;

namespace minescape.jobs
{
    public struct SetBlockDataJob : IJob
    {
        [ReadOnly] public int seed;

        [ReadOnly] public int minTerrainheight;
        [ReadOnly] public int maxTerrainheight;

        [ReadOnly] public float elevationScale;
        [ReadOnly] public int elevationOctaves;
        [ReadOnly] public float reliefScale;
        [ReadOnly] public int reliefOctaves;
        [ReadOnly] public float persistance;
        [ReadOnly] public float lacunarity;

        [ReadOnly] public int3 position;

        [WriteOnly] public NativeArray<byte> blockMap;
        [WriteOnly] public NativeArray<byte> biomeMap;
        [WriteOnly] public NativeArray<byte> heightMap;
        [WriteOnly] public NativeList<Structure> structureMap;

        public void Execute()
        {
            seed *= 7919; // makes seed more unique

            for (int x = 0; x < Constants.ChunkWidth; x++)
            {
                for (int z = 0; z < Constants.ChunkWidth; z++)
                {
                    // offset
                    var pos = new float2(position.x + x, position.z + z);

                    // elevation
                    int terrainHeight = 0;

                    // base terrain height
                    float elevationX = Noise.GetTerrainNoise(pos, seed, 1, elevationScale, elevationOctaves, persistance, lacunarity, 15, 2.4f);

                    // secondary terrain height with higher frequency
                    float elevationY = Noise.GetTerrainNoise(pos, seed, 1, elevationScale * 10, elevationOctaves, persistance, lacunarity, 10, 1f);

                    // scales down elevationY close to water level
                    float smooth = math.lerp(elevationX, elevationY, math.clamp(elevationX * 10, -1, 1));

                    if (elevationX < 0f) // ocean
                    {
                        int depth = GetY(Splines.Elevation, elevationX);
                        terrainHeight = math.clamp(depth + (int)math.floor(smooth * 8), 32, 65);
                    }
                    else // land
                    {
                        float relief = Noise.GetTerrainNoise(pos, seed, -10000, reliefScale, reliefOctaves, persistance, lacunarity, 12, 3f);
                        float normalizedRelief = (relief + 1f) / 2f;

                        terrainHeight = 63 + (int)math.floor((elevationX * normalizedRelief * 96) + smooth * 16);
                    }

                    // set biome
                    float temperature = Noise.GetBiomeNoise(pos, seed, 1, 0.06f, true);
                    float humidity = Noise.GetBiomeNoise(pos, seed, 1, 0.15f, true);
                    byte biomeID = GetBiome(elevationX, temperature, humidity);
                    int Index2D = x + z * Constants.ChunkWidth;
                    biomeMap[Index2D] = biomeID;
                    var biome = Biomes.biomes[biomeID];

                    heightMap[Index2D] = (byte)terrainHeight;

                    // set blocks
                    int index = 0;
                    for (int y = 0; y < Constants.ChunkHeight; y++)
                    {
                        index = Chunk.ConvertToIndex(x, y, z);
                        if (y == 0)
                            blockMap[index] = Blocks.BEDROCK.ID;
                        else if (y < terrainHeight - 4)
                            blockMap[index] = Blocks.STONE.ID;
                        else if (y < terrainHeight)
                            blockMap[index] = biome.FillerBlock.ID;
                        else if (y == terrainHeight)
                            blockMap[index] = biome.SurfaceBlock.ID;
                        else if (y > terrainHeight && y <= Constants.WaterLevel)
                            blockMap[index] = Blocks.WATER.ID;
                        else
                            break;
                    }

                    // set trees
                    var treeMap = Noise.TreeNoise(pos, 15f);
                    if (treeMap > biome.TreeFrequency && terrainHeight >= Constants.WaterLevel && biome.ID <= 10) // above threshold and not a beach/ocean biome
                    {
                        if (biome.ID == Biomes.DESERT.ID)
                            structureMap.Add(new Structure(Structures.CACTUS.ID, Structures.CACTUS.Radius, Type.Cactus, new int3(x, terrainHeight + 1, z)));
                        else
                            structureMap.Add(new Structure(Structures.TREE.ID, Structures.TREE.Radius, Type.Tree, new int3(x, terrainHeight + 1, z)));
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