using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using minescape.init;
using minescape.block;
using minescape.biome;
using minescape.structures;

namespace minescape.jobs
{
    [BurstCompile]
    public struct GenerateChunkJob : IJob
    {
        // info
        [ReadOnly] public int seed;
        [ReadOnly] public int3 position;

        // output
        public NativeArray<byte> blockMap;
        public NativeArray<byte> biomeMap;
        public NativeList<Structure> structureMap;

        // 00000000 00000000 00000000 11111 111111 11111 111111 111111 111111 111111
        //                            r     g      b     normal x      y      z
        //                            5     6      5     6      6      6      6
        NativeList<ulong> vertexData;
        NativeList<int> triangles;

        // references
        [ReadOnly] public NativeHashMap<byte, Block> blocks;
        [ReadOnly] public NativeHashMap<byte, Biome> biomes;
        [ReadOnly] public NativeHashMap<byte, Structure> structures;
        [ReadOnly] public NativeArray<float2> elevation;

        // noise
        [ReadOnly] public float elevationScale;
        [ReadOnly] public int elevationOctaves;

        [ReadOnly] public float reliefScale;
        [ReadOnly] public int reliefOctaves;

        [ReadOnly] public float persistance;
        [ReadOnly] public float lacunarity;

        [ReadOnly] public float caveScale;
        [ReadOnly] public int caveOctaves;
        [ReadOnly] public float caveThreshold;

        public void Execute()
        {
            SetBlockData();
            SetStructures();
        }

        /*
         * main functions
         */

        void SetBlockData()
        {
            seed *= 7919; // makes seed more unique

            // -1/+1 to get cross chunk data
            for (int x = -1; x < Constants.ChunkWidth + 1; x++)
                for (int z = -1; z < Constants.ChunkWidth + 1; z++)
                {
                    var pos = new float2(position.x + x, position.z + z); // offset

                    int terrainHeight = 0;
                    float elevationX = Noise.GetTerrainNoise(pos, seed, 1,
                        elevationScale, elevationOctaves, persistance, lacunarity, 15, 2.4f); // base terrain height
                    float elevationY = Noise.GetTerrainNoise(pos, seed, 1,
                        elevationScale * 10, elevationOctaves, persistance, lacunarity, 10, 1f); // secondary terrain height with higher frequency
                    float smooth = math.lerp(elevationX, elevationY, math.clamp(elevationX * 10, -1, 1)); // scales down elevationY close to water level

                    if (elevationX < 0f) // ocean
                    {
                        int depth = GetY(elevation, elevationX);
                        terrainHeight = math.clamp(depth + (int)math.floor(smooth * 8), 32, 65);
                    }
                    else // land
                    {
                        float relief = Noise.GetTerrainNoise(pos, seed, -10000, reliefScale, reliefOctaves, persistance, lacunarity, 12, 3f);
                        float normalizedRelief = (relief + 1f) / 2f;
                        terrainHeight = 63 + (int)math.floor((elevationX * normalizedRelief * 96) + smooth * 16);
                    }

                    // biome
                    float temperature = Noise.GetBiomeNoise(pos, seed, 1, 0.06f, true);
                    float humidity = Noise.GetBiomeNoise(pos, seed, 1, 0.15f, true);
                    byte biomeID = GetBiome(elevationX, temperature, humidity);
                    int Index2D = Utils.ConvertToIndex(x + 1, z + 1);
                    biomeMap[Index2D] = biomeID;
                    var biome = biomes[biomeID];

                    // set blocks
                    int index = 0;
                    for (int y = 0; y < Constants.ChunkHeight; y++)
                    {
                        index = Utils.ConvertToIndex(x + 1, y, z + 1);
                        byte setBlock = 0;
                        if (y == 0)
                            setBlock = BlockIDs.BEDROCK;
                        else if (y < terrainHeight - 4)
                            setBlock = BlockIDs.STONE;
                        else if (y < terrainHeight)
                            setBlock = biome.FillerBlock;
                        else if (y == terrainHeight)
                            setBlock = biome.SurfaceBlock;
                        else if (y > terrainHeight && y <= Constants.WaterLevel)
                            setBlock = BlockIDs.WATER;
                        else
                            break;

                        // caves
                        var pos3D = new float3(position.x + x, position.y + y, position.z + z);
                        int maxHeight = math.min(terrainHeight, terrainHeight - 5 + (int)((elevationX + elevationY * 3) * 5));
                        int minHeight = math.max(3, 3 + (int)(elevationY * 10));
                        if (y >= minHeight && y <= maxHeight)
                        {
                            // caves are the intersections of two 3D noise maps
                            float value1 = Noise.GetCaveNoise(pos3D, seed, 0, caveScale, caveOctaves);
                            if (value1 < caveThreshold)
                            {
                                float value2 = Noise.GetCaveNoise(pos3D, seed, 10000, caveScale, caveOctaves);
                                if (value2 < caveThreshold)
                                    setBlock = BlockIDs.AIR;
                            }
                        }

                        blockMap[index] = setBlock;

                        // ores
                        if (setBlock == BlockIDs.STONE && y < terrainHeight - 4 && y > 4)
                            SetOres(pos3D, y, index);
                    }

                    // set trees
                    var treeMap = Noise.FoliageNoise(pos, 15f);
                    if (treeMap > biome.TreeFrequency)
                    {
                        if (biome.ID == BiomesIDs.DESERT)
                        {
                            var cactus = structures[StructureIDs.CACTUS];
                            structureMap.Add(new Structure(cactus.ID, cactus.Radius, Type.Cactus, new int3(x, terrainHeight + 1, z)));
                        }
                        else
                        {
                            var tree = structures[StructureIDs.TREE];
                            structureMap.Add(new Structure(tree.ID, tree.Radius, Type.Tree, new int3(x, terrainHeight + 1, z)));
                        }
                    }

                }
        }

