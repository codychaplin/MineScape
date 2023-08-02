using minescape.block;

namespace minescape.biomes
{
    public struct Biome
    {
        public byte ID { get; }
        public int TerrainHeight { get; }
        public Block SurfaceBlock { get; }

        public Biome(byte id, int terrainHeight, Block surfaceBlock)
        {
            ID = id;
            TerrainHeight = terrainHeight;
            SurfaceBlock = surfaceBlock;
        }
    }
}