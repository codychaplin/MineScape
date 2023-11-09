
namespace minescape.biome
{
    public struct Biome
    {
        public byte ID { get; }
        public byte SurfaceBlock { get; }
        public byte FillerBlock { get; }
        public float TreeFrequency { get; }
        public float GrassDensity { get; }
        public ushort GrassTint { get; }

        public Biome(byte id, byte surfaceBlock, byte fillerBlock, float treeFrequency, float grassDensity, ushort grassTint)
        {
            ID = id;
            SurfaceBlock = surfaceBlock;
            FillerBlock = fillerBlock;
            TreeFrequency = treeFrequency;
            GrassDensity = grassDensity;
            GrassTint = grassTint;
        }
    }
}