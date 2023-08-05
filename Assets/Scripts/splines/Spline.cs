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
            new float2(0f,32f),
            new float2(0.25f,32f),
            new float2(0.3f,50f),
            new float2(0.5f,50f),
            new float2(0.55f,64f),
            new float2(0.6f,65f),
            new float2(0.7f,70f),
            new float2(0.9f,80f),
            new float2(1f,85f)
        };
    }
}