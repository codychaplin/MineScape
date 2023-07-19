using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using block;
using world.chunk;

namespace world
{
    public class World : MonoBehaviour
    {
        public int seed;
        public bool renderWorld;
        public RawImage image;

        public Transform player;
        [NonSerialized]
        public Vector3 spawnpoint;
        public Material material;
        public BlockType[] BlockTypes;

        Chunk[,] chunks = new Chunk[BlockData.WorldSizeInChunks, BlockData.WorldSizeInChunks];

        List<ChunkCoord> activeChunks = new();
        ChunkCoord playerChunkCoord;
        ChunkCoord playerLastChunkCoord;

        void Start()
        {
            UnityEngine.Random.InitState(seed);
            /*GenerateWorld();
            ConvertMapToPng();

            spawnpoint = new Vector3(BlockData.WorldSizeInBlocks / 2, BlockData.ChunkHeight + 2, BlockData.WorldSizeInBlocks / 2);
            player.position = spawnpoint;
            playerLastChunkCoord = GetChunkCoord(player.position);*/
        }

        /*void Update()
        {
            playerChunkCoord = GetChunkCoord(player.position);
            if (!playerChunkCoord.Equals(playerLastChunkCoord))
                CheckViewDistance();
        }*/

        void GenerateWorld()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            float total = 0;
            for (int x = 0; x < BlockData.WorldSizeInChunks; x++)
                for (int z = 0; z < BlockData.WorldSizeInChunks; z++)
                {
                    long start = sw.ElapsedMilliseconds;
                    ChunkCoord chunkCoord = new(x, z);
                    CreateChunk(chunkCoord);
                    total += sw.ElapsedMilliseconds - start;
                }
            total /= BlockData.WorldSizeInChunks * BlockData.WorldSizeInChunks;
            Debug.Log($"CreateChunk() took an average of: {Math.Round(total, 3)}ms");
            Debug.Log($"GenerateWorld() took a total of:  {sw.ElapsedMilliseconds}ms");
        }

        void CreateChunk(ChunkCoord chunkCoord)
        {
            chunks[chunkCoord.x, chunkCoord.z] = new Chunk(this, chunkCoord);
            if (renderWorld)
                activeChunks.Add(chunkCoord);
        }


