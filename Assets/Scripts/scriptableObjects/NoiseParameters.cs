using UnityEngine;

[CreateAssetMenu(fileName = "Noise Parameters", menuName = "MineScape/Noise Parameters")]
public class NoiseParameters : ScriptableObject
{
    public int offset;
    public float scale;
    public int minTerrainheight;
    public int maxTerrainheight;
}