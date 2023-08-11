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
            new float2(-1f,36f),
            new float2(-0.5f,38f),
            new float2(-0.45f,43f),
            new float2(-0.3f,53f),
            new float2(-0.02f,56f),
            new float2(0f,63f)
            /*new float2(0.2f,65f),
            new float2(0.25f,70f),
            new float2(0.4f,71f),
            new float2(0.45f,80f),
            new float2(0.6f,81f),
            new float2(0.9f,90f),
            new float2(1f,100f)*/
        };

        public static float2[] Topography = new float2[]
        {
            new float2(-1f,48f),
            new float2(-0.7f,56f),
            new float2(-0.5f,64f),
            new float2(-0.3f,32f),
            new float2(-0.03f,0f),
            new float2(-0.02f,-12f),
            new float2(0.02f,-12f),
            new float2(0.03f,0),
            new float2(0.4f,32),
            new float2(0.8f,64f),
            new float2(1f,56f)
        };
    }
}