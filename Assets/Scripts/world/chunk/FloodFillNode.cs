using Unity.Burst;
using Unity.Mathematics;

namespace minescape.world.chunk
{
    [BurstCompile]
    public struct FloodFillNode
    {
        public int3 Pos;
        public byte LightLevel;

        public FloodFillNode(int3 pos, byte lightLevel)
        {
            Pos = pos;
            LightLevel = lightLevel;
        }
    }
}