using minescape.block;

namespace minescape.biomes
{
    public struct Biome
    {
        public byte ID { get; }
        public int TerrainHeight { get; }
        public Block SurfaceBlock { get; }
        public Block FillerBlock { get; }

        public Biome(byte id, int terrainHeight, Block surfaceBlock, Block fillerBlock)
        {
            ID = id;
            TerrainHeight = terrainHeight;
            SurfaceBlock = surfaceBlock;
            FillerBlock = fillerBlock;
        }
    }
}