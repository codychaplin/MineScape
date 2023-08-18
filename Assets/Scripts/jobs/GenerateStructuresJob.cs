using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

namespace minescape.jobs
{
    public struct GenerateStructuresJob : IJob
    {
        [ReadOnly] public int seed;

        [ReadOnly] public int3 position;

        [WriteOnly] public NativeArray<byte> blockMap;

        public void Execute()
        {
            
        }
    }
}