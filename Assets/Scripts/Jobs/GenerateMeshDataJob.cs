using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using minescape.init;
using minescape.block;
using minescape.world;
using minescape.biomes;
using minescape.world.chunk;

namespace minescape.jobs
{
    [BurstCompile]
    public struct GenerateMeshDataJob : IJob
    {
        [ReadOnly] public ChunkCoord coord;

        [ReadOnly] public int3 position;

        [ReadOnly] public NativeHashMap<byte, Block> blocks;
        [ReadOnly] public NativeHashMap<byte, Biome> biomes;

        [ReadOnly] public NativeArray<byte> blockMap;
        [ReadOnly] public NativeArray<byte> lightMap;
        [ReadOnly] public NativeArray<byte> biomeMap;

        [ReadOnly] public NativeArray<byte> northBlockMap;
        [ReadOnly] public NativeArray<byte> eastBlockMap;
        [ReadOnly] public NativeArray<byte> southBlockMap;
        [ReadOnly] public NativeArray<byte> westBlockMap;

        [ReadOnly] public NativeArray<byte> northLightMap;
        [ReadOnly] public NativeArray<byte> eastLightMap;
        [ReadOnly] public NativeArray<byte> southLightMap;
        [ReadOnly] public NativeArray<byte> westLightMap;

        [WriteOnly] public NativeList<float3> vertices;
        [WriteOnly] public NativeList<int> triangles;
        [WriteOnly] public NativeList<int> transparentTriangles;
        [WriteOnly] public NativeList<int> plantTriangles;
        [WriteOnly] public NativeList<Color32> colours;
        [WriteOnly] public NativeList<float2> uvs;
        [WriteOnly] public NativeList<float2> lightUvs;
        [WriteOnly] public NativeList<float3> normals;
        [WriteOnly] public NativeList<float3> plantHitboxVertices;
        [WriteOnly] public NativeList<int> plantHitboxTriangles;

        [ReadOnly] public NativeReference<bool> isDirty;

        public int vertexIndex;
        public int hitboxVertexIndex;

        public void Execute()
        {
            if (!isDirty.Value)
                return;

            vertices.Clear();
            triangles.Clear();
            transparentTriangles.Clear();
            plantTriangles.Clear();
            colours.Clear();
            uvs.Clear();
            lightUvs.Clear();
            normals.Clear();
            plantHitboxVertices.Clear();
            plantHitboxTriangles.Clear();

            int index = 0;
            int3 index3 = new(0, 0, 0);
            for (int x = 0; x < Constants.ChunkWidth; x++)
                for (int z = 0; z < Constants.ChunkWidth; z++)
                    for (int y = 0; y < Constants.ChunkHeight; y++)
                    {
                        index3.x = x;
                        index3.y = y;
                        index3.z = z;
                        index = Utils.ConvertToIndex(index3);
                        if (blockMap[index] != BlockIDs.AIR)
                            AddBlockToChunk(index3);
                    }
        }

        void AddBlockToChunk(int3 pos)
        {
            int index = Utils.ConvertToIndex(pos);
            var blockID = blockMap[index];
            var block = blocks[blockID];
            var biome = biomeMap[Utils.ConvertToIndex(pos.x, pos.z)];

            if (!block.IsPlant)
                AddBlock(pos, blockID, block.IsTransparent, biome, block.TintToBiome);
            else
                AddPlant(pos, blockID, biome, block.TintToBiome);
        }

