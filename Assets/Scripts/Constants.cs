namespace minescape
{
    public static class Constants
    {
        // chunk data
        public static readonly int ChunkWidth = 16;
        public static readonly int ChunkHeight = 256;

        // world data
        public static readonly int WorldSizeInChunks = 32;
        public static int HalfWorldSizeInChunks => WorldSizeInChunks / 2;
        public static int WorldSizeInBlocks => WorldSizeInChunks * ChunkWidth;

        // view distance
        public static readonly int ViewDistance = 3;

        // misc
        public static int WaterLevel = 64;
    }
}