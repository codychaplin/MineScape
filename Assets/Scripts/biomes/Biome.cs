using minescape.block;

namespace minescape.biomes
{
    public struct Biome
    {
        public byte ID { get; }
        public byte SurfaceBlock { get; }
        public byte FillerBlock { get; }
        public float TreeFrequency { get; }

        public Biome(byte id, byte surfaceBlock, byte fillerBlock, float treeFrequency)
        {
            ID = id;
            SurfaceBlock = surfaceBlock;
            FillerBlock = fillerBlock;
            TreeFrequency = treeFrequency;
        }
    }
}