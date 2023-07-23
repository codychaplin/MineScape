using System.Linq;
using System.Collections.Generic;

namespace minescape.world.chunk
{
    public class ChunkManager
    {
        World world;
        public List<ChunkCoord> activeChunks = new();
        public List<Chunk> Chunks = new();

        public List<MapChunk> MapChunks = new();

        public ChunkManager(World _world)
        {
            world = _world;
        }

        public Chunk GetChunk(ChunkCoord chunkCoord)
        {
            return Chunks.FirstOrDefault(c => c.coord.Equals(chunkCoord));
        }

        public MapChunk GetMapChunk(ChunkCoord chunkCoord)
        {
            return MapChunks.FirstOrDefault(c => c.coord.Equals(chunkCoord));
        }
    }
}