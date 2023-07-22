using UnityEngine;
using minescape.init;
using minescape.block;

namespace minescape.world.chunk
{
    public class MapChunk
    {
        public ChunkCoord coord; // coordinates of chunk
        public byte[,] BlockMap = new byte[Constants.ChunkWidth, Constants.ChunkWidth]; // x,z coordinates for
        byte[,] Biomes = new byte[Constants.ChunkWidth, Constants.ChunkWidth]; // x,z coordinates for biomes

        public Vector2Int position;

        public MapChunk(ChunkCoord _coord)
        {
            coord = _coord;
            position = new Vector2Int(coord.x * Constants.ChunkWidth, coord.z * Constants.ChunkWidth);
        }

        /// <summary>
        /// Sets block ID in chunk.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <param name="block"></param>
        public void SetBlock(int x, int z, byte block)
        {
            BlockMap[x, z] = block;
        }

        /// <summary>
        /// Gets Block at coordinates in Chunk.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns>Block object at coordinates</returns>
        public Block GetBlock(int x, int z)
        {
            return Blocks.blocks[BlockMap[x, z]];
        }
    }
}