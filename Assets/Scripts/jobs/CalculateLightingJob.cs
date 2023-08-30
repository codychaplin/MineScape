using Unity.Jobs;
using Unity.Collections;

namespace minescape.jobs
{
    public struct CalculateLightingJob : IJob
    {
        [ReadOnly] public NativeArray<byte> blockMap;
        [WriteOnly] public NativeArray<byte> LightMap;

        public void Execute()
        {
            
        }
    }
}