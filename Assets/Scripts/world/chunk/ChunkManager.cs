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
                NativeArray<byte> blockMap = new(82944, Allocator.TempJob); // 18x256x18 (16x16 + 1 on sides)
            }

            CreateChunksQueue.Dequeue();
            return;
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