using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using minescape.init;
using minescape.world.chunk;

namespace minescape.world.generation
{
    public class ChunkGenerator : MonoBehaviour
    {
        public World world;
        public bool renderMap;
        public bool renderChunks;

        public Queue<ChunkCoord> chunksToCreate = new();

        private void Start()
        {
            // generate chunks
            if (renderChunks)
                GenerateChunks();
            if (renderMap)
                GenerateMap();
        }

        void Update()
        {
            /*if (chunksToCreate.Count > 0)
                CreateChunks();*/
        }

        public Chunk CreateChunk(ChunkCoord coord)
        {
            Chunk chunk = new(world, coord);
            SetBlocksInChunk(chunk);
            world.chunkManager.Chunks.Add(chunk);
            return chunk;
        }

        public Chunk CreateChunkNow(ChunkCoord coord)
        {
            Chunk chunk = new(world, coord);
            var handle = SetBlocksInChunk(chunk);
            handle.Complete();
            world.chunkManager.Chunks.Add(chunk);
            return chunk;
        }

        void CreateChunks()
        {
            while (chunksToCreate.Count > 0)
            {
                Chunk chunk = world.chunkManager.GetChunk(chunksToCreate.Peek());
                chunk.RenderChunk();
                chunksToCreate.Dequeue();
                if (chunk.coord.x - world.playerChunkCoord.x >= Constants.ViewDistance ||
                    chunk.coord.z - world.playerChunkCoord.z >= Constants.ViewDistance)
                {
                    chunk.IsActive = false;
                }

            }
        }

        public void GenerateChunks()
        {
            var sw = Stopwatch.StartNew();
            long generateTotal = 0;
            long renderTotal = 0;

            // set up jobs
            int index = 0;
            int chunkCount = (Constants.ViewDistance * 2) * (Constants.ViewDistance * 2);
            NativeArray<JobHandle> handles = new(chunkCount, Allocator.TempJob);

            for (int x = Constants.HalfWorldSizeInChunks - Constants.ViewDistance; x < Constants.HalfWorldSizeInChunks + Constants.ViewDistance; x++)
                for (int z = Constants.HalfWorldSizeInChunks - Constants.ViewDistance; z < Constants.HalfWorldSizeInChunks + Constants.ViewDistance; z++)
                {
                    var start = sw.ElapsedMilliseconds;

                    Chunk chunk = new(world, new ChunkCoord(x, z));
                    var handle = SetBlocksInChunk(chunk);
                    handles[index++] = handle;

                    world.chunkManager.Chunks.Add(chunk);
                    world.chunkManager.activeChunks.Add(chunk.coord);

                    generateTotal += sw.ElapsedMilliseconds - start;
                }

            // complete jobs
            JobHandle.CompleteAll(handles);
            handles.Dispose();

            var chunks = world.chunkManager.Chunks.ToArray();
            foreach (var chunk in chunks)
            {
                var start = sw.ElapsedMilliseconds;
                chunk.RenderChunk();
                var total = sw.ElapsedMilliseconds - start;
                renderTotal += total;
            }

            Debug.Log($"Chunks generated on average: {generateTotal / world.chunkManager.Chunks.Count}ms");
            Debug.Log($"Chunks rendered on average: {renderTotal / world.chunkManager.Chunks.Count}ms");
        }

        JobHandle SetBlocksInChunk(Chunk chunk)
        {
            SetBlockDataJob jobData = new()
            {
                position = new int3(chunk.position.x, 0, chunk.position.z),
                map = chunk.BlockMap
            };
            
            return jobData.Schedule();
        }

        public struct SetBlockDataJob : IJob
        {
            [ReadOnly]
            public int3 position;
            [WriteOnly]
            public NativeArray<byte> map;

            public void Execute()
            {
                for (int x = 0; x < Constants.ChunkWidth; x++)
                {
                    for (int z = 0; z < Constants.ChunkWidth; z++)
                    {
                        var noise = Noise.Get2DPerlin(new float2(position.x + x, position.z + z), 0, 0.15f);
                        var terrainHeight = Mathf.FloorToInt(128 * noise + 32);
                        for (int y = 0; y < Constants.ChunkHeight; y++)
                        {
                            int index = x + z * Constants.ChunkWidth + y * Constants.ChunkHeight;
                            if (y == 0)
                                map[index] = Blocks.BEDROCK.ID;
                            else if (y <= terrainHeight)
                                map[index] = Blocks.STONE.ID;
                            else if (y > terrainHeight && y == Constants.WaterLevel)
                                map[index] = Blocks.WATER.ID;
                            else if (y > terrainHeight && y > Constants.WaterLevel)
                                break;
                        }
                    }
                }
            }
        }

        void ReplaceSurfaceBlocks(Chunk chunk)
        {

        }


        public void GenerateMap()
        {
            for (int x = 0; x < Constants.WorldSizeInChunks; x++)
                for (int z = 0; z < Constants.WorldSizeInChunks; z++)
                {
                    MapChunk chunk = new(new ChunkCoord(x, z));
                    SetBlocksInMapChunk(ref chunk);
                    world.chunkManager.MapChunks.Add(chunk);
                }

            ConvertMapToPng();
        }

        void SetBlocksInMapChunk(ref MapChunk chunk)
        {
            for (int x = 0; x < Constants.ChunkWidth; x++)
            {
                for (int z = 0; z < Constants.ChunkWidth; z++)
                {
                    var terrainHeight = Mathf.FloorToInt(128 * Noise.Get2DPerlin(new float2(chunk.position.x + x, chunk.position.y + z), 0, 0.5f)) + 16;
                    if (terrainHeight > Constants.WaterLevel)
                        chunk.SetBlock(x, z, Blocks.STONE.ID);
                    else
                        chunk.SetBlock(x, z, Blocks.WATER.ID);
                }
            }
        }

        void ConvertMapToPng()
        {
            Dictionary<byte, Color32> colours = new()
            {
                { 0, new Color32(255,255,255,255) }, // air
                { 1, new Color32(41,41,41,255) }, // bedrock
                { 2, new Color32(115,115,115,255) }, // stone
                { 3, new Color32(108,83,47,255) }, // dirt
                { 4, new Color32(66,104,47,255) }, // grass
                { 5, new Color32(227,213,142,255) }, // sand
                { 6, new Color32(80,172,220,255) } // water
            };

            Texture2D texture = new(Constants.WorldSizeInBlocks, Constants.WorldSizeInBlocks);

            for (int x = 0; x < Constants.WorldSizeInChunks; x++)
            {
                for (int y = 0; y < Constants.WorldSizeInChunks; y++)
                {
                    MapChunk mapChunk = world.chunkManager.GetMapChunk(new ChunkCoord(x, y));
                    int offsetX = x * Constants.ChunkWidth;
                    int offsetY = y * Constants.ChunkWidth;
                    for (int chunkX = 0; chunkX < Constants.ChunkWidth; chunkX++)
                        for (int chunkY = 0; chunkY < Constants.ChunkWidth; chunkY++)
                        {
                            var block = mapChunk.BlockMap[chunkX, chunkY];
                            texture.SetPixel(chunkX + offsetX, chunkY + offsetY, colours[block]);
                        }
                }
            }

            texture.Apply();
            world.image.texture = texture;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
        }
    }
}