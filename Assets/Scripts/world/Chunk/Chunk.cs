using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using minescape.structures;

namespace minescape.world.chunk
{
    public class Chunk
    {
        public Material textureMap;
        public Material transparentTextureMap;
        public Transform worldTransform;

        public ChunkCoord coord; // coordinates of chunk
        public NativeArray<byte> BlockMap; // blocks in chunk
        public NativeArray<byte> BiomeMap; // biomes in chunk
        public NativeArray<byte> HeightMap; // terrain height in chunk
        public NativeList<Structure> Structures; // structures in chunks

        // used for rendering outside faces
        public NativeArray<bool> NorthFace;
        public NativeArray<bool> SouthFace;
        public NativeArray<bool> EastFace;
        public NativeArray<bool> WestFace;

        public bool generated = false;
        public bool isRendered = false;
        public bool isProcessing = false;
        public bool activate = true;

        GameObject chunkObject;
        public MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        MeshCollider meshCollider;

        public NativeList<float3> vertices;
        public NativeList<float3> normals;
        public NativeList<int> triangles;
        public NativeList<int> transparentTriangles;
        public NativeList<Color> colors;
        public NativeList<float2> uvs;
        
        public Vector3Int position;

        public bool IsActive
        {
            get { return chunkObject.activeSelf; }
            set { chunkObject.SetActive(value); }
        }

        public Chunk(Material _textureMap, Material _transparentTextureMap, Transform _worldTransform, ChunkCoord _coord)
        {
            textureMap = _textureMap;
            transparentTextureMap = _transparentTextureMap;
            worldTransform = _worldTransform;
            coord = _coord;
            position = new(coord.x * Constants.ChunkWidth, 0, coord.z * Constants.ChunkWidth);

            BlockMap = new(65536, Allocator.Persistent); // 65536 = 16x16x256 (x,z,y)
            BiomeMap = new(256, Allocator.Persistent); // 256 = 16x16 (x,z)
            HeightMap = new(256, Allocator.Persistent);
            Structures = new(Allocator.Persistent);

            NorthFace = new(4096, Allocator.Persistent); // 4096 = 16x256
            SouthFace = new(4096, Allocator.Persistent);
            EastFace = new(4096, Allocator.Persistent);
            WestFace = new(4096, Allocator.Persistent);

            vertices = new(2048, Allocator.Persistent); // 2048 = average length
            normals = new(2048, Allocator.Persistent);
            uvs = new(512, Allocator.Persistent);
            triangles = new(512, Allocator.Persistent);
            transparentTriangles = new(512, Allocator.Persistent);
            colors = new(Allocator.Persistent);
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
        /// Gets block ID in chunk.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns>Block ID</returns>
        public byte GetBlock(int x, int y, int z)
        {
            int index = ConvertToIndex(x, y, z);
            return BlockMap[index];
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
            if (x < 0 || x >= Constants.ChunkWidth ||
                y < 0 || y >= Constants.ChunkHeight ||
                z < 0 || z >= Constants.ChunkWidth)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Renders chunk using block data.
        /// </summary>
        public void RenderChunk()
        {
            if (isRendered)
                return;

            if (!generated)
            {
                chunkObject = new();
                chunkObject.layer = 6;
                meshFilter = chunkObject.AddComponent<MeshFilter>();
                meshRenderer = chunkObject.AddComponent<MeshRenderer>();
                meshCollider = chunkObject.AddComponent<MeshCollider>();
                meshRenderer.materials = new Material[] { textureMap, transparentTextureMap };
                chunkObject.transform.SetParent(worldTransform);
                chunkObject.transform.position = position;
                chunkObject.name = $"{coord.x},{coord.z}";
                generated = true;
            }

            CreateMesh();
            isProcessing = false;
            isRendered = true;

            // triggered if out of view distance
            if (!activate)
            {
                IsActive = false;
                activate = true;
            }
        }

        /// <summary>
        /// Uses voxel data to create mesh.
        /// </summary>
        void CreateMesh()
        {
            var vertArray = vertices.ToArray(Allocator.Temp);
            var normArray = normals.ToArray(Allocator.Temp);
            var uvsArray = uvs.ToArray(Allocator.Temp);

            Mesh mesh = new() { subMeshCount = 2 };
            mesh.SetVertices(vertArray);
            mesh.SetTriangles(triangles.ToArray(), 0);
            mesh.SetTriangles(transparentTriangles.ToArray(), 1);
            mesh.SetUVs(0, uvsArray);
            mesh.SetColors(colors.ToArray());
            mesh.SetNormals(normArray);

            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;
        }

        /// <summary>
        /// Disposes of native collections.
        /// </summary>
        public void Dispose()
        {
            BlockMap.Dispose();
            BiomeMap.Dispose();
            HeightMap.Dispose();
            Structures.Dispose();

            NorthFace.Dispose();
            SouthFace.Dispose();
            EastFace.Dispose();
            WestFace.Dispose();

            vertices.Dispose();
            normals.Dispose();
            triangles.Dispose();
            transparentTriangles.Dispose();
            colors.Dispose();
            uvs.Dispose();
        }

        public override string ToString()
        {
            return $"Chunk: {coord.x},{coord.z}";
        }
    }
}