        void AddBlock(int3 pos, byte blockID, bool isTransparent, byte biome, bool tintToBiome)
        {
            for (int i = 0; i < 6; i++)
            {
                int3 direction = VoxelData.faceCheck[i];
                int3 adjacentIndex = pos + direction;

                // if out of world, skip
                if (!World.IsBlockInWorld(adjacentIndex + position))
                    continue;

                // if doesn't meet conditions, skip
                var dontRender = DontRender(adjacentIndex, blockID);
                if (dontRender)
                    continue;

                // sets sunlight level
                var lightLevel = new float2(15f, 0f);
                if (Utils.IsBlockInChunk(adjacentIndex.x, adjacentIndex.y, adjacentIndex.z))
                    lightLevel.x = lightMap[Utils.ConvertToIndex(adjacentIndex)];
                else
                    lightLevel.x = GetAdjacentLightLevel(adjacentIndex);
                lightUvs.Add(lightLevel);
                lightUvs.Add(lightLevel);
                lightUvs.Add(lightLevel);
                lightUvs.Add(lightLevel);

                if (tintToBiome)
                {
                    Color biomeColour = (Color)biomes[biome].GrassTint;
                    colours.Add(biomeColour);
                    colours.Add(biomeColour);
                    colours.Add(biomeColour);
                    colours.Add(biomeColour);
                }
                else
                {
                    colours.Add(Color.clear);
                    colours.Add(Color.clear);
                    colours.Add(Color.clear);
                    colours.Add(Color.clear);
                }

                // set vertices
                float3 v0 = pos + VoxelData.verts[VoxelData.tris[i * 4 + 0]];
                float3 v1 = pos + VoxelData.verts[VoxelData.tris[i * 4 + 1]];
                float3 v2 = pos + VoxelData.verts[VoxelData.tris[i * 4 + 2]];
                float3 v3 = pos + VoxelData.verts[VoxelData.tris[i * 4 + 3]];
                vertices.Add(v0);
                vertices.Add(v1);
                vertices.Add(v2);
                vertices.Add(v3);

                // set normals
                normals.Add(direction);
                normals.Add(direction);
                normals.Add(direction);
                normals.Add(direction);

                AddTexture(blocks[blockID].GetFace(i));

                // set triangles
                if (isTransparent)
                {
                    transparentTriangles.Add(vertexIndex);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 3);
                }
                else
                {
                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 3);
                }

                vertexIndex += 4;
            }
        }

        void AddPlant(int3 pos, byte blockID, byte biome, bool tintToBiome)
        {
            // visible mesh
            for (int i = 0; i < 2; i++)
            {
                // set vertices
                float3 v0 = pos + VoxelData.verts[VoxelData.plantTris[i * 4 + 0]];
                float3 v1 = pos + VoxelData.verts[VoxelData.plantTris[i * 4 + 1]];
                float3 v2 = pos + VoxelData.verts[VoxelData.plantTris[i * 4 + 2]];
                float3 v3 = pos + VoxelData.verts[VoxelData.plantTris[i * 4 + 3]];
                vertices.Add(v0);
                vertices.Add(v1);
                vertices.Add(v2);
                vertices.Add(v3);

                // set light level
                var lightLevel = new float2(15f, 0f);
                lightUvs.Add(lightLevel);
                lightUvs.Add(lightLevel);
                lightUvs.Add(lightLevel);
                lightUvs.Add(lightLevel);

                if (tintToBiome)
                {
                    Color biomeColour = (Color)biomes[biome].GrassTint;
                    colours.Add(biomeColour);
                    colours.Add(biomeColour);
                    colours.Add(biomeColour);
                    colours.Add(biomeColour);
                }
                else
                {
                    colours.Add(Color.clear);
                    colours.Add(Color.clear);
                    colours.Add(Color.clear);
                    colours.Add(Color.clear);
                }

                // set normals
                int3 normal = pos + VoxelData.plantFaceCheck[i];
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);

                AddTexture(blocks[blockID].GetFace(i));

                // set triangles
                plantTriangles.Add(vertexIndex);
                plantTriangles.Add(vertexIndex + 1);
                plantTriangles.Add(vertexIndex + 2);
                plantTriangles.Add(vertexIndex + 2);
                plantTriangles.Add(vertexIndex + 1);
                plantTriangles.Add(vertexIndex + 3);

                vertexIndex += 4;
            }

            // hitbox mesh
            for (int i = 0;i < 6; i++)
            {
                // set vertices
                float3 v0 = pos + VoxelData.verts[VoxelData.tris[i * 4 + 0]];
                float3 v1 = pos + VoxelData.verts[VoxelData.tris[i * 4 + 1]];
                float3 v2 = pos + VoxelData.verts[VoxelData.tris[i * 4 + 2]];
                float3 v3 = pos + VoxelData.verts[VoxelData.tris[i * 4 + 3]];
                plantHitboxVertices.Add(v0);
                plantHitboxVertices.Add(v1);
                plantHitboxVertices.Add(v2);
                plantHitboxVertices.Add(v3);

                // set triangles
                plantHitboxTriangles.Add(hitboxVertexIndex);
                plantHitboxTriangles.Add(hitboxVertexIndex + 1);
                plantHitboxTriangles.Add(hitboxVertexIndex + 2);
                plantHitboxTriangles.Add(hitboxVertexIndex + 2);
                plantHitboxTriangles.Add(hitboxVertexIndex + 1);
                plantHitboxTriangles.Add(hitboxVertexIndex + 3);

                hitboxVertexIndex += 4;
            }
        }

        bool DontRender(int3 pos, byte blockID)
        {
            byte adjBlockID = 0;

            // if outside chunk, get block from adjacent chunk
            if (!Utils.IsBlockInChunk(pos.x, pos.y, pos.z))
            {
                if (pos.z >= Constants.ChunkWidth)
                {
                    int northIndex = Utils.ConvertToIndex(pos.x, pos.y, 0);
                    adjBlockID = northBlockMap[northIndex];
                }
                else if (pos.z < 0)
                {
                    int southIndex = Utils.ConvertToIndex(pos.x, pos.y, 15);
                    adjBlockID = southBlockMap[southIndex];
                }
                else if (pos.x >= Constants.ChunkWidth)
                {
                    int eastIndex = Utils.ConvertToIndex(0, pos.y, pos.z);
                    adjBlockID = eastBlockMap[eastIndex];
                }
                else if (pos.x < 0)
                {
                    int westIndex = Utils.ConvertToIndex(15, pos.y, pos.z);
                    adjBlockID = westBlockMap[westIndex];
                }
            }
            else // inside chunk, get block
            {
                adjBlockID = blockMap[Utils.ConvertToIndex(pos)];
            }

            bool transparent = blocks[adjBlockID].IsTransparent;
            bool bothWater = blockID == BlockIDs.WATER && adjBlockID == BlockIDs.WATER;
            return !transparent || bothWater;
        }

        byte GetAdjacentLightLevel(int3 pos)
        {
            // if inside chunk, get light level
            if (Utils.IsBlockInChunk(pos.x, pos.y, pos.z))
                return lightMap[Utils.ConvertToIndex(pos)];

            // if outside chunk, get light level from adjacent chunk
            if (pos.z >= Constants.ChunkWidth)
            {
                int northIndex = Utils.ConvertToIndex(pos.x, pos.y, 0);
                return northLightMap[northIndex];
            }
            else if (pos.z < 0)
            {
                int southIndex = Utils.ConvertToIndex(pos.x, pos.y, 15);
                return southLightMap[southIndex];
            }
            else if (pos.x >= Constants.ChunkWidth)
            {
                int eastIndex = Utils.ConvertToIndex(0, pos.y, pos.z);
                return eastLightMap[eastIndex];
            }
            else if (pos.x < 0)
            {
                int westIndex = Utils.ConvertToIndex(15, pos.y, pos.z);
                return westLightMap[westIndex];
            }

            return 0;

        }

        void AddTexture(int textureId)
        {
            // get normalized texture coordinates
            float y = textureId / Constants.TextureAtlasSize;
            float x = textureId - (y * Constants.TextureAtlasSize);
            x *= Constants.NormalizedTextureSize;
            y *= Constants.NormalizedTextureSize;

            // set uvs
            uvs.Add(new float2(x, y));
            uvs.Add(new float2(x, y + Constants.NormalizedTextureSize));
            uvs.Add(new float2(x + Constants.NormalizedTextureSize, y));
            uvs.Add(new float2(x + Constants.NormalizedTextureSize, y + Constants.NormalizedTextureSize));
        }
    }
}