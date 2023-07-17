using UnityEngine;

public class Noise
{
    public static float Get2DPerlin(Vector2 pos, float offset, float scale)
    {
        return Mathf.PerlinNoise(pos.x / Block.ChunkWidth * scale + offset, pos.y / Block.ChunkWidth * scale + offset);
    }
}