using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using minescape.init;
using minescape.splines;
using minescape.world.chunk;

namespace minescape.world
{
    public class World : MonoBehaviour
    {
        public int Seed => 69;
        public RawImage image;

        public ChunkManager chunkManager;
        ChunkCoordComparer comparer = new();

        public ChunkCoord playerChunkCoord;
        public ChunkCoord playerLastChunkCoord;

        public Transform player;
        Vector3 spawnpoint;

        [System.NonSerialized] public Blocks Blocks;
        [System.NonSerialized] public Biomes Biomes;
        [System.NonSerialized] public Structures Structures;
        [System.NonSerialized] public Splines Splines;

        void Awake()
        {
            Blocks = new();
            Biomes = new();
            Structures = new();
            Splines = new();
        }

        void Start()
        {
            Application.targetFrameRate = 60;

            // initialize seed, player position, and classes
            UnityEngine.Random.InitState(Seed);

            // set spawn
            spawnpoint = new Vector3(Constants.WorldSizeInBlocks / 2f, 128f, Constants.WorldSizeInBlocks / 2f);
            player.position = spawnpoint;
        }

        void Update()
        {
            if (chunkManager.renderMap)
                return;

            playerChunkCoord = GetChunkCoord(player.position);
            if (!playerChunkCoord.Equals(playerLastChunkCoord))
                CheckViewDistance();
        }

        void OnApplicationQuit()
        {
            // release native collections from memory
            if (chunkManager != null && chunkManager.Chunks != null && chunkManager.Chunks.Count > 0)
                foreach (var chunk in chunkManager.Chunks.Values)
                {
                    chunk.Dispose();
                }

            if (chunkManager != null && chunkManager.MapChunks != null && chunkManager.MapChunks.Count > 0)
                foreach (var mapChunk in chunkManager.MapChunks)
                {
                    mapChunk.Dispose();
                }

            Splines.Elevation.Dispose();
            Biomes.biomes.Dispose();
            Blocks.blocks.Dispose();
            Structures.structures.Dispose();
        }

        public static bool IsBlockInWorld(int3 pos)
        {
            return pos.x >= 0 && pos.x < Constants.WorldSizeInBlocks &&
                   pos.y >= 0 && pos.y < Constants.ChunkHeight &&
                   pos.z >= 0 && pos.z < Constants.WorldSizeInBlocks;
        }

        public static bool IsChunkInWorld(int x, int z)
        {
            return x >= 0 && x < Constants.WorldSizeInChunks &&
                   z >= 0 && z < Constants.WorldSizeInChunks;
        }

        ChunkCoord GetChunkCoord(Vector3 pos)
        {
            int x = (int)math.floor(pos.x / Constants.ChunkWidth);
            int z = (int)math.floor(pos.z / Constants.ChunkWidth);
            return new ChunkCoord(x, z);
        }

        public Chunk GetChunk(ChunkCoord coord)
        {
            return chunkManager.TryGetChunk(coord);
        }

        public Chunk GetChunk(Vector3Int pos)
        {
            var coord = GetChunkCoord(pos);
            return chunkManager.TryGetChunk(coord);
        }

        void CheckViewDistance()
        {
            playerLastChunkCoord = playerChunkCoord;

            // cache activeChunks
            chunkManager.newActiveChunks.Clear();
            var chunkCoord = new ChunkCoord();
            for (int x = playerChunkCoord.x - Constants.ViewDistance; x < playerChunkCoord.x + Constants.ViewDistance; x++)
                for (int z = playerChunkCoord.z - Constants.ViewDistance; z < playerChunkCoord.z + Constants.ViewDistance; z++)
                {
                    // if chunk is out of bounds, skip
                    if (!IsChunkInWorld(x, z))
                        continue;

                    chunkCoord.x = x;
                    chunkCoord.z = z;

                    Chunk chunk = chunkManager.TryGetChunk(chunkCoord);

                    if (chunk == null) // if doesn't exist, add to queue
                        chunkManager.ChunksToCreate.Enqueue(chunkCoord);
                    else if (!chunk.isRendered && !chunk.isProcessing) // if not yet rendered and not currently processing, add to queue
                        chunkManager.ChunksToCreate.Enqueue(chunkCoord);
                    else if (chunk.isRendered && !chunk.IsActive) // if not active, activate
                        chunk.IsActive = true;

                    chunkManager.newActiveChunks.Add(chunkCoord);
                }

            // deactivate leftover chunks
            foreach (var coord in chunkManager.activeChunks)
                if (!chunkManager.newActiveChunks.Contains(coord, comparer))
                {
                    Chunk chunk = chunkManager.TryGetChunk(coord);
                    if (chunk != null)
                    {
                        // if rendered, disable chunk, else schedule it to disable
                        if (chunk.isRendered)
                            chunk.IsActive = false;
                        else
                            chunk.activate = false;
                    }
                }

            chunkManager.activeChunks.Clear();
            chunkManager.activeChunks.UnionWith(chunkManager.newActiveChunks);
        }
    }
}