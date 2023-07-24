using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using minescape.init;
using minescape.world.chunk;

namespace minescape.world.generation
{
    public class ChunkGenerator
    {
        World world;

        public List<ChunkCoord> chunksToCreate = new();
        public bool isCreatingChunks;

        public ChunkGenerator(World _world)
        {
            world = _world;
        }

        public Chunk CreateChunk(ChunkCoord coord)
        {
            Chunk chunk = new(world, coord);
            SetBlocksInChunk(chunk);
            world.chunkManager.Chunks.Add(chunk);
            return chunk;
        }

        public IEnumerator CreateChunks()
        {
            isCreatingChunks = true;

            while (chunksToCreate.Count > 0)
            {
                Chunk chunk = world.chunkManager.GetChunk(chunksToCreate[0]);
                chunk.RenderChunk();
                chunksToCreate.RemoveAt(0);
                if (chunk.coord.x - world.playerChunkCoord.x >= Constants.ViewDistance ||
                    chunk.coord.z - world.playerChunkCoord.z >= Constants.ViewDistance)
                {
                    chunk.IsActive = false;
                }
                yield return null;

            }

            isCreatingChunks = false;
        }

        public void GenerateChunks()
        {
            var sw = Stopwatch.StartNew();
            long generateTotal = 0;
            long renderTotal = 0;
            for (int x = Constants.HalfWorldSizeInChunks - Constants.ViewDistance; x < Constants.HalfWorldSizeInChunks + Constants.ViewDistance; x++)
                for (int z = Constants.HalfWorldSizeInChunks - Constants.ViewDistance; z < Constants.HalfWorldSizeInChunks + Constants.ViewDistance; z++)
                {
                    var start = sw.ElapsedMilliseconds;
                    Chunk chunk = new(world, new ChunkCoord(x, z));
                    SetBlocksInChunk(chunk);
                    world.chunkManager.Chunks.Add(chunk);
                    world.chunkManager.activeChunks.Add(chunk.coord);
                    generateTotal += sw.ElapsedMilliseconds - start;
                }

            var chunks = world.chunkManager.Chunks.ToArray();
            foreach (var chunk in chunks)
            {
                var start = sw.ElapsedMilliseconds;
                chunk.RenderChunk();
                var total = sw.ElapsedMilliseconds - start;
                renderTotal += total;
            }

            UnityEngine.Debug.Log($"Chunks generated on average: {generateTotal / world.chunkManager.Chunks.Count}ms");
            UnityEngine.Debug.Log($"Chunks rendered on average: {renderTotal / world.chunkManager.Chunks.Count}ms");
        }

        void SetBlocksInChunk(Chunk chunk)
        {
            for (int x = 0; x < Constants.ChunkWidth; x++)
            {
                for (int z = 0; z < Constants.ChunkWidth; z++)
                {
                    var noise = Noise.Get2DPerlin(new Vector2(chunk.position.x + x, chunk.position.z + z), 0, 0.2f);
                    var terrainHeight = Mathf.FloorToInt(128 * noise + 16);
                    for (int y = 0; y < Constants.ChunkHeight; y++)
                    {
                        if (y == 0)
                            chunk.SetBlock(x, y, z, Blocks.BEDROCK.ID);
                        else if (y <= terrainHeight)
                            chunk.SetBlock(x, y, z, Blocks.STONE.ID);
                        else if (y > terrainHeight && y == Constants.WaterLevel)
                            chunk.SetBlock(x, y, z, Blocks.WATER.ID);
                        else if (y > terrainHeight && y > Constants.WaterLevel)
                            break;
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
                    var terrainHeight = Mathf.FloorToInt(128 * Noise.Get2DPerlin(new Vector2(chunk.position.x + x, chunk.position.y + z), 0, 0.5f)) + 16;
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