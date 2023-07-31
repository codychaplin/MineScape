using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
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
        public bool isProcessing = false;

        GameObject chunkObject;
        public MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        public NativeList<float3> vertices;
        public NativeList<float3> normals;
        public NativeList<int> triangles;
        public NativeList<float2> uvs;
        
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
            position = new(coord.x * Constants.ChunkWidth, 0, coord.z * Constants.ChunkWidth);
            BlockMap = new(65536, Allocator.Persistent); // 65536 = 16x16x256 (x,z,y)
            vertices = new(4096, Allocator.Persistent);
            normals = new(4096, Allocator.Persistent);
            triangles = new(4096, Allocator.Persistent);
            uvs = new(4096, Allocator.Persistent);
        }

        public static int ConvertToIndex(int x, int y, int z)
        {
            return x + z * Constants.ChunkWidth + y * Constants.ChunkHeight;
        }

        public static int ConvertToIndex(Vector3Int pos)
        {
            return pos.x + pos.z * Constants.ChunkWidth + pos.y * Constants.ChunkHeight;
        }

        public static int ConvertToIndex(int3 pos)
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
            int index = ConvertToIndex(x, y, z);
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

            int index = ConvertToIndex(pos);
            return Blocks.blocks[BlockMap[index]];
        }

        /// <summary>
        /// Checks if block is within the bounds of its chunk.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns>Whether block is within chunk.</returns>
        public static bool IsBlockInChunk(int x, int y, int z)
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

            CreateMesh();
            isProcessing = false;
            isRenderd = true;
        }

        /// <summary>
        /// Uses voxel data to create mesh.
        /// </summary>
        void CreateMesh()
        {
            var vertArray = vertices.ToArray(Allocator.Temp);
            var normArray = normals.ToArray(Allocator.Temp);
            var uvsArray = uvs.ToArray(Allocator.Temp);

            Mesh mesh = new();
            mesh.SetVertices(vertArray);
            mesh.SetTriangles(triangles.ToArray(), 0);
            mesh.SetUVs(0, uvsArray);
            mesh.SetNormals(normArray);

            meshFilter.mesh = mesh;
        }

        public void Dispose()
        {
            BlockMap.Dispose();
            vertices.Dispose();
            normals.Dispose();
            triangles.Dispose();
            uvs.Dispose();
        }

        public override string ToString()
        {
            return $"Chunk: {coord.x},{coord.z}";
        }
    }
}