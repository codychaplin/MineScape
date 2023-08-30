using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using minescape.init;
using minescape.world.chunk;

namespace minescape.jobs
{
    [BurstCompile]
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
                    Calculate(ref index3D, ref adjIndex3D, ref index2D, ref blockID, ref adjBlockID, northChunk, northFace);
                }

            for (int x = 0; x < Constants.ChunkWidth; x++)
                for (int y = 0; y < Constants.ChunkHeight; y++)
                {
                    index3D = Chunk.ConvertToIndex(x, y, 0);
                    adjIndex3D = Chunk.ConvertToIndex(x, y, 15);
                    index2D = x + y * Constants.ChunkWidth;
                    Calculate(ref index3D, ref adjIndex3D, ref index2D, ref blockID, ref adjBlockID, southChunk, southFace);
                }

            for (int z = 0; z < Constants.ChunkWidth; z++)
                for (int y = 0; y < Constants.ChunkHeight; y++)
                {
                    index3D = Chunk.ConvertToIndex(15, y, z);
                    adjIndex3D = Chunk.ConvertToIndex(0, y, z);
                    index2D = z + y * Constants.ChunkWidth;
                    Calculate(ref index3D, ref adjIndex3D, ref index2D, ref blockID, ref adjBlockID, eastChunk, eastFace);
                }

            for (int z = 0; z < Constants.ChunkWidth; z++)
                for (int y = 0; y < Constants.ChunkHeight; y++)
                {
                    index3D = Chunk.ConvertToIndex(0, y, z);
                    adjIndex3D = Chunk.ConvertToIndex(15, y, z);
                    index2D = z + y * Constants.ChunkWidth;
                    Calculate(ref index3D, ref adjIndex3D, ref index2D, ref blockID, ref adjBlockID, westChunk, westFace);
                }
        }

        void Calculate(ref int index3D, ref int adjIndex3D, ref int index2D, ref byte blockID, ref byte adjBlockID, NativeArray<byte> adjChunk, NativeArray<bool> adjFace)
        {
            blockID = chunk[index3D];
            adjBlockID = adjChunk[adjIndex3D];

            bool notAirNotWater = adjBlockID != BlockIDs.AIR && adjBlockID != BlockIDs.WATER;
            bool bothWater = blockID == BlockIDs.WATER && adjBlockID == BlockIDs.WATER;

            adjFace[index2D] = notAirNotWater || bothWater;
        }
    }
}