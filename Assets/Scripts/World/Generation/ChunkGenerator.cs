using UnityEngine;
using minescape.init;
using minescape.world.chunk;
using System.Collections.Generic;

namespace minescape.world.generation
{
    public class ChunkGenerator
    {
        World world;

        public ChunkGenerator(World _world)
        {
            world = _world;
            if (world.renderChunks)
                GenerateChunks();
            if (world.renderMap)
                GenerateMap();
        }

        void GenerateMap()
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

        void GenerateChunks()
        {
            for (int x = 0; x < Constants.WorldSizeInChunks; x++)
                for (int z = 0; z < Constants.WorldSizeInChunks; z++)
                {
                    Chunk chunk = new(world, new ChunkCoord(x, z));
                    SetBlocksInChunk(ref chunk);
                    world.chunkManager.Chunks.Add(chunk);
                }

            foreach (var chunk in world.chunkManager.Chunks)
            {
                chunk.RenderChunk();
            }
        }

        void SetBlocksInChunk(ref Chunk chunk)
        {
            for (int x = 0; x < Constants.ChunkWidth; x++)
            {
                for (int z = 0; z < Constants.ChunkWidth; z++)
                {
                    var terrainHeight = Mathf.FloorToInt(128 * Noise.Get2DPerlin(new Vector2(chunk.Position.x + x, chunk.Position.z + z), 0, 0.25f)) + 16;
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
    }
}