        public byte GetBlock(Vector3 pos, int terrainHeight)
        {
            int y = Mathf.FloorToInt(pos.y);

            if (!IsBlockInWorld(pos))
                return 0; // air

            if (y == 0)
                return 1; // bedrock

            if (y == terrainHeight)
                return 4; // grass
            else if (y > terrainHeight && y <= 32)
                return 5; // water
            else if (y < terrainHeight && y >= terrainHeight - 4)
                return 3; // dirt
            else if (y < terrainHeight)
                return 2; // stone
            else
                return 0; // air
        }
        public byte GetBlock(Vector3 pos)
        {
            int y = Mathf.FloorToInt(pos.y);

            if (!IsBlockInWorld(pos))
                return 0; // air

            if (y == 0)
                return 1; // bedrock

            int terrainHeight = Mathf.FloorToInt(128 * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, 0.25f)) + 32;
            if (y == terrainHeight)
                return 4; // grass
            else if (y > terrainHeight && y <= 64)
                return 5; // water
            else if (y < terrainHeight && y >= terrainHeight - 4)
                return 3; // dirt
            else if (y < terrainHeight)
                return 2; // stone
            else
                return 0; // air
        }

        void ConvertMapToPng()
        {
            /*Dictionary<byte, Color32> colours = new()
            {
                { 0, new Color32(255,255,255,255) }, // air
                { 1, new Color32(41,41,41,255) }, // bedrock
                { 2, new Color32(115,115,115,255) }, // stone
                { 3, new Color32(108,83,47,255) }, // dirt
                { 4, new Color32(66,104,47,255) }, // grass
                { 5, new Color32(80,172,220,255) } // water
            };

            Texture2D texture = new(Block.WorldSizeInBlocks, Block.WorldSizeInBlocks);

            for (int x = 0; x < Block.WorldSizeInChunks; x++)
            {
                for (int y = 0; y < Block.WorldSizeInChunks; y++)
                {
                    int offsetX = x * Block.ChunkWidth;
                    int offsetY = y * Block.ChunkWidth;
                    for (int chunkX = 0 + offsetX; chunkX < Block.ChunkWidth + offsetX; chunkX++)
                    {
                        for (int chunkY = 0 + offsetY; chunkY < Block.ChunkWidth + offsetY; chunkY++)
                        {
                            var block = chunks[x, y].Map2D[chunkX - offsetX, chunkY - offsetY];
                            Color32 tintedColour;
                            if (block.type != 5)
                                tintedColour = Color32.Lerp(colours[block.type], Color.white, (float)block.height / Block.ChunkHeight);
                            else
                                tintedColour = colours[block.type];
                            texture.SetPixel(chunkX, chunkY, tintedColour);
                        }
                    }
                }
            }

            texture.Apply();
            image.texture = texture;
            SplitTexture(texture, 512);*/

        }

        void SplitTexture(Texture2D texture, int chunkSize)
        {
            string path = "Assets/Textures/Map/";
            byte[] bytes = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes($"{path}full.png", bytes);

            int count = (BlockData.WorldSizeInBlocks / chunkSize) * 2;
            for (int i = 0; i < count; i++)
            {
                int offsetX = (i % 2) * chunkSize;
                int offsetY = (i / 2) * chunkSize;

                var pixels = texture.GetPixels(offsetX, offsetY, chunkSize, chunkSize);
                Texture2D splitTexture = new(chunkSize, chunkSize);
                splitTexture.SetPixels(pixels);
                splitTexture.Apply();

                byte[] splitBytes = splitTexture.EncodeToPNG();
                System.IO.File.WriteAllBytes($"{path}{offsetX},{offsetY}.png", splitBytes);
            }
        }


        void CheckViewDistance()
        {
            ChunkCoord coord = GetChunkCoord(player.position);

            // cache then clear activeChunks
            List<ChunkCoord> previouslyActiveChunks = new(activeChunks);
            //activeChunks.Clear();

            for (int x = coord.x - BlockData.ViewDistance; x <= coord.x + BlockData.ViewDistance; x++)
                for (int z = coord.z - BlockData.ViewDistance; z <= coord.z + BlockData.ViewDistance; z++)
                {
                    // if chunk is out of bounds, skip
                    if (!IsChunkInWorld(x, z))
                        continue;

                    var chunkCoord = new ChunkCoord(x, z);
                    if (chunks[x, z] == null) // if doesn't exist, create one
                    {
                        CreateChunk(chunkCoord);
                    }
                    else if (!chunks[x, z].IsActive) // if not active, activate and add to activeChunks
                    {
                        chunks[x, z].IsActive = true;
                        activeChunks.Add(chunkCoord);
                    }

                    // remove active chunks from previouslyActiveChunks
                    for (int i = 0; i < previouslyActiveChunks.Count; i++)
                    {
                        if (previouslyActiveChunks[i].Equals(chunkCoord))
                            previouslyActiveChunks.RemoveAt(i);
                    }
                }

            // disable leftover chunks in previouslyActiveChunks
            foreach (var chunk in previouslyActiveChunks)
                chunks[chunk.x, chunk.z].IsActive = false;
        }

        bool IsBlockInWorld(Vector3 pos)
        {
            return pos.x >= 0 && pos.x < BlockData.WorldSizeInBlocks &&
                   pos.y >= 0 && pos.y < BlockData.ChunkHeight &&
                   pos.z >= 0 && pos.z < BlockData.WorldSizeInBlocks;
        }

        bool IsChunkInWorld(int x, int z)
        {
            return x >= 0 && x < BlockData.WorldSizeInChunks &&
                   z >= 0 && z < BlockData.WorldSizeInChunks;
        }

        ChunkCoord GetChunkCoord(Vector3 pos)
        {
            int x = Mathf.FloorToInt(pos.x / BlockData.ChunkWidth);
            int z = Mathf.FloorToInt(pos.z / BlockData.ChunkWidth);
            return new ChunkCoord(x, z);
        }
    }
}