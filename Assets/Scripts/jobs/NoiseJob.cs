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
        [ReadOnly] public int offset;
        [ReadOnly] public float scale;
        [ReadOnly] public int octaves;
        [ReadOnly] public float persistance;
        [ReadOnly] public float lacunarity;
        [ReadOnly] public int2 position;
        [WriteOnly] public NativeArray<byte> map;

        public void Execute()
        {
            float min = 100;
            float max = -100;
            for (int x = 0; x < Constants.MapChunkWidth; x++)
            {
                for (int z = 0; z < Constants.MapChunkWidth; z++)
                {
                    var pos = new float2(position.x + x, position.y + z);
                    float elevation = Noise.GetElevation(pos, offset, scale, octaves, persistance, lacunarity);
                    //float temperature = Noise.GetClamped2DNoise(pos, 0, 0.06f, false);
                    //float humidity = Noise.GetClamped2DNoise(pos, 0, 0.15f, true);
                    if (elevation < min)
                        min = elevation;
                    if (elevation > max)
                        max = elevation;

                    var index = MapChunk.ConvertToIndex(x, z);
                    map[index] = (byte)(elevation * 255);
                }
            }
            UnityEngine.Debug.Log($"min={min}, max={max}");
        }
    }
}