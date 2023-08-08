namespace minescape
{
    public static class Constants
    {
        // chunk data
        public static readonly int ChunkWidth = 16;
        public static readonly int ChunkHeight = 256;

        // world data
        public static readonly int WorldSizeInChunks = 1024;
        public static int HalfWorldSizeInChunks => WorldSizeInChunks / 2;
        public static int WorldSizeInBlocks => WorldSizeInChunks * ChunkWidth;

        // view distance
        public static readonly int ViewDistance = 12;

        // misc
        public static readonly int WaterLevel = 63;

        // map data
        public static readonly int MapChunkWidth = 512;
        public static int WorldSizeInMapChunks = 4;
        public static int WorldSizeInMapBlocks => WorldSizeInMapChunks * MapChunkWidth;
    }
}