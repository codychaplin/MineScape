using UnityEngine;

[CreateAssetMenu(fileName = "Noise Parameters", menuName = "MineScape/Noise Parameters")]
public class NoiseParameters : ScriptableObject
{
    public int offset;
    public float scale;
    public float borderWeight;
    public float borderScale;
    public int minTerrainheight;
    public int maxTerrainheight;
}