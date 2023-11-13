using UnityEngine;
using Unity.Mathematics;

namespace minescape
{
    public static class Utils
    {
        /// <summary>
        /// Converts 2D index into 1D using x and z.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns>index</returns>
        public static int ConvertToIndex(int x, int z)
        {
            return x + z * Constants.ChunkWidth;
        }

        public static int ConvertToIndexLarge(int x, int z)
        {
            return x + z * 18;
        }

        /// <summary>
        /// Converts 3D index into 1D using x, y, and z.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns>index</returns>
        public static int ConvertToIndex(int x, int y, int z)
        {
            return x + z * Constants.ChunkWidth + y * Constants.ChunkHeight;
        }
        
        public static int ConvertToIndexLarge(int x, int y, int z)
        {
            return x + z * 18 + y * 18 * 18;
        }

        /// <summary>
        /// Converts Vector3Int into 1D index.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>index</returns>
        public static int ConvertToIndex(Vector3Int pos)
        {
            return pos.x + pos.z * Constants.ChunkWidth + pos.y * Constants.ChunkHeight;
        }

        /// <summary>
        /// Converts int3 into 1D index.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>index</returns>
        public static int ConvertToIndex(int3 pos)
        {
            return pos.x + pos.z * Constants.ChunkWidth + pos.y * Constants.ChunkHeight;
        }

        public static int ConvertToIndexLarge(int3 pos)
        {
            return pos.x + pos.z * 18 + pos.y * 18 * 18;
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
    }
}
