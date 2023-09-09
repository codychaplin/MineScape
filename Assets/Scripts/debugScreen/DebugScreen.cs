using TMPro;
using UnityEngine;
using minescape.world;
using Unity.Mathematics;
using minescape.world.chunk;
using System;

namespace minescape.debugScreen
{
    public class DebugScreen : MonoBehaviour
    {
        public World world;
        public TextMeshProUGUI positionText;
        int x;
        int y;
        int z;
        public TextMeshProUGUI chunkCoordText;
        ChunkCoord chunkCoord;
        public TextMeshProUGUI LightLevelText;
        byte lightLevel;
        Chunk chunk;

        void Update()
        {
            GetData();
            // minus worldsize to simulate starting at 0,0
            positionText.text = $"x: {x - Constants.WorldSizeInBlocks / 2}, y: {y}, z: {z - Constants.WorldSizeInBlocks / 2}";
            chunkCoordText.text = $"Chunk: {chunkCoord.x - Constants.WorldSizeInChunks / 2},{chunkCoord.z - Constants.WorldSizeInChunks / 2}";
            LightLevelText.text = $"Light Level: {lightLevel}";
        }

        void GetData()
        {
            try
            {
                x = (int)math.floor(world.player.position.x);
                y = (int)math.floor(world.player.position.y);
                z = (int)math.floor(world.player.position.z);
                if (world.chunkChanged)
                {
                    chunkCoord = world.playerChunkCoord;
                    chunk = world.GetChunk(chunkCoord);
                }
                if (chunk != null)
                    lightLevel = chunk.LightMap[Chunk.ConvertToIndex(x % Constants.ChunkWidth, y, z % Constants.ChunkWidth)];
            }
            catch (InvalidOperationException)
            {
                //Debug.Log(ex);
            }
        }
    }
}