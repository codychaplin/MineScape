using UnityEngine;

namespace minescape.biomes
{
    public struct Biome
    {
        public byte ID { get; }
        public byte SurfaceBlock { get; }
        public byte FillerBlock { get; }
        public float TreeFrequency { get; }
        public float GrassDensity { get; }
        public Color32 GrassTint { get; }

        public Biome(byte id, byte surfaceBlock, byte fillerBlock, Color32 grassTint, float treeFrequency, float grassDensity)
        {
            ID = id;
            SurfaceBlock = surfaceBlock;
            FillerBlock = fillerBlock;
            GrassTint = grassTint;
            TreeFrequency = treeFrequency;
            GrassDensity = grassDensity;
        }
    }
}