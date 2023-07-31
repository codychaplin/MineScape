using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using minescape.world.chunk;

namespace minescape.jobs
{
    public struct SetMapBlockDataJob : IJob
    {
        [ReadOnly]
        public int2 position;
        [WriteOnly]
        public NativeArray<byte> map;

        public void Execute()
        {
            for (int x = 0; x < Constants.MapChunkWidth; x++)
            {
                for (int z = 0; z < Constants.MapChunkWidth; z++)
                {
                    var noise = Noise.Get2DPerlin(new float2(position.x + x, position.y + z), 0, 0.15f);
                    var terrainHeight = (int)math.floor(128 * noise + 32);
                    var index = MapChunk.ConvertToIndex(x, z);
                    var blockID = (byte)((terrainHeight > Constants.WaterLevel) ? 2 : 6);
                    map[index] = blockID;
                }
            }
        }
    }
}