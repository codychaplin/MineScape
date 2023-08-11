using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using minescape.world.chunk;

namespace minescape.jobs
{
    [BurstCompile]
    public struct NoiseJob : IJob
    {
        [ReadOnly] public float elevationScale;
        [ReadOnly] public int elevationOctaves;
        [ReadOnly] public float reliefScale;
        [ReadOnly] public int reliefOctaves;
        [ReadOnly] public float topographyScale;
        [ReadOnly] public int topographyOctaves;
        [ReadOnly] public float persistance;
        [ReadOnly] public float lacunarity;
        [ReadOnly] public int2 position;
        [WriteOnly] public NativeArray<byte> map;

        public void Execute()
        {
            for (int x = 0; x < Constants.MapChunkWidth; x++)
            {
                for (int z = 0; z < Constants.MapChunkWidth; z++)
                {
                    var pos = new float2(position.x + x, position.y + z);
                    float elevation = Noise.GetTerrainNoise(pos, 0, elevationScale, elevationOctaves, persistance, lacunarity, 15, 2.35f);
                    /*float elev = math.clamp(elevation + 1f, 0f, 1f);
                    elevation -= elev;*/

                    /*float relief = Noise.GetTerrainNoise(pos, -10000, reliefScale, reliefOctaves, persistance, lacunarity, 12, 2.35f, TerrainNoise.Relief);
                    float topography = Noise.GetTerrainNoise(pos, 10000, topographyScale, topographyOctaves, persistance, lacunarity, 8, 1.65f, TerrainNoise.Topography);
                    float temperature = Noise.GetBiomeNoise(pos, -20000, 0.06f, true);
                    float humidity = Noise.GetBiomeNoise(pos, 20000, 0.15f, true);*/


                    elevation = (elevation + 1f) / 2f;
                    float height = math.clamp(elevation * 255, 0, 255);
                    var index = MapChunk.ConvertToIndex(x, z);
                    map[index] = (byte)height;
                }
            }
        }
    }
}