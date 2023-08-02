using UnityEngine;

namespace minescape.scriptableobjects
{
    [CreateAssetMenu(fileName = "Biome", menuName = "MineScape/Biome")]
    public class Biome : ScriptableObject
    {
        public float minTemperature;
        public float maxTemperature;
        public float minHumidity;
        public float maxHumidity;
        public int minTerrainheight;
        public int maxTerrainheight;
    }
}