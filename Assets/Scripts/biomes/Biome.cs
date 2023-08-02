using minescape.block;

namespace minescape.biomes
{
    public struct Biome
    {
        public byte ID { get; }
        public Block SurfaceBlock { get; }

        public Biome(byte id, Block surfaceBlock)
        {
            ID = id;
            SurfaceBlock = surfaceBlock;
        }
    }
}