        void SetStructures()
        {
            foreach (Structure structure in structureMap)
            {
                int x = structure.LocalPosition.x + 1;
                int y = structure.LocalPosition.y;
                int z = structure.LocalPosition.z + 1;
                var rand = new Random((uint)(x * 79 + y * 557 + z * 991));
                byte radius = structure.Radius;

                switch (structure.Type)
                {
                    case Type.Tree:
                        GenerateTree(x, y, z, rand, radius);
                        break;
                    case Type.Cactus:
                        GenerateCactus(x, y, z, rand);
                        break;
                    default:
                        break;
                }
            }
        }

        /*
         * Initializing block map
         */

        int GetY(NativeArray<float2> spline, float elevationX)
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

        float CubicInterpolate(float x0, float y0, float x1, float y1, float t)
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

        void SetOres(float3 pos3D, int y, int index)
        {
            var coal = Noise.GetOreVeinNoise(pos3D, seed, 0, 26f);
            if (coal > 0.42f)
            {
                blockMap[index] = BlockIDs.COAL_ORE;
                return;
            }

            var iron = Noise.GetOreVeinNoise(pos3D, seed, 1000, 24f);
            if (iron > 0.44f)
            {
                blockMap[index] = BlockIDs.IRON_ORE;
                return;
            }

            var copper = Noise.GetOreVeinNoise(pos3D, seed, 2000, 22f);
            if (copper > 0.43f)
            {
                blockMap[index] = BlockIDs.COPPER_ORE;
                return;
            }

            var zinc = Noise.GetOreVeinNoise(pos3D, seed, 3000, 23f);
            if (zinc > 0.46f)
            {
                blockMap[index] = BlockIDs.ZINC_ORE;
                return;
            }

            var tin = Noise.GetOreVeinNoise(pos3D, seed, 4000, 24f);
            if (tin > 0.43f)
            {
                blockMap[index] = BlockIDs.TIN_ORE;
                return;
            }

            var silver = Noise.GetOreVeinNoise(pos3D, seed, 5000, 24f);
            if (silver > 0.45f)
            {
                blockMap[index] = BlockIDs.SILVER_ORE;
                return;
            }

            if (y < 42)
            {
                var titanium = Noise.GetOreVeinNoise(pos3D, seed, 6000, 20f);
                if (titanium > 0.47f)
                {
                    blockMap[index] = BlockIDs.TITANIUM_ORE;
                    return;
                }

                if (y < 36)
                {
                    var gold = Noise.GetOreVeinNoise(pos3D, seed, 7000, 22f);
                    if (gold > 0.47f)
                    {
                        blockMap[index] = BlockIDs.GOLD_ORE;
                        return;
                    }

                    if (y < 32)
                    {
                        var tungsten = Noise.GetOreVeinNoise(pos3D, seed, 8000, 21f);
                        if (tungsten > 0.5f)
                        {
                            blockMap[index] = BlockIDs.TUNGSTEN_ORE;
                            return;
                        }

                        var diamond = Noise.GetOreVeinNoise(pos3D, seed, 9000, 21f);
                        if (diamond > 0.49f)
                        {
                            blockMap[index] = BlockIDs.DIAMOND_ORE;
                            return;
                        }
                    }
                }
            }
        }

        /*
         * Placing structures
         */

        void PlaceBlock(int x, int y, int z, byte blockID)
        {
            if (x < 0 || x >= Constants.ChunkWidth + 2 ||
                y < 0 || y >= Constants.ChunkHeight ||
                z < 0 || z >= Constants.ChunkWidth + 2)
                return;

            int index = Utils.ConvertToIndex(x, y, z);
            if (blockMap[index] == BlockIDs.AIR)
                blockMap[index] = blockID;
        }

        void GenerateTree(int x, int y, int z, Random rand, byte radius)
        {
            int height = rand.NextInt(4, 7);

            blockMap[Utils.ConvertToIndex(x, y - 1, z)] = BlockIDs.DIRT;
            for (int yy = y; yy < y + height; yy++)
                blockMap[Utils.ConvertToIndex(x, yy, z)] = BlockIDs.WOOD;

            bool flag = false;
            for (int yy = y + height - 2; yy <= y + height + 1; yy++)
            {
                for (int xx = x - radius; xx <= x + radius; xx++)
                    for (int zz = z - radius; zz <= z + radius; zz++)
                        PlaceBlock(xx, yy, zz, BlockIDs.LEAVES);

                if (flag)
                    radius -= 1;
                flag = !flag;
                if (radius < 0)
                    break;
            }
        }

        void GenerateCactus(int x, int y, int z, Random rand)
        {
            int height = rand.NextInt(2, 6);
            for (int yy = y; yy <= y + height; yy++)
            {
                if (blockMap[Utils.ConvertToIndex(x + 1, yy, z)] != BlockIDs.AIR ||
                    blockMap[Utils.ConvertToIndex(x - 1, yy, z)] != BlockIDs.AIR ||
                    blockMap[Utils.ConvertToIndex(x, yy, z + 1)] != BlockIDs.AIR ||
                    blockMap[Utils.ConvertToIndex(x, yy, z - 1)] != BlockIDs.AIR)
                    break;

                blockMap[Utils.ConvertToIndex(x, yy, z)] = BlockIDs.CACTUS;
            }
        }
    }
}