using UnityEngine;

namespace minescape.scriptableobjects
{
    [CreateAssetMenu(fileName = "Noise Parameters", menuName = "MineScape/Noise Parameters")]
    public class NoiseParameters : ScriptableObject
    {
        public int offset;
        public float scale;
        public int octaves;
        public float persistance;
        public float lacunarity;
        public int minTerrainHeight;
        public int maxTerrainHeight;
    }
}