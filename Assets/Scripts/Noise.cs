using minescape.block;
using Unity.Mathematics;

public class Noise
{
    public static float Get2DPerlin(float2 pos, float offset, float scale)
    {
        pos = new float2(pos.x / BlockData.ChunkWidth * scale + offset, pos.y / BlockData.ChunkWidth * scale + offset);
        var og = noise.cnoise(pos);
        return (og + 0.707f) / (2f * 0.707f);
    }
}