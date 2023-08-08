using UnityEngine;
using Unity.Mathematics;

namespace minescape.splines
{
    [CreateAssetMenu(fileName = "Spline", menuName = "MineScape/Splines")]
    public class Spline : ScriptableObject
    {
        public float2[] point;
    }

    public static class Splines
    {
        public static float2[] Elevation = new float2[]
        {
            new float2(-1f,32f),
            new float2(-0.45f,33f),
            new float2(-0.4f,46f),
            new float2(-0.1f,46f),
            new float2(0f,64f),
            new float2(0.1f,65f),
            new float2(0.75f,70f),
            new float2(0.9f,80f),
            new float2(1f,90f)
        };

        public static float2[] Topography = new float2[]
        {
            new float2(-1f,-32f),
            new float2(-0.5f,-16f),
            new float2(0f,0f),
            new float2(0.5f,16f),
            new float2(1f,32f)
        };
    }
}