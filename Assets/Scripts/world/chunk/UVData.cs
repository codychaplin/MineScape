using Unity.Mathematics;

namespace minescape.world.chunk
{
    public struct UVData
    {
        public float2 uv0;
        public float2 uv1;

        public UVData(float2 _uv0, float2 _uv1)
        {
            uv0 = _uv0;
            uv1 = _uv1;
        }

        public override string ToString()
        {
            return $"uv0:{uv0}, uv1:{uv1}";
        }
    }
}