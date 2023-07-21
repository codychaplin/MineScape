using minescape.block;
using minescape.init;
using minescape.world.biome;
using minescape.world.chunk;
using minescape.world.generation;
using UnityEngine;

namespace minescape.world
{
    public class World : MonoBehaviour
    {
        public int Seed { get; set; }
        public Material textureMap;

        public BiomeManager biomeManager;
        public ChunkManager chunkManager;
        public ChunkGenerator chunkGenerator;

        void Start()
        {
            Random.InitState(Seed);
            biomeManager = new(Seed);
            chunkManager = new(this);
            chunkGenerator = new(this);
        }

        public Block GetBlock(Vector3 pos)
        {
            if (!IsBlockInWorld(pos))
                return Blocks.list[0];

            int x = Mathf.FloorToInt(pos.x);
            int y = Mathf.FloorToInt(pos.y);
            int z = Mathf.FloorToInt(pos.z);

            Chunk chunk = GetChunkFromBlockCoords(x, z);
            Vector3 localPos = new(x - (chunk.coord.x * Constants.ChunkWidth), y, z - (chunk.coord.z * Constants.ChunkWidth));
            return chunk.GetBlock(localPos);
        }

        public Chunk GetChunkFromBlockCoords(int x, int z)
        {
            ChunkCoord chunkCoord = new(x / Constants.ChunkWidth, z / Constants.ChunkWidth);
            return GetChunkFromChunkCoord(chunkCoord);
        }

        public Chunk GetChunkFromChunkCoord(ChunkCoord chunkCoord)
        {
            return chunkManager.GetChunk(chunkCoord);
        }

        bool IsBlockInWorld(Vector3 pos)
        {
            return pos.x >= 0 && pos.x < Constants.WorldSizeInBlocks &&
                   pos.y >= 0 && pos.y < Constants.ChunkHeight &&
                   pos.z >= 0 && pos.z < Constants.WorldSizeInBlocks;
        }
    }
}