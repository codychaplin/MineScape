using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace minescape.world.chunk
{
    public class ChunkManager
    {
        World world;
        public List<Chunk> Chunks = new();

        public ChunkManager(World _world)
        {
            world = _world;
        }

        public Chunk GetChunk(ChunkCoord chunkCoord)
        {
            var chunk = Chunks.FirstOrDefault(c => c.coord.Equals(chunkCoord));
            return chunk ?? throw new Exception("Empty Chunk");
        }
    }
}