using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using minescape.init;
using minescape.block;
using minescape.biome;
using minescape.world;
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
        
        public NativeList<int2> vertexData;
        public NativeList<int> triangles;

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

        public int vertexIndex;

        public void Execute()
        {
            SetBlockData();
            SetStructures();
            GenerateMeshData();
        }

        /*
         * Initializing block map
         */

        void SetBlockData()
        {
            seed *= 7919; // makes seed more unique

            for (int x = 0; x < Constants.ChunkWidth + 2; x++)
                for (int z = 0; z < Constants.ChunkWidth + 2; z++)
                {
                    var pos = new float2(position.x + x - 1, position.z + z - 1); // offset

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
                    int Index2D = Utils.ConvertToIndexLarge(x, z);
                    biomeMap[Index2D] = biomeID;
                    var biome = biomes[biomeID];

                    // set blocks
                    int index = 0;
                    for (int y = 0; y < Constants.ChunkHeight; y++)
                    {
                        index = Utils.ConvertToIndexLarge(x, y, z);
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

                        blockMap[index] = setBlock;

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
                    bool render = blockMap[Utils.ConvertToIndexLarge(x, terrainHeight, z)] != BlockIDs.AIR;
                    if (render && terrainHeight >= Constants.WaterLevel && biome.ID < BiomesIDs.BEACH) // not a beach/ocean biome
                    {
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
        }

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

        void SetStructures()
        {
            foreach (Structure structure in structureMap)
            {
                int x = structure.LocalPosition.x;
                int y = structure.LocalPosition.y;
                int z = structure.LocalPosition.z;
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

        void PlaceBlock(int x, int y, int z, byte blockID)
        {
            if (x < 0 || x >= Constants.ChunkWidth + 2 ||
                y < 0 || y >= Constants.ChunkHeight ||
                z < 0 || z >= Constants.ChunkWidth + 2)
                return;

            int index = Utils.ConvertToIndexLarge(x, y, z);
            if (blockMap[index] == BlockIDs.AIR)
                blockMap[index] = blockID;
        }

        void GenerateTree(int x, int y, int z, Random rand, byte radius)
        {
            int height = rand.NextInt(4, 7);

            blockMap[Utils.ConvertToIndexLarge(x, y - 1, z)] = BlockIDs.DIRT;
            for (int yy = y; yy < y + height; yy++)
                blockMap[Utils.ConvertToIndexLarge(x, yy, z)] = BlockIDs.WOOD;

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
                if (blockMap[Utils.ConvertToIndexLarge(x + 1, yy, z)] != BlockIDs.AIR ||
                    blockMap[Utils.ConvertToIndexLarge(x - 1, yy, z)] != BlockIDs.AIR ||
                    blockMap[Utils.ConvertToIndexLarge(x, yy, z + 1)] != BlockIDs.AIR ||
                    blockMap[Utils.ConvertToIndexLarge(x, yy, z - 1)] != BlockIDs.AIR)
                    break;

                blockMap[Utils.ConvertToIndexLarge(x, yy, z)] = BlockIDs.CACTUS;
            }
        }

        /*
         * Mesh data
         */

        void GenerateMeshData()
        {
            vertexData.Clear();
            triangles.Clear();

            int index = 0;
            int3 index3 = new(0, 0, 0);
            for (int x = 1; x <= Constants.ChunkWidth; x++)
                for (int z = 1; z <= Constants.ChunkWidth; z++)
                    for (int y = 0; y < Constants.ChunkHeight; y++)
                    {
                        index3.x = x;
                        index3.y = y;
                        index3.z = z;
                        index = Utils.ConvertToIndexLarge(x, y, z);
                        if (blockMap[index] != BlockIDs.AIR)
                            AddBlockToChunk(index3, index);
                    }
        }

        void AddBlockToChunk(int3 pos, int index)
        {
            var blockID = blockMap[index];
            var block = blocks[blockID];
            var biome = biomeMap[Utils.ConvertToIndexLarge(pos.x, pos.z)];
            AddBlock(pos, blockID, block.IsTransparent, biome, block.TintToBiome);
        }

        void AddBlock(int3 pos, byte blockID, bool isTransparent, byte biome, bool tintToBiome)
        {
            float3 pos1 = 0;
            float3 pos2 = 0;
            float3 pos3 = 0;
            float3 pos4 = 0;
            int normal = 0;
            ushort colour = 0;
            byte lightLevel = 0;
            float2 uv1 = 0;
            float2 uv2 = 0;
            float2 uv3 = 0;
            float2 uv4 = 0;
            int2 v1 = 0;
            int2 v2 = 0;
            int2 v3 = 0;
            int2 v4 = 0;

            for (int i = 0; i < 6; i++)
            {
                int3 direction = VoxelData.faceCheck[i];
                int3 adjacentPos = pos + direction;

                // if out of world, skip
                if (!World.IsBlockInWorld(adjacentPos + position))
                    continue;

                // if doesn't meet conditions, skip
                byte adjBlockID = blockMap[Utils.ConvertToIndexLarge(adjacentPos)];
                bool transparent = blocks[adjBlockID].IsTransparent;
                bool bothWater = blockID == BlockIDs.WATER && adjBlockID == BlockIDs.WATER;
                if (!transparent || bothWater)
                    continue;

                // vertices
                pos1 = pos + VoxelData.verts[VoxelData.tris[i * 4 + 0]] - 1;
                pos2 = pos + VoxelData.verts[VoxelData.tris[i * 4 + 1]] - 1;
                pos3 = pos + VoxelData.verts[VoxelData.tris[i * 4 + 2]] - 1;
                pos4 = pos + VoxelData.verts[VoxelData.tris[i * 4 + 3]] - 1;

                normal = VoxelData.packedNormals[i];
                colour = tintToBiome ? biomes[biome].GrassTint : ushort.MinValue;
                lightLevel = 255;

                // UVs
                byte textureId = blocks[blockID].GetFace(i);
                float y = textureId / Constants.TextureAtlasSize;
                float x = textureId - (y * Constants.TextureAtlasSize);
                x *= Constants.NormalizedTextureSize;
                y *= Constants.NormalizedTextureSize;
                uv1 = new float2(x, y) * 16;
                uv2 = new float2(x, y + Constants.NormalizedTextureSize) * 16;
                uv3 = new float2(x + Constants.NormalizedTextureSize, y) * 16;
                uv4 = new float2(x + Constants.NormalizedTextureSize, y + Constants.NormalizedTextureSize) * 16;

                // pack data
                v1.x = lightLevel << 24 | normal << 18 | (int)pos1.x << 13 | (int)pos1.y << 5 | (int)pos1.z;
                v1.y = (int)uv1.x << 21 | (int)uv1.y << 16 | colour;
                v2.x = lightLevel << 24 | normal << 18 | (int)pos2.x << 13 | (int)pos2.y << 5 | (int)pos2.z;
                v2.y = (int)uv2.x << 21 | (int)uv2.y << 16 | colour;
                v3.x = lightLevel << 24 | normal << 18 | (int)pos3.x << 13 | (int)pos3.y << 5 | (int)pos3.z;
                v3.y = (int)uv3.x << 21 | (int)uv3.y << 16 | colour;
                v4.x = lightLevel << 24 | normal << 18 | (int)pos4.x << 13 | (int)pos4.y << 5 | (int)pos4.z;
                v4.y = (int)uv4.x << 21 | (int)uv4.y << 16 | colour;

                vertexData.Add(v1);
                vertexData.Add(v2);
                vertexData.Add(v3);
                vertexData.Add(v4);

                // set triangles
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);

                vertexIndex += 4;
            }
        }
    }
}