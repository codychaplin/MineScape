using Unity.Jobs;
using Unity.Collections;
using minescape.init;
using minescape.world.chunk;

namespace minescape.jobs
{
    public struct GetBorderDataJob : IJob
    {
        [ReadOnly] public NativeArray<byte> chunk;

        [ReadOnly] public NativeArray<byte> northChunk;
        [ReadOnly] public NativeArray<byte> southChunk;
        [ReadOnly] public NativeArray<byte> eastChunk;
        [ReadOnly] public NativeArray<byte> westChunk;

        [WriteOnly] public NativeArray<bool> northFace;
        [WriteOnly] public NativeArray<bool> southFace;
        [WriteOnly] public NativeArray<bool> eastFace;
        [WriteOnly] public NativeArray<bool> westFace;

        public void Execute()
        {
            int index3D = 0;
            int adjIndex3D = 0;
            int index2D = 0;
            byte blockID = 0;
            byte adjBlockID = 0;

            for (int x = 0; x < Constants.ChunkWidth; x++)
                for (int y = 0; y < Constants.ChunkHeight; y++)
                {
                    index3D = Chunk.ConvertToIndex(x, y, 15);
                    adjIndex3D = Chunk.ConvertToIndex(x, y, 0);
                    index2D = x + y * Constants.ChunkWidth;

                    blockID = chunk[index3D];
                    adjBlockID = northChunk[adjIndex3D];

                    bool notAirNotWater = adjBlockID != 0 && adjBlockID != Blocks.WATER.ID;
                    bool bothWater = blockID == Blocks.WATER.ID && adjBlockID == Blocks.WATER.ID;

                    northFace[index2D] = notAirNotWater || bothWater;
                }

            for (int x = 0; x < Constants.ChunkWidth; x++)
                for (int y = 0; y < Constants.ChunkHeight; y++)
                {
                    index3D = Chunk.ConvertToIndex(x, y, 0);
                    adjIndex3D = Chunk.ConvertToIndex(x, y, 15);
                    index2D = x + y * Constants.ChunkWidth;

                    blockID = chunk[index3D];
                    adjBlockID = southChunk[adjIndex3D];

                    bool notAirNotWater = adjBlockID != 0 && adjBlockID != Blocks.WATER.ID;
                    bool bothWater = blockID == Blocks.WATER.ID && adjBlockID == Blocks.WATER.ID;

                    southFace[index2D] = notAirNotWater || bothWater;
                }

            for (int z = 0; z < Constants.ChunkWidth; z++)
                for (int y = 0; y < Constants.ChunkHeight; y++)
                {
                    index3D = Chunk.ConvertToIndex(15, y, z);
                    adjIndex3D = Chunk.ConvertToIndex(0, y, z);
                    index2D = z + y * Constants.ChunkWidth;

                    blockID = chunk[index3D];
                    adjBlockID = eastChunk[adjIndex3D];

                    bool notAirNotWater = adjBlockID != 0 && adjBlockID != Blocks.WATER.ID;
                    bool bothWater = blockID == Blocks.WATER.ID && adjBlockID == Blocks.WATER.ID;

                    eastFace[index2D] = notAirNotWater || bothWater;
                }

            for (int z = 0; z < Constants.ChunkWidth; z++)
                for (int y = 0; y < Constants.ChunkHeight; y++)
                {
                    index3D = Chunk.ConvertToIndex(0, y, z);
                    adjIndex3D = Chunk.ConvertToIndex(15, y, z);
                    index2D = z + y * Constants.ChunkWidth;

                    blockID = chunk[index3D];
                    adjBlockID = westChunk[adjIndex3D];

                    bool notAirNotWater = adjBlockID != 0 && adjBlockID != Blocks.WATER.ID;
                    bool bothWater = blockID == Blocks.WATER.ID && adjBlockID == Blocks.WATER.ID;

                    westFace[index2D] = notAirNotWater || bothWater;
                }
        }
    }
}