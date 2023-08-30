using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using minescape.init;
using minescape.block;
using minescape.world;
using minescape.world.chunk;

namespace minescape.jobs
{
    public struct GenerateMeshDataJob : IJob
    {
        [ReadOnly] public ChunkCoord coord;

        [ReadOnly] public int3 position;

        [ReadOnly] public NativeHashMap<byte, Block> blocks;

        [ReadOnly] public NativeArray<byte> map;

        [ReadOnly] public NativeArray<bool> northFace;
        [ReadOnly] public NativeArray<bool> eastFace;
        [ReadOnly] public NativeArray<bool> southFace;
        [ReadOnly] public NativeArray<bool> westFace;

        [WriteOnly] public NativeList<float3> vertices;
        [WriteOnly] public NativeList<int> triangles;
        [WriteOnly] public NativeList<int> transparentTriangles;
        [WriteOnly] public NativeList<float2> uvs;
        [WriteOnly] public NativeList<float3> normals;

        public int vertexIndex;

        public void Execute()
        {
            vertices.Clear();
            triangles.Clear();
            transparentTriangles.Clear();
            uvs.Clear();
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
            if (!Chunk.IsBlockInChunk(pos.x, pos.y, pos.z))
            {
                if (pos.z >= Constants.ChunkWidth)
                {
                    int northIndex = pos.x + pos.y * Constants.ChunkWidth;
                    return northFace[northIndex];
                }
                else if (pos.z < 0)
                {
                    int southIndex = pos.x + pos.y * Constants.ChunkWidth;
                    return southFace[southIndex];
                }
                else if (pos.x >= Constants.ChunkWidth)
                {
                    int eastIndex = pos.z + pos.y * Constants.ChunkWidth;
                    return eastFace[eastIndex];
                }
                else if (pos.x < 0)
                {
                    int westIndex = pos.z + pos.y * Constants.ChunkWidth;
                    return westFace[westIndex];
                }
            }

            var adjBlockID = map[Chunk.ConvertToIndex(pos)];
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