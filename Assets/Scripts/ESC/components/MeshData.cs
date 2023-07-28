using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace minescape.ESC.components
{
    public struct MeshData : IComponentData
    {
        public int vertexIndex;
        public NativeList<float3> vertices;
        public NativeList<int> triangles;
        public NativeList<float2> uvs;

        public MeshData(int _vertexIndex)
        {
            vertexIndex = _vertexIndex;
            vertices = new(4096, Allocator.Persistent);
            triangles = new(4096, Allocator.Persistent);
            uvs = new(4096, Allocator.Persistent);
        }

        public void Dispose()
        {
            vertices.Dispose();
            triangles.Dispose();
            uvs.Dispose();
        }
    }
}