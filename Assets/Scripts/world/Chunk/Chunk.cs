using UnityEngine;
using UnityEngine.Rendering;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using minescape.jobs;
using minescape.structures;

namespace minescape.world.chunk
{
    public class Chunk
    {
        public World world;
        public Vector3Int position;

        public ChunkCoord coord; // coordinates of chunk
        public NativeArray<byte> BlockMap; // blocks in chunk
        public NativeArray<byte> LightMap; // Light levels in chunk
        public NativeArray<byte> BiomeMap; // biomes in chunk
        public NativeList<Structure> StructureMap; // structures in chunks

        // used to decide whether to regenerate chunk mesh
        public NativeReference<bool> isDirty; // for use in jobs
        public bool isRendered = false; // for use on main thread

        // syncronization utils
        public bool generated = false;
        public bool isProcessing = false;
        public bool activate = true;

        // game object and rendering properties
        GameObject chunkObject;
        public MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        Mesh mesh;

        // int x
        // 11111111 111111 11111 11111111 11111
        // light    normal x     y        z
        // 8        6      5     8        5
        // int y
        // 000000 11111 11111 11111 111111 11111
        //        uv.x  uv.y  r     g      b    
        // 6      5     5     5     6      5  
        public NativeList<int2> vertexData;
        public NativeList<int> triangles;

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
            LightMap = new(65536, Allocator.Persistent);
            BiomeMap = new(256, Allocator.Persistent); // 256 = 16x16 (x,z)

            StructureMap = new(Allocator.Persistent);

            isDirty = new(false, Allocator.Persistent);

            vertexData = new(Allocator.Persistent);
            triangles = new(Allocator.Persistent);
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
            int index = Utils.ConvertToIndex(x, y, z);
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
            int index = Utils.ConvertToIndex(x, y, z);
            return BlockMap[index];
        }

        /// <summary>
        /// Renders chunk using block data.
        /// </summary>
        public void RenderChunk()
        {
            if (isDirty.Value)
                isRendered = false;

            if (isRendered)
                return;

            if (!generated)
            {
                chunkObject = new() { layer = 6 }; // chunk layer
                mesh = new();
                mesh.MarkDynamic();
                meshFilter = chunkObject.AddComponent<MeshFilter>();
                meshRenderer = chunkObject.AddComponent<MeshRenderer>();
                meshRenderer.materials = new Material[] { world.textureMap, world.transparentTextureMap, world.plants };
                chunkObject.transform.SetParent(world.transform);
                chunkObject.transform.position = position;
                chunkObject.name = $"{coord.x},{coord.z}";
                generated = true;
            }

            CreateMesh();
            isProcessing = false;
            isRendered = true;
            isDirty.Value = false;

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
            int verticesLength = vertexData.Length;
            int trianglesLength = triangles.Length;
            mesh.Clear();
            mesh.SetVertexBufferParams(verticesLength, Constants.VertexAttributes);
            mesh.SetVertexBufferData(vertexData.AsArray(), 0, 0, verticesLength, 0);
            mesh.SetIndexBufferParams(trianglesLength, IndexFormat.UInt32);
            mesh.SetIndexBufferData(triangles.AsArray(), 0, 0, trianglesLength);
            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, trianglesLength), MeshUpdateFlags.DontRecalculateBounds);
            mesh.bounds = Constants.ChunkBounds;
            meshFilter.mesh = mesh;
        }

        /// <summary>
        /// Disposes of native collections.
        /// </summary>
        public void Dispose()
        {
            if (BlockMap.IsCreated) BlockMap.Dispose();
            if (LightMap.IsCreated) LightMap.Dispose();
            if (BiomeMap.IsCreated) BiomeMap.Dispose();
            if (StructureMap.IsCreated) StructureMap.Dispose();

            if (isDirty.IsCreated) isDirty.Dispose();

            if (vertexData.IsCreated) vertexData.Dispose();
            if (triangles.IsCreated) triangles.Dispose();
        }

        public override string ToString()
        {
            return $"Chunk: {coord.x},{coord.z}";
        }
    }
}