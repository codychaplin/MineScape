using Unity.Mathematics;
using minescape;

public class Noise
{
    public static float GetPerlin(float2 pos, float offset, float scale)
    {
        pos = new float2(pos.x / 100 * scale + offset, pos.y / 100 * scale + offset);
        var og = noise.cnoise(pos);
        return (og + 1f) / 2f;
    }

    public static float GetSimplex(float2 pos, float offset, float scale)
    {
        pos = new float2(pos.x / 100 * scale + offset, pos.y / 100 * scale + offset);
        var og = noise.snoise(pos);
        return (og + 1f) / 2f;
    }

    public static float GetTerrainNoise(float2 pos, float offset, float scale, int octaves, float persistance, float lacunarity, int fuzziness, float normalizeFactor, TerrainNoise type)
    {
        // init position and multipliers
        pos = new float2(pos.x / 100 * scale + offset, pos.y / 100 * scale + offset);
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

        var fuzzyNoise = noise.cnoise(pos * fuzziness);
        totalNoise = (totalNoise + fuzzyNoise / 20) / 1.05f;

        // normalize between -1 to 1
        float normalizedValue = totalNoise / octaves;
        normalizedValue *= normalizeFactor; // more octaves = closer to 0. Amplify to get back to original range

        if (type == TerrainNoise.Elevation)
            return ElevationClamp(normalizedValue);
        else if (type == TerrainNoise.Relief)
            return ReliefClamp(normalizedValue);
        else // (type == TerrainNoise.Topography)
            return TopographyClamp(normalizedValue);
    }

    public static float GetClampedBiomeNoise(float2 pos, float offset, float scale, bool isFuzzy)
    {
        // get noise value
        pos = new float2(pos.x / 100 * scale + offset, pos.y / 100 * scale + offset);
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

    static float ElevationClamp(float value)
    {
        if (value < -0.35f)
            return 0f;
        else if (value < -0.15f)
            return 0.1f;
        else if (value < -0.1f)
            return 0.286f;
        else if (value < 0.1f)
            return 0.429f;
        else if (value < 0.35f)
            return 0.572f;
        else if (value < 0.75f)
            return 0.715f;
        else if (value < 0.9f)
            return 0.858f;
        else
            return 1f;
    }

    static float ReliefClamp(float value)
    {
        if (value < -0.75f)
            return 0f;
        else if (value < -0.5f)
            return 0.143f;
        else if (value < -0.25f)
            return 0.286f;
        else if (value < 0f)
            return 0.429f;
        else if (value < 0.25f)
            return 0.572f;
        else if (value < 0.5f)
            return 0.715f;
        else if (value < 0.75f)
            return 0.858f;
        else
            return 1f;
    }

    static float TopographyClamp(float value)
    {
        if (value < -0.7f)
            return 0.66f;
        else if (value < -0.5f)
            return 1f;
        else if (value < -0.3f)
            return 0.66f;
        else if (value < -0.07f)
            return 0.33f;
        else if (value < 0f)
            return 0f;
        else if (value < 0.2f)
            return 0.33f;
        else if (value < 0.4f)
            return 0.66f;
        else if (value < 0.8f)
            return 1f;
        else
            return 0.66f;
    }
}

public enum TerrainNoise
{
    Elevation,
    Relief,
    Topography
}