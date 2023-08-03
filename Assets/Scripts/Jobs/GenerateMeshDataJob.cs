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
        [ReadOnly]
        public ChunkCoord coord;

        [ReadOnly]
        public int3 position;

        [ReadOnly]
        public NativeArray<byte> map;

        [ReadOnly]
        public NativeArray<byte> north;
        [ReadOnly]
        public NativeArray<byte> east;
        [ReadOnly]
        public NativeArray<byte> south;
        [ReadOnly]
        public NativeArray<byte> west;

        [WriteOnly]
        public NativeList<float3> vertices;
        [WriteOnly]
        public NativeList<int> triangles;
        [WriteOnly]
        public NativeList<float2> uvs;
        [WriteOnly]
        public NativeList<float3> normals;

        public int vertexIndex;

        public void Execute()
        {
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
            for (int i = 0; i < 6; i++)
            {
                if (blockID == Blocks.WATER.ID && i != 2) // only render top of water
                    continue;

                var adjacentIndex = pos + BlockData.faceCheck[i];
                if (!World.IsBlockInWorld(adjacentIndex + position)) // if out of world, skip
                    continue;

                var adjacentBlock = GetBlock(adjacentIndex);
                if (adjacentBlock != 0 && adjacentBlock != Blocks.WATER.ID) // if adjacent block is not air or water, skip
                    continue;

                float3 v0 = pos + BlockData.verts[BlockData.tris[i * 4 + 0]];
                float3 v1 = pos + BlockData.verts[BlockData.tris[i * 4 + 1]];
                float3 v2 = pos + BlockData.verts[BlockData.tris[i * 4 + 2]];
                float3 v3 = pos + BlockData.verts[BlockData.tris[i * 4 + 3]];
                vertices.Add(v0);
                vertices.Add(v1);
                vertices.Add(v2);
                vertices.Add(v3);

                float3 faceNormal = CalculateFaceNormal(v0, v1, v2);
                normals.Add(faceNormal);
                normals.Add(faceNormal);
                normals.Add(faceNormal);
                normals.Add(faceNormal);

                AddTexture(Blocks.blocks[blockID].Faces[i]);

                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);

                vertexIndex += 4;
            }
        }

        float3 CalculateFaceNormal(float3 v0, float3 v1, float3 v2)
        {
            float3 side1 = v1 - v0;
            float3 side2 = v2 - v0;
            return Vector3.Cross(side1, side2).normalized;
        }

        byte GetBlock(int3 pos)
        {
            if (!Chunk.IsBlockInChunk(pos.x, pos.y, pos.z))
            {
                if (pos.z >= Constants.ChunkWidth)
                {
                    int northIndex = Chunk.ConvertToIndex(pos.x, pos.y, 0);
                    return north[northIndex];
                }
                else if (pos.z < 0)
                {
                    int southIndex = Chunk.ConvertToIndex(pos.x, pos.y, 15);
                    return south[southIndex];
                }
                else if (pos.x >= Constants.ChunkWidth)
                {
                    int eastIndex = Chunk.ConvertToIndex(0, pos.y, pos.z);
                    return east[eastIndex];
                }
                else if (pos.x < 0)
                {
                    int westIndex = Chunk.ConvertToIndex(15, pos.y, pos.z);
                    return west[westIndex];
                }
            }

            int index = Chunk.ConvertToIndex(pos);
            return map[index];
        }

        void AddTexture(int textureId)
        {
            float y = textureId / BlockData.TextureAtlasSize;
            float x = textureId - (y * BlockData.TextureAtlasSize);

            x *= BlockData.NormalizedTextureSize;
            y *= BlockData.NormalizedTextureSize;

            uvs.Add(new float2(x, y));
            uvs.Add(new float2(x, y + BlockData.NormalizedTextureSize));
            uvs.Add(new float2(x + BlockData.NormalizedTextureSize, y));
            uvs.Add(new float2(x + BlockData.NormalizedTextureSize, y + BlockData.NormalizedTextureSize));
        }
    }
}