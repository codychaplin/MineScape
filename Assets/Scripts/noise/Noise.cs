using minescape;
using Unity.Mathematics;

public class Noise
{
    public static float GetPerlin(float2 pos, float offset, float scale)
    {
        pos = new float2(pos.x / Constants.ChunkWidth * scale + offset, pos.y / Constants.ChunkWidth * scale + offset);
        var og = noise.cnoise(pos);
        return (og + 1f) / 2f;
    }

    public static float GetSimplex(float2 pos, float offset, float scale)
    {
        pos = new float2(pos.x / Constants.ChunkWidth * scale + offset, pos.y / Constants.ChunkWidth * scale + offset);
        var og = noise.snoise(pos);
        return (og + 1f) / 2f;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pos">block position</param>
    /// <param name="offset">offset</param>
    /// <param name="scale">scale of noise</param>
    /// <param name="octaves">amount of noise layers</param>
    /// <param name="persistance">changes amplitude of each layer</param>
    /// <param name="lacunarity">changes frequency of each layer</param>
    /// <returns></returns>
    public static float Get2DPerlin(float2 pos, float offset, float scale, int octaves, float persistance, float lacunarity)
    {
        pos = new float2(pos.x / Constants.ChunkWidth * scale + offset, pos.y / Constants.ChunkWidth * scale + offset);
        float totalNoise = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        for (int oct = 0; oct < octaves; oct++)
        {
            var noiseValue = noise.cnoise(pos * frequency);
            totalNoise += noiseValue * amplitude;
            amplitude *= persistance;
            frequency *= lacunarity;
        }

        // normalize value to be between 0-1
        float normalizedValue = (totalNoise + octaves) / (2f * octaves);
        return normalizedValue;
    }
}