using System.Collections.Generic;
using UnityEngine;
using block;

namespace world.chunk
{
    public class Chunk
    {
        World world; // world object
        ChunkCoord coord; // coordinates of chunk
        byte[,,] Map = new byte[Constants.ChunkWidth, Constants.ChunkHeight, Constants.ChunkWidth]; // x,y,z
        byte[,] Biomes = new byte[Constants.ChunkWidth, Constants.ChunkWidth]; // maps x,z coordinates to biomes

        GameObject chunkObject = null;
        MeshRenderer meshRenderer = null;
        MeshFilter meshFilter = null;
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
            position = new Vector3(coord.x * BlockData.ChunkWidth, 0f, coord.z * BlockData.ChunkWidth);

            PopulateMap();

            if (world.renderWorld)
            {
                CreateChunk();
                chunkObject = new();
                meshFilter = chunkObject.AddComponent<MeshFilter>();
                meshRenderer = chunkObject.AddComponent<MeshRenderer>();
                meshRenderer.material = world.material;
                chunkObject.transform.SetParent(world.transform);
                chunkObject.transform.position = new Vector3(coord.x * BlockData.ChunkWidth, 0f, coord.z * BlockData.ChunkWidth);
                chunkObject.name = $"{coord.x},{coord.z}";
                CreateMesh();
            }
        }

        /// <summary>
        /// Populates bool map with block types.
        /// </summary>
        void PopulateMap()
        {
            int terrainHeight;
            var pos = Vector3.zero;
            var pos2D = Vector2.zero;
            var maxHeight = 128;
            var minHeight = 32;
            var scale = 0.25f;
            for (int x = 0; x < BlockData.ChunkWidth; x++)
                for (int z = 0; z < BlockData.ChunkWidth; z++)
                {
                    pos.x = x;
                    pos.z = z;
                    pos += Position;
                    pos2D.x = pos.x;
                    pos2D.y = pos.z;
                    float noise = Noise.Get2DPerlin(pos2D, 0, scale);
                    terrainHeight = Mathf.FloorToInt(maxHeight * noise) + minHeight;
                    for (int y = 0; y < BlockData.ChunkHeight; y++)
                    {
                        pos.y = y;
                        var type = world.GetBlock(pos, terrainHeight);
                        Map[x, y, z] = type;
                    }
                }
        }

        /// <summary>
        /// Adds blocks to chunk.
        /// </summary>
        void CreateChunk()
        {
            for (int x = 0; x < BlockData.ChunkWidth; x++)
            {
                for (int z = 0; z < BlockData.ChunkWidth; z++)
                {
                    for (int y = 0; y < BlockData.ChunkHeight; y++)
                    {
                        if (world.BlockTypes[Map[x, y, z]].isSolid)
                            AddBlockToChunk(new Vector3(x, y, z));
                    }
                }
            }
        }

        /// <summary>
        /// Checks if voxel is within the bounds of its chunk.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns>Whether voxel is within chunk.</returns>
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
        /// Gets value from map.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>Whether block face should be rendered</returns>
        bool CheckBlock(Vector3 pos)
        {
            // round coordinates down
            int x = Mathf.FloorToInt(pos.x);
            int y = Mathf.FloorToInt(pos.y);
            int z = Mathf.FloorToInt(pos.z);

            // if out of bounds
            if (!IsBlockInChunk(x, y, z))
                return world.BlockTypes[world.GetBlock(pos + Position)].isSolid;

            // return value in map
            return world.BlockTypes[Map[x, y, z]].isSolid;
        }

        /// <summary>
        /// Generates exposed block faces.
        /// </summary>
        /// <param name="pos"></param>
        void AddBlockToChunk(Vector3 pos)
        {
            for (int i = 0; i < 6; i++)
            {
                if (CheckBlock(pos + BlockData.faceCheck[i]))
                    continue;

                vertices.Add(pos + BlockData.verts[BlockData.tris[i, 0]]);
                vertices.Add(pos + BlockData.verts[BlockData.tris[i, 1]]);
                vertices.Add(pos + BlockData.verts[BlockData.tris[i, 2]]);
                vertices.Add(pos + BlockData.verts[BlockData.tris[i, 3]]);

                byte blockId = Map[(int)pos.x, (int)pos.y, (int)pos.z];
                AddTexture(world.BlockTypes[blockId].GetTextureId(i));

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