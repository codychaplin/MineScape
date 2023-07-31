using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using minescape.init;

namespace minescape.jobs
{
    public struct SetBlockDataJob : IJob
    {
        
        [ReadOnly] public int offset;
        [ReadOnly] public float scale;
        [ReadOnly] public int minTerrainheight;
        [ReadOnly] public int maxTerrainheight;

        [ReadOnly] public int3 position;
        [WriteOnly] public NativeArray<byte> map;

        public void Execute()
        {
            for (int x = 0; x < Constants.ChunkWidth; x++)
            {
                for (int z = 0; z < Constants.ChunkWidth; z++)
                {
                    var noise = Noise.Get2DPerlin(new float2(position.x + x, position.z + z), offset, scale);
                    var terrainHeight = (int)math.floor(maxTerrainheight * noise + minTerrainheight);
                    for (int y = 0; y < Constants.ChunkHeight; y++)
                    {
                        int index = x + z * Constants.ChunkWidth + y * Constants.ChunkHeight;
                        if (y == 0)
                            map[index] = Blocks.BEDROCK.ID;
                        else if (y <= terrainHeight)
                            map[index] = Blocks.STONE.ID;
                        else if (y > terrainHeight && y == Constants.WaterLevel)
                            map[index] = Blocks.WATER.ID;
                        else if (y > terrainHeight && y > Constants.WaterLevel)
                            break;
                    }
                }
            }
        }
    }
}