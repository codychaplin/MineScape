using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using minescape.init;
using minescape.block;

namespace minescape.world.chunk
{
    public class Chunk
    {
        World world; // world object
        public ChunkCoord coord; // coordinates of chunk
        public NativeArray<byte> BlockMap;
        //byte[,] Biomes = new byte[Constants.ChunkWidth, Constants.ChunkWidth]; // x,z coordinates for biomes
        public bool isRenderd = false;

        GameObject chunkObject;
        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        int vertexIndex = 0;
        List<Vector3> vertices = new();
        //NativeList<Vector3> vertices1 = new();
        List<int> triangles = new();
        List<Vector2> uvs = new();

        public Vector3Int position;

        public bool IsActive
        {
            get { return chunkObject.activeSelf; }
            set { chunkObject.SetActive(value); }
        }

        public Chunk(World _world, ChunkCoord _coord)
        {
            world = _world;
            coord = _coord;
            position = new Vector3Int(coord.x * Constants.ChunkWidth, 0, coord.z * Constants.ChunkWidth);
            BlockMap = new NativeArray<byte>(65536, Allocator.Persistent); // 65536 = 16x16x256 (x,z,y)
        }

        /// <summary>
        /// Convert 3D coordinates to 1D.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns>Index</returns>
        int ConvertXYZToIndex(int x, int y, int z)
        {
            return x + z * Constants.ChunkWidth + y * Constants.ChunkHeight;
        }

        /// <summary>
        /// Converts Vector3Int to 1D.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>Index</returns>
        int ConvertVector3ToIndex(Vector3Int pos)
        {
            return pos.x + pos.z * Constants.ChunkWidth + pos.y * Constants.ChunkHeight;
        }

        /// <summary>
        /// Sets block ID in chunk.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="block"></param>
        public void SetBlock(int x, int y, int z, byte block)
        {
            int index = ConvertXYZToIndex(x, y, z);
            BlockMap[index] = block;
        }

        /// <summary>
        /// Gets Block at coordinates in Chunk.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>Block object at coordinates</returns>
        public Block GetBlock(Vector3Int pos)
        {
            if (!IsBlockInChunk(pos.x, pos.y, pos.z))
                return world.GetBlock(pos + position);

            int index = ConvertVector3ToIndex(pos);
            return Blocks.blocks[BlockMap[index]];
        }

        /// <summary>
        /// Checks if block is within the bounds of its chunk.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns>Whether block is within chunk.</returns>
        bool IsBlockInChunk(int x, int y, int z)
        {
            if (x < 0 || x >= BlockData.ChunkWidth ||
                y < 0 || y >= BlockData.ChunkHeight ||
                z < 0 || z >= BlockData.ChunkWidth)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Renders chunk using block data.
        /// </summary>
        public void RenderChunk()
        {
            if (isRenderd)
                return;

            chunkObject = new();
            meshFilter = chunkObject.AddComponent<MeshFilter>();
            meshRenderer = chunkObject.AddComponent<MeshRenderer>();
            meshRenderer.material = world.textureMap;
            chunkObject.transform.SetParent(world.transform);
            chunkObject.transform.position = position;
            chunkObject.name = $"{coord.x},{coord.z}";

            for (int x = 0; x < Constants.ChunkWidth; x++)
                for (int z = 0; z < Constants.ChunkWidth; z++)
                    for (int y = 0; y < Constants.ChunkHeight; y++)
                    {
                        int index = ConvertXYZToIndex(x, y, z);
                        if (Blocks.blocks[BlockMap[index]].IsSolid)
                            AddBlockToChunk(new Vector3Int(x, y, z));
                    }

            CreateMesh();
            isRenderd = true;
        }

        /// <summary>
        /// Generates exposed block faces.
        /// </summary>
        /// <param name="pos"></param>
        void AddBlockToChunk(Vector3Int pos)
        {
            int index = ConvertVector3ToIndex(pos);
            var blockID = BlockMap[index];
            for (int i = 0; i < 6; i++)
            {
                if (blockID == 6 && i != 2) // only render top of water
                    continue;

                var adjacentBlock = pos + BlockData.faceCheck[i];
                if (!world.IsBlockInWorld(adjacentBlock + position)) // if out of world, skip
                    continue;

                var clonk = GetBlock(adjacentBlock);
                if (!clonk.IsTransparent) // if adjacent block is not transparent, skip
                    continue;

                vertices.Add(pos + BlockData.verts[BlockData.tris[i, 0]]);
                vertices.Add(pos + BlockData.verts[BlockData.tris[i, 1]]);
                vertices.Add(pos + BlockData.verts[BlockData.tris[i, 2]]);
                vertices.Add(pos + BlockData.verts[BlockData.tris[i, 3]]);
                
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

        /// <summary>
        /// Adds texture data to uvs.
        /// </summary>
        /// <param name="textureId"></param>
        void AddTexture(int textureId)
        {
            float y = textureId / BlockData.TextureAtlasSize;
            float x = textureId - (y * BlockData.TextureAtlasSize);

            x *= BlockData.NormalizedTextureSize;
            y *= BlockData.NormalizedTextureSize;

            uvs.Add(new Vector2(x, y));
            uvs.Add(new Vector2(x, y + BlockData.NormalizedTextureSize));
            uvs.Add(new Vector2(x + BlockData.NormalizedTextureSize, y));
            uvs.Add(new Vector2(x + BlockData.NormalizedTextureSize, y + BlockData.NormalizedTextureSize));
        }

        /// <summary>
        /// Uses voxel data to create mesh.
        /// </summary>
        void CreateMesh()
        {
            Mesh mesh = new()
            {
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray(),
                uv = uvs.ToArray()
            };
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;
        }

        public override string ToString()
        {
            return $"Chunk: {coord.x},{coord.z}";
        }
    }
}