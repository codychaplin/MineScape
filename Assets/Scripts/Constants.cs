namespace minescape
{
    public static class Constants
    {
        // chunk data
        public static readonly int ChunkWidth = 16;
        public static readonly int ChunkHeight = 256;

        // world data
        public static readonly int WorldSizeInChunks = 32;
        public static int WorldSizeInBlocks => WorldSizeInChunks * ChunkWidth;

        // misc
        public static int WaterLevel = 64;
    }
}