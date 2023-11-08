using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

namespace minescape.jobs
{
    [BurstCompile]
    public struct GenerateChunkSectionJob : IJob
    {
        public NativeArray<byte> blocks;
        public NativeArray<byte> heightMap;

        public void Execute()
        {
            
        }
    }
}
