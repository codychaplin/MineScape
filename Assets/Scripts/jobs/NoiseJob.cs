using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using minescape.world.chunk;

namespace minescape.jobs
{
    public struct NoiseJob : IJob
    {
        [ReadOnly] public int2 position;
        [WriteOnly] public NativeArray<byte> map;

        public void Execute()
        {
            for (int x = 0; x < Constants.MapChunkWidth; x++)
            {
                for (int z = 0; z < Constants.MapChunkWidth; z++)
                {
                    var pos = new float2(position.x + x, position.y + z);
                    float height = Noise.Get2DPerlin(pos, 0, 0.4f, 3, 0.5f, 2f);
                    var index = MapChunk.ConvertToIndex(x, z);
                    map[index] = (byte)(height * 255);
                }
            }
        }
    }
}