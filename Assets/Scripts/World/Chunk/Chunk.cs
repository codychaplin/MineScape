using System.Collections.Generic;
using UnityEngine;
using minescape.block;
using minescape.init;
using static UnityEditor.PlayerSettings;

namespace minescape.world.chunk
{
    public class Chunk
    {
        World world; // world object
        public ChunkCoord coord; // coordinates of chunk
        byte[,,] BlockMap = new byte[Constants.ChunkWidth, Constants.ChunkHeight, Constants.ChunkWidth]; // x,y,z coordinates for
        byte[,] Biomes = new byte[Constants.ChunkWidth, Constants.ChunkWidth]; // x,z coordinates for biomes

        GameObject chunkObject;
        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        int vertexIndex = 0;
        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector2> uvs = new();

        Vector3 position;
        public Vector3 Position
        {
            get { return chunkObject == null ? position : chunkObject.transform.position; }
        }

        public bool IsActive
        {
            get { return chunkObject.activeSelf; }
            set { chunkObject.SetActive(value); }
        }

        public Chunk(World _world, ChunkCoord _coord)
        {
            world = _world;
            coord = _coord;
            position = new Vector3(coord.x * Constants.ChunkWidth, 0f, coord.z * Constants.ChunkWidth);
            
            chunkObject = new();
            meshFilter = chunkObject.AddComponent<MeshFilter>();
            meshRenderer = chunkObject.AddComponent<MeshRenderer>();
            meshRenderer.material = world.textureMap;
            chunkObject.transform.SetParent(world.transform);
            chunkObject.transform.position = new Vector3(coord.x * Constants.ChunkWidth, 0f, coord.z * Constants.ChunkWidth);
            chunkObject.name = $"{coord.x},{coord.z}";
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
            BlockMap[x, y, z] = block;
        }

        /// <summary>
        /// Gets Block at coordinates in Chunk.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>Block object at coordinates</returns>
        public Block GetBlock(Vector3 pos)
        {
            // round coordinates down
            int x = Mathf.FloorToInt(pos.x);
            int y = Mathf.FloorToInt(pos.y);
            int z = Mathf.FloorToInt(pos.z);

            // if out of bounds
            if (!IsBlockInChunk(x, y, z))
                return world.GetBlock(pos + Position);

            // return whether Block is solid
            return Blocks.list[BlockMap[x, y, z]];
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

        public void RenderChunk()
        {
            for (int x = 0; x < Constants.ChunkWidth; x++)
                for (int z = 0; z < Constants.ChunkWidth; z++)
                    for (int y = 0; y < Constants.ChunkHeight; y++)
                        if (Blocks.list[BlockMap[x, y, z]].IsSolid)
                            AddBlockToChunk(new Vector3(x, y, z));

            CreateMesh();
        }

        /// <summary>
        /// Generates exposed block faces.
        /// </summary>
        /// <param name="pos"></param>
        void AddBlockToChunk(Vector3 pos)
        {
            var blockID = BlockMap[(int)pos.x, (int)pos.y, (int)pos.z];
            for (int i = 0; i < 6; i++)
            {
                if (blockID == 6 && i != 2) // only render top of water
                    continue;

                var adjacentBlock = pos + BlockData.faceCheck[i];
                if (!world.IsBlockInWorld(adjacentBlock + position)) // if out of world, skip
                    continue;

                if (!GetBlock(adjacentBlock).IsTransparent) // if adjacent block is not transparent, skip
                    continue;

                vertices.Add(pos + BlockData.verts[BlockData.tris[i, 0]]);
                vertices.Add(pos + BlockData.verts[BlockData.tris[i, 1]]);
                vertices.Add(pos + BlockData.verts[BlockData.tris[i, 2]]);
                vertices.Add(pos + BlockData.verts[BlockData.tris[i, 3]]);
                
                AddTexture(Blocks.list[blockID].Faces[i]);

                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);

                vertexIndex += 4;
            }
        }

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
    }
}