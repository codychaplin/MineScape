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
        public ChunkSection[] ChunkSections;
        public NativeArray<byte> BlockMap; // blocks in chunk
        public NativeArray<byte> LightMap; // Light levels in chunk
        public NativeArray<byte> BiomeMap; // biomes in chunk
        public NativeList<Structure> Structures; // structures in chunks

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
        Mesh mainMesh;
        Mesh colliderMesh;
        Mesh plantMesh;
        MeshCollider meshCollider;
        MeshCollider plantMeshCollider;

        // mesh data
        public NativeList<int> triangles;
        public NativeList<int> transparentTriangles;
        public NativeList<int> plantTriangles;
        public NativeList<float3> vertices;
        public NativeList<float3> normals;
        public NativeList<Color32> colours;
        public NativeList<UVData> uvData;
        public NativeList<float3> plantHitboxVertices;
        public NativeList<int> plantHitboxTriangles;

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

            ChunkSections = new ChunkSection[16]; // contains 16 16x16x16 arrays

            BlockMap = new(65536, Allocator.Persistent); // 65536 = 16x16x256 (x,z,y)
            LightMap = new(65536, Allocator.Persistent);
            BiomeMap = new(256, Allocator.Persistent); // 256 = 16x16 (x,z)

            Structures = new(Allocator.Persistent);

            isDirty = new(false, Allocator.Persistent);
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
        public JobHandle RenderChunk()
        {
            if (isDirty.Value)
                isRendered = false;

            if (isRendered)
                return default;

            if (!generated)
            {
                chunkObject = new() { layer = 6 }; // chunk layer
                mainMesh = new();
                mainMesh.MarkDynamic();
                colliderMesh = new();
                colliderMesh.MarkDynamic();
                plantMesh = new();
                plantMesh.MarkDynamic();
                meshFilter = chunkObject.AddComponent<MeshFilter>();
                meshRenderer = chunkObject.AddComponent<MeshRenderer>();
                meshCollider = chunkObject.AddComponent<MeshCollider>();
                plantMeshCollider = chunkObject.AddComponent<MeshCollider>();
                meshRenderer.materials = new Material[] { world.textureMap, world.transparentTextureMap, world.plants };
                chunkObject.transform.SetParent(world.transform);
                chunkObject.transform.position = position;
                chunkObject.name = $"{coord.x},{coord.z}";
                generated = true;
            }

            var handle = CreateMesh();
            isProcessing = false;
            isRendered = true;
            isDirty.Value = false;

            // triggered if out of view distance
            if (!activate)
            {
                IsActive = false;
                activate = true;
            }

            return handle;
        }

        public void InitializeMeshCollections()
        {
            if (!vertices.IsCreated) vertices = new(Allocator.Persistent);
            if (!normals.IsCreated) normals = new(Allocator.Persistent);
            if (!colours.IsCreated) colours = new(Allocator.Persistent);
            if (!uvData.IsCreated) uvData = new(Allocator.Persistent);
            if (!triangles.IsCreated) triangles = new(Allocator.Persistent);
            if (!transparentTriangles.IsCreated) transparentTriangles = new(Allocator.Persistent);
            if (!plantTriangles.IsCreated) plantTriangles = new(Allocator.Persistent);
            if (!plantHitboxVertices.IsCreated) plantHitboxVertices = new(Allocator.Persistent);
            if (!plantHitboxTriangles.IsCreated) plantHitboxTriangles = new(Allocator.Persistent);
        }

        /// <summary>
        /// Uses voxel data to create mesh.
        /// </summary>
        JobHandle CreateMesh()
        {
            // cache lengths
            int verticesLength = vertices.Length;
            int trianglesLength = triangles.Length;

            SetColliderMesh(verticesLength, trianglesLength);
            BakeMeshJob bakeColliderMeshJob = new() { meshID = colliderMesh.GetInstanceID() };
            var bakeColliderMeshHandle = bakeColliderMeshJob.Schedule();

            SetPlantColliderMesh();
            BakeMeshJob bakePlantColliderMeshJob = new() { meshID = plantMesh.GetInstanceID() };
            var bakePlantColliderMeshHandle = bakePlantColliderMeshJob.Schedule();

            SetMainMesh(verticesLength, trianglesLength);

            return JobHandle.CombineDependencies(bakeColliderMeshHandle, bakePlantColliderMeshHandle);
        }

        void SetColliderMesh(int verticesLength, int trianglesLength)
        {
            colliderMesh.Clear();

            // set vertex data
            colliderMesh.SetVertexBufferParams(verticesLength, new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0));
            colliderMesh.SetVertexBufferData(vertices.AsArray(), 0, 0, verticesLength);

            // set triangles
            colliderMesh.SetIndexBufferParams(trianglesLength, IndexFormat.UInt32);
            colliderMesh.SetIndexBufferData(triangles.AsArray(), 0, 0, trianglesLength);
            colliderMesh.subMeshCount = 1;
            colliderMesh.SetSubMesh(0, new SubMeshDescriptor(0, trianglesLength));
        }

        void SetPlantColliderMesh()
        {
            var plantVerticesLength = plantHitboxVertices.Length;
            var plantTrianglesLength = plantHitboxTriangles.Length;
            plantMesh.Clear();
            plantMesh.SetVertexBufferParams(plantVerticesLength, new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0));
            plantMesh.SetVertexBufferData(plantHitboxVertices.AsArray(), 0, 0, plantVerticesLength);
            plantMesh.SetIndexBufferParams(plantTrianglesLength, IndexFormat.UInt32);
            plantMesh.SetIndexBufferData(plantHitboxTriangles.AsArray(), 0, 0, plantTrianglesLength);
            plantMesh.subMeshCount = 1;
            plantMesh.SetSubMesh(0, new SubMeshDescriptor(0, plantTrianglesLength));
        }

        void SetMainMesh(int verticesLength, int trianglesLength)
        {
            // visible mesh
            mainMesh.Clear();

            // set vertex data
            mainMesh.SetVertexBufferParams(verticesLength, Constants.VertexAttributes);
            mainMesh.SetVertexBufferData(vertices.AsArray(), 0, 0, verticesLength, 0);
            mainMesh.SetVertexBufferData(normals.AsArray(), 0, 0, verticesLength, 1);
            mainMesh.SetVertexBufferData(colours.AsArray(), 0, 0, verticesLength, 2);
            mainMesh.SetVertexBufferData(uvData.AsArray(), 0, 0, verticesLength, 3);

            // set triangles
            int firstTwoTriCount = trianglesLength + transparentTriangles.Length;
            int totalTriCount = firstTwoTriCount + plantTriangles.Length;
            mainMesh.SetIndexBufferParams(totalTriCount, IndexFormat.UInt32);
            mainMesh.SetIndexBufferData(triangles.AsArray(), 0, 0, trianglesLength);
            mainMesh.SetIndexBufferData(transparentTriangles.AsArray(), 0, trianglesLength, transparentTriangles.Length);
            mainMesh.SetIndexBufferData(plantTriangles.AsArray(), 0, firstTwoTriCount, plantTriangles.Length);
            mainMesh.subMeshCount = 3;
            mainMesh.SetSubMesh(0, new SubMeshDescriptor(0, trianglesLength));
            mainMesh.SetSubMesh(1, new SubMeshDescriptor(trianglesLength, transparentTriangles.Length));
            mainMesh.SetSubMesh(2, new SubMeshDescriptor(firstTwoTriCount, plantTriangles.Length));

            mainMesh.bounds = Constants.ChunkBounds;
            meshFilter.mesh = mainMesh;
        }

        public void BakeCollider()
        {
            meshCollider.sharedMesh = colliderMesh;
            plantMeshCollider.sharedMesh = plantMesh;
            plantMeshCollider.excludeLayers = 1 << 3; // player
        }

        /// <summary>
        /// Disposes of native collections.
        /// </summary>
        public void Dispose()
        {
            if (BlockMap.IsCreated) BlockMap.Dispose();
            if (LightMap.IsCreated) LightMap.Dispose();
            if (BiomeMap.IsCreated) BiomeMap.Dispose();
            if (Structures.IsCreated) Structures.Dispose();

            foreach (var section in ChunkSections)
                if (section.blocks.IsCreated)
                    section.blocks.Dispose();

            if (isDirty.IsCreated) isDirty.Dispose();

            if (vertices.IsCreated) vertices.Dispose();
            if (normals.IsCreated) normals.Dispose();
            if (colours.IsCreated) colours.Dispose();
            if (uvData.IsCreated) uvData.Dispose();
            if (triangles.IsCreated) triangles.Dispose();
            if (transparentTriangles.IsCreated) transparentTriangles.Dispose();
            if (plantTriangles.IsCreated) plantTriangles.Dispose();
            if (plantHitboxVertices.IsCreated) plantHitboxVertices.Dispose();
            if (plantHitboxTriangles.IsCreated) plantHitboxTriangles.Dispose();
        }

        public override string ToString()
        {
            return $"Chunk: {coord.x},{coord.z}";
        }
    }
}