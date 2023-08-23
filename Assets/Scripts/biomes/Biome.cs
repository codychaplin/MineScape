using minescape.block;

namespace minescape.biomes
{
    public struct Biome
    {
        public byte ID { get; }
        public int TerrainHeight { get; }
        public Block SurfaceBlock { get; }
        public Block FillerBlock { get; }
        public float TreeFrequency { get; }

        public Biome(byte id, int terrainHeight, Block surfaceBlock, Block fillerBlock, float treeFrequency)
        {
            ID = id;
            TerrainHeight = terrainHeight;
            SurfaceBlock = surfaceBlock;
            FillerBlock = fillerBlock;
            TreeFrequency = treeFrequency;
        }
    }
}