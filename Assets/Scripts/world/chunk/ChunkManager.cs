using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using minescape.jobs;
using minescape.lighting;
using minescape.scriptableobjects;

namespace minescape.world.chunk
{
    public class ChunkManager : MonoBehaviour
    {
        public World world;
        public bool renderChunks;
        public bool calculateLight;

        public NoiseParameters elevation;
        public NoiseParameters relief;
        public NoiseParameters temperature;
        public NoiseParameters humidity;
        public NoiseParameters caves;

        public Dictionary<ChunkCoord, Chunk> Chunks = new();

        public HashSet<ChunkCoord> activeChunks = new();
        public HashSet<ChunkCoord> newActiveChunks = new();

        public Queue<ChunkCoord> CreateChunksQueue = new();

        NativeQueue<FloodFillNode> BfsQueue;
        NativeQueue<FloodFillNode> BfsRemovalQueue;

        public List<MapChunk> MapChunks = new();

        private void Start()
        {
            BfsQueue = new(Allocator.Persistent);
            BfsRemovalQueue = new(Allocator.Persistent);
        }

        void Update()
        {
            if (CreateChunksQueue.Count > 0)
                GenerateChunks();
        }

        void GenerateChunks()
        {
            var coord = CreateChunksQueue.Peek();
            if (!Chunks.TryGetValue(coord, out var chunk))
            {
                chunk = new(world, coord);
                Chunks.Add(coord, chunk);
            }

            NativeArray<byte> blockMap = new(82944, Allocator.TempJob); // 18x256x18 (16x16 + 1 on sides)
            NativeArray<byte> biomeMap = new(324, Allocator.TempJob); // 18x18
            GenerateChunkJob job = new()
            {
                seed = world.Seed,
                position = new int3(chunk.position.x, 0, chunk.position.z),
                blockMap = blockMap,
                biomeMap = biomeMap,
                structureMap = chunk.StructureMap,
                vertexData = chunk.vertexData,
                triangles = chunk.triangles,
                blocks = world.Blocks.blocks,
                biomes = world.Biomes.biomes,
                structures = world.Structures.structures,
                elevation = world.Splines.Elevation,
                elevationScale = elevation.scale,
                elevationOctaves = elevation.octaves,
                reliefScale = relief.scale,
                reliefOctaves = relief.octaves,
                persistance = elevation.persistance,
                lacunarity = elevation.lacunarity,
                caveScale = caves.scale,
                caveOctaves = caves.octaves,
                caveThreshold = caves.lacunarity,
                vertexIndex = 0
            };
            var handle = job.Schedule();

            CreateChunksQueue.Dequeue();

            StartCoroutine(ApplyMeshDataChunk(handle, chunk, blockMap, biomeMap));
            return;
        }

        IEnumerator ApplyMeshDataChunk(JobHandle dependency, Chunk chunk, NativeArray<byte> blockMap, NativeArray<byte> biomeMap)
        {
            while (!dependency.IsCompleted)
            {
                yield return null;
            }
            dependency.Complete();

            chunk.RenderChunk();
            blockMap.Dispose();
            biomeMap.Dispose();
        }

        /// <summary>
        /// Tries to get chunk if it exists, otherwise returns null.
        /// </summary>
        /// <param name="chunkCoord"></param>
        /// <returns>Chunk or null</returns>
        public Chunk TryGetChunk(ChunkCoord chunkCoord)
        {
            return Chunks.GetValueOrDefault(chunkCoord);
        }


        public void Dispose()
        {
            BfsQueue.Dispose();
            BfsRemovalQueue.Dispose();
        }
    }
}