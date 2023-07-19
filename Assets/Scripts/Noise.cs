using UnityEngine;
using block;

public class Noise
{
    public static float Get2DPerlin(Vector2 pos, float offset, float scale)
    {
        return Mathf.PerlinNoise(pos.x / BlockData.ChunkWidth * scale + offset, pos.y / BlockData.ChunkWidth * scale + offset);
    }
}