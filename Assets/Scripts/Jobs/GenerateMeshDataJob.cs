using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using minescape.init;
using minescape.block;
using minescape.world;
using minescape.world.chunk;

namespace minescape.jobs
{
    [BurstCompile]
    public struct GenerateMeshDataJob : IJob
    {
        [ReadOnly] public ChunkCoord coord;

        [ReadOnly] public int3 position;

        [ReadOnly] public NativeHashMap<byte, Block> blocks;

        [ReadOnly] public NativeArray<byte> map;
        [ReadOnly] public NativeArray<byte> lightMap;

        [ReadOnly] public NativeArray<byte> northChunk;
        [ReadOnly] public NativeArray<byte> eastChunk;
        [ReadOnly] public NativeArray<byte> southChunk;
        [ReadOnly] public NativeArray<byte> westChunk;

        [WriteOnly] public NativeList<float3> vertices;
        [WriteOnly] public NativeList<int> triangles;
        [WriteOnly] public NativeList<int> transparentTriangles;
        [WriteOnly] public NativeList<float2> uvs;
        [WriteOnly] public NativeList<float2> lightUvs;
        [WriteOnly] public NativeList<float3> normals;

        public int vertexIndex;

        public void Execute()
        {
            vertices.Clear();
            triangles.Clear();
            transparentTriangles.Clear();
            uvs.Clear();
            lightUvs.Clear();
            normals.Clear();

            int index = 0;
            int3 index3 = new(0, 0, 0);
            for (int x = 0; x < Constants.ChunkWidth; x++)
                for (int z = 0; z < Constants.ChunkWidth; z++)
                    for (int y = 0; y < Constants.ChunkHeight; y++)
                    {
                        index3.x = x;
                        index3.y = y;
                        index3.z = z;
                        index = Chunk.ConvertToIndex(index3);
                        if (map[index] != 0)
                            AddBlockToChunk(index3);
                    }
        }

        void AddBlockToChunk(int3 pos)
        {
            int index = Chunk.ConvertToIndex(pos);
            var blockID = map[index];
            bool isTransparent = blocks[blockID].IsTransparent;

            for (int i = 0; i < 6; i++)
            {
                int3 adjacentIndex = pos + VoxelData.faceCheck[i];
                if (!World.IsBlockInWorld(adjacentIndex + position)) // if out of world, skip
                    continue;

                var dontRender = DontRender(adjacentIndex, blockID);
                if (dontRender)
                    continue;

                var lightLevel = new float2(15f, 0f);
                if (Chunk.IsBlockInChunk(adjacentIndex.x, adjacentIndex.y, adjacentIndex.z))
                {
                    int adjIndex = Chunk.ConvertToIndex(adjacentIndex);
                    lightLevel.x = lightMap[adjIndex];
                    lightUvs.Add(lightLevel);
                    lightUvs.Add(lightLevel);
                    lightUvs.Add(lightLevel);
                    lightUvs.Add(lightLevel);
                }
                else
                {
                    lightUvs.Add(lightLevel);
                    lightUvs.Add(lightLevel);
                    lightUvs.Add(lightLevel);
                    lightUvs.Add(lightLevel);
                }

                float3 v0 = pos + VoxelData.verts[VoxelData.tris[i * 4 + 0]];
                float3 v1 = pos + VoxelData.verts[VoxelData.tris[i * 4 + 1]];
                float3 v2 = pos + VoxelData.verts[VoxelData.tris[i * 4 + 2]];
                float3 v3 = pos + VoxelData.verts[VoxelData.tris[i * 4 + 3]];
                vertices.Add(v0);
                vertices.Add(v1);
                vertices.Add(v2);
                vertices.Add(v3);

                float3 faceNormal = CalculateFaceNormal(v0, v1, v2);
                normals.Add(faceNormal);
                normals.Add(faceNormal);
                normals.Add(faceNormal);
                normals.Add(faceNormal);

                AddTexture(blocks[blockID].GetFace(i));

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

        float3 CalculateFaceNormal(float3 v0, float3 v1, float3 v2)
        {
            float3 side1 = v1 - v0;
            float3 side2 = v2 - v0;
            return Vector3.Cross(side1, side2).normalized;
        }

        bool DontRender(int3 pos, byte blockID)
        {
            byte adjBlockID = 0;
            if (!Chunk.IsBlockInChunk(pos.x, pos.y, pos.z))
            {
                if (pos.z >= Constants.ChunkWidth)
                {
                    int northIndex = Chunk.ConvertToIndex(pos.x, pos.y, 0);
                    adjBlockID = northChunk[northIndex];
                }
                else if (pos.z < 0)
                {
                    int southIndex = Chunk.ConvertToIndex(pos.x, pos.y, 15);
                    adjBlockID = southChunk[southIndex];
                }
                else if (pos.x >= Constants.ChunkWidth)
                {
                    int eastIndex = Chunk.ConvertToIndex(0, pos.y, pos.z);
                    adjBlockID = eastChunk[eastIndex];
                }
                else if (pos.x < 0)
                {
                    int westIndex = Chunk.ConvertToIndex(15, pos.y, pos.z);
                    adjBlockID = westChunk[westIndex];
                }
            }
            else
            {
                adjBlockID = map[Chunk.ConvertToIndex(pos)];
            }

            bool transparent = blocks[adjBlockID].IsTransparent;
            bool bothWater = blockID == BlockIDs.WATER && adjBlockID == BlockIDs.WATER;
            return !transparent || bothWater;
        }

        void AddTexture(int textureId)
        {
            float y = textureId / VoxelData.TextureAtlasSize;
            float x = textureId - (y * VoxelData.TextureAtlasSize);

            x *= VoxelData.NormalizedTextureSize;
            y *= VoxelData.NormalizedTextureSize;

            uvs.Add(new float2(x, y));
            uvs.Add(new float2(x, y + VoxelData.NormalizedTextureSize));
            uvs.Add(new float2(x + VoxelData.NormalizedTextureSize, y));
            uvs.Add(new float2(x + VoxelData.NormalizedTextureSize, y + VoxelData.NormalizedTextureSize));
        }
    }
}