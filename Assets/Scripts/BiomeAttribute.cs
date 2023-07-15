using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "MineScape/Biome Attribute")]
public class BiomeAttribute : ScriptableObject
{
    public string biomeName;
    public int minTerrainHeight;
    public int maxTerrainHeight;
    public float scale;
}