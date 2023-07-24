using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using minescape.init;
using minescape.block;
using minescape.world.biome;
using minescape.world.chunk;
using minescape.world.generation;
using System.Drawing;
using System.Linq;

namespace minescape.world
{
    public class World : MonoBehaviour
    {
        public int Seed => 696969;
        public Material textureMap;
        public RawImage image;
        public bool renderMap;
        public bool renderChunks;

        public BiomeManager biomeManager;
        public ChunkManager chunkManager;
        public ChunkGenerator chunkGenerator;
        
        public ChunkCoord playerChunkCoord;
        public ChunkCoord playerLastChunkCoord;

        public Transform player;
        [System.NonSerialized]
        public Vector3 spawnpoint;

        void Start()
        {
            // initialize seed, player position, and classes
            Random.InitState(Seed);
            playerLastChunkCoord = GetChunkCoord(player.position);
            biomeManager = new(Seed);
            chunkManager = new();
            chunkGenerator = new(this);

            // generate chunks
            if (renderChunks)
                chunkGenerator.GenerateChunks();
            if (renderMap)
                chunkGenerator.GenerateMap();

            // set spawn
            spawnpoint = new Vector3(Constants.WorldSizeInBlocks / 2f, 128f, Constants.WorldSizeInBlocks / 2f);
            player.position = spawnpoint;
        }

        void Update()
        {
            playerChunkCoord = GetChunkCoord(player.position);
            if (!playerChunkCoord.Equals(playerLastChunkCoord))
                CheckViewDistance();

            if (chunkGenerator.chunksToCreate.Count > 0 && !chunkGenerator.isCreatingChunks)
                StartCoroutine(chunkGenerator.CreateChunks());
        }

        public Block GetBlock(Vector3Int pos)
        {
            if (!IsBlockInWorld(pos))
                return Blocks.AIR;

            Chunk chunk = GetChunkFromBlockCoords(pos.x, pos.z);
            if (chunk == null)
            {
                var coord = new ChunkCoord(pos.x / Constants.ChunkWidth, pos.z / Constants.ChunkWidth);
                chunk = chunkGenerator.CreateChunk(coord);
            }

            Vector3Int localPos = new(pos.x - (chunk.coord.x * Constants.ChunkWidth), pos.y, pos.z - (chunk.coord.z * Constants.ChunkWidth));
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

        public bool IsBlockInWorld(Vector3Int pos)
        {
            return pos.x >= 0 && pos.x < Constants.WorldSizeInBlocks &&
                   pos.y >= 0 && pos.y < Constants.ChunkHeight &&
                   pos.z >= 0 && pos.z < Constants.WorldSizeInBlocks;
        }

        bool IsChunkInWorld(int x, int z)
        {
            return x >= 0 && x < Constants.WorldSizeInChunks &&
                   z >= 0 && z < Constants.WorldSizeInChunks;
        }

        ChunkCoord GetChunkCoord(Vector3 pos)
        {
            int x = Mathf.FloorToInt(pos.x / Constants.ChunkWidth);
            int z = Mathf.FloorToInt(pos.z / Constants.ChunkWidth);
            return new ChunkCoord(x, z);
        }

        void CheckViewDistance()
        {
            playerLastChunkCoord = playerChunkCoord;

            // cache activeChunks
            List<ChunkCoord> newActiveChunks = new();

            for (int x = playerChunkCoord.x - Constants.ViewDistance; x < playerChunkCoord.x + Constants.ViewDistance; x++)
                for (int z = playerChunkCoord.z - Constants.ViewDistance; z < playerChunkCoord.z + Constants.ViewDistance; z++)
                {
                    // if chunk is out of bounds, skip
                    if (!IsChunkInWorld(x, z))
                        continue;

                    var chunkCoord = new ChunkCoord(x, z);
                    Chunk chunk = chunkManager.GetChunk(chunkCoord);
                    //Debug.Log($"{chunkCoord.x},{chunkCoord.z}");
                    if (chunk == null) // if doesn't exist, generate one
                    {
                        chunkGenerator.CreateChunk(chunkCoord);
                        chunkGenerator.chunksToCreate.Add(chunkCoord);
                    }
                    else if (!chunk.isRenderd) // if not yet rendered, add to queue
                    {
                        chunkGenerator.chunksToCreate.Add(chunkCoord);
                    }
                    else if (!chunk.IsActive) // if not active, activate
                    {
                        chunk.IsActive = true;
                    }

                    newActiveChunks.Add(chunkCoord);
                }

            // disable leftover chunks
            chunkManager.activeChunks = newActiveChunks;
            var comparer = new ChunkCoordComparer();
            foreach (var chunk in chunkManager.Chunks)
                if (!chunkManager.activeChunks.Contains(chunk.coord, comparer))
                    if (chunk.isRenderd)
                        chunk.IsActive = false;

            // theres doubles somehow??
        }
    }
}