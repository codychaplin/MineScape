using Unity.Mathematics;
using minescape;

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

    public static float Get2DNoiseOctaves(float2 pos, float offset, float scale, int octaves, float persistance, float lacunarity)
    {
        // init position and multipliers
        pos = new float2(pos.x / Constants.ChunkWidth * scale + offset, pos.y / Constants.ChunkWidth * scale + offset);
        float totalNoise = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        
        // calculate noise with octaves
        for (int oct = 0; oct < octaves; oct++)
        {
            var noiseValue = noise.cnoise(pos * frequency);
            totalNoise += noiseValue * amplitude;
            amplitude *= persistance;
            frequency *= lacunarity;
        }

        // normalize between 0-1
        float normalizedValue = (totalNoise + octaves) / (2f * octaves);
        return normalizedValue;
    }

    public static float GetElevation(float2 pos, float offset, float scale, int octaves, float persistance, float lacunarity)
    {
        // init position and multipliers
        pos = new float2(pos.x / Constants.ChunkWidth * scale + offset, pos.y / Constants.ChunkWidth * scale + offset);
        float totalNoise = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        // calculate noise with octaves
        for (int oct = 0; oct < octaves; oct++)
        {
            var noiseValue = noise.cnoise(pos * frequency);
            totalNoise += noiseValue * amplitude;
            amplitude *= persistance;
            frequency *= lacunarity;
        }

        var fuzzyNoise = noise.cnoise(pos * 10);
        totalNoise = (totalNoise + fuzzyNoise / 10) / 1.1f;

        // normalize between 0-1
        float normalizedValue = (totalNoise + octaves) / (2f * octaves);

        if (normalizedValue >= 0 && normalizedValue < 0.4f)
            normalizedValue = 0f;
        else if (normalizedValue >= 0.4f && normalizedValue < 0.47f)
            normalizedValue = 0.25f;
        else if (normalizedValue >= 0.47f && normalizedValue < 0.5f)
            normalizedValue = 0.5f;
        else if (normalizedValue >= 0.5f && normalizedValue < 0.55f)
            normalizedValue = 0.75f;
        else if (normalizedValue >= 0.55f && normalizedValue <= 1f)
            normalizedValue = 1f;

        return normalizedValue;
    }

    public static float GetClamped2DNoiseOctaves(float2 pos, float offset, float scale, int octaves, float persistance, float lacunarity)
    {
        // init position and multipliers
        pos = new float2(pos.x / Constants.ChunkWidth * scale + offset, pos.y / Constants.ChunkWidth * scale + offset);
        float totalNoise = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        // calculate noise with octaves
        for (int oct = 0; oct < octaves; oct++)
        {
            var noiseValue = noise.cnoise(pos * frequency);
            totalNoise += noiseValue * amplitude;
            amplitude *= persistance;
            frequency *= lacunarity;
        }

        // normalize between 0-1
        float normalizedValue = (totalNoise + octaves) / (2f * octaves);

        if (normalizedValue >= 0 && normalizedValue < 0.38f)
            normalizedValue = 0.1f;
        else if (normalizedValue >= 0.38f && normalizedValue < 0.46f)
            normalizedValue = 0.3f;
        else if (normalizedValue >= 0.46f && normalizedValue < 0.54f)
            normalizedValue = 0.5f;
        else if (normalizedValue >= 0.54f && normalizedValue < 0.64f)
            normalizedValue = 0.7f;
        else if (normalizedValue >= 0.64f && normalizedValue <= 0.7f)
            normalizedValue = 0.9f;

        return normalizedValue;
    }

    public static float GetClamped2DNoise(float2 pos, float offset, float scale, bool isFuzzy)
    {
        // get noise value
        pos = new float2(pos.x / Constants.ChunkWidth * scale + offset, pos.y / Constants.ChunkWidth * scale + offset);
        var noiseValue = noise.cnoise(pos);
        if (isFuzzy)
        {
            var fuzzyNoise = noise.cnoise(pos * 10);
            noiseValue = (noiseValue + fuzzyNoise / 20) / 1.05f;
        }

        // normalize between 0-1
        float normalized = (noiseValue + 1f) / 2f;

        // clamp to intervals
        if (normalized >= 0 && normalized < 0.2f)
            normalized = 0.1f;
        else if (normalized >= 0.2f && normalized < 0.4f)
            normalized = 0.3f;
        else if (normalized >= 0.4f && normalized < 0.6f)
            normalized = 0.5f;
        else if (normalized >= 0.6f && normalized < 0.8f)
            normalized = 0.7f;
        else if (normalized >= 0.8f && normalized <= 1f)
            normalized = 0.9f;

        return normalized;
    }
}