using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

namespace minescape.splines
{
    [CreateAssetMenu(fileName = "Spline", menuName = "MineScape/Splines")]
    public class Spline : ScriptableObject
    {
        public float2[] point;
    }

    public class Splines
    {
        public NativeArray<float2> Elevation;

        public Splines()
        {
            Elevation = new NativeArray<float2>(5, Allocator.Persistent);
            Elevation[0] = new float2(-1f, 40f);
            Elevation[1] = new float2(-0.4f, 44f);
            Elevation[2] = new float2(-0.35f, 53f);
            Elevation[3] = new float2(-0.04f, 56f);
            Elevation[4] = new float2(0f, 63f);
        }
    }
}