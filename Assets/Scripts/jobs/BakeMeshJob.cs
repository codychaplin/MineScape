using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

namespace minescape.jobs
{
    [BurstCompile]
    public struct BakeMeshJob : IJob
    {
        public NativeReference<int> meshID;

        public void Execute()
        {
            Physics.BakeMesh(meshID.Value, false);
        }
    }
}
