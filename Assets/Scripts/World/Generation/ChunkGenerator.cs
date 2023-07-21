using UnityEngine;
using minescape.init;
using minescape.world.chunk;

namespace minescape.world.generation
{
    public class ChunkGenerator
    {
        World world;

        public ChunkGenerator(World _world)
        {
            world = _world;
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
                        {
                            chunk.SetBlock(x, y, z, Blocks.list[1].ID);
                            continue;
                        }

                        if (y > terrainHeight && y <= Constants.WaterLevel)
                            chunk.SetBlock(x, y, z, Blocks.list[6].ID);
                        else if (y <= terrainHeight)
                            chunk.SetBlock(x, y, z, Blocks.list[2].ID);
                    }
                }
            }
        }

        void ReplaceSurfaceBlocks(Chunk chunk)
        {

        }
    }
}