using Unity.Entities;
using minescape.ESC.components;
using Unity.Rendering;
using UnityEngine;
using Unity.Collections;

namespace minescape.ESC.systems
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct RenderChunkSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            
        }

        public void OnUpdate(ref SystemState state)
        {
            var cbs = state.World.GetOrCreateSystemManaged<BeginPresentationEntityCommandBufferSystem>();
            var cb = cbs.CreateCommandBuffer();

            foreach (var (_, _, meshData, entity) in SystemAPI.Query<RefRO<NeedsRendering>, RefRO<Chunk>, RefRO<MeshData>>().WithEntityAccess())
            {
                var verticesArray = meshData.ValueRO.vertices.ToArray(Allocator.Temp);
                var trianglesArray = meshData.ValueRO.triangles.ToArray(Allocator.Temp);
                var uvsArray = meshData.ValueRO.uvs.ToArray(Allocator.Temp);

                Mesh mesh = new();
                mesh.SetVertices(verticesArray);
                mesh.SetTriangles(trianglesArray.ToArray(), 0);
                mesh.SetUVs(0, uvsArray);
                mesh.RecalculateNormals();

                var renderMeshArray = state.EntityManager.GetSharedComponentManaged<RenderMeshArray>(entity);
                renderMeshArray.Meshes[0] = mesh;

                /*float4x4 matrix = state.EntityManager.GetComponentData<LocalToWorld>(entity).Value;
                Bounds bounds = mesh.bounds;
                AABB aabb = new();
                aabb.Center = math.transform(matrix, bounds.center);
                aabb.Extents = bounds.extents * math.cmax(matrix.c0.xyz, matrix.c1.xyz, matrix.c2.xyz);
                RenderBounds renderBounds = new() { Value = aabb };*/

                cb.SetSharedComponentManaged(entity, renderMeshArray);
                cb.RemoveComponent<NeedsRendering>(entity);
            }
        }


        public void OnDestroy(ref SystemState state)
        {
            foreach (var (chunk, meshData) in SystemAPI.Query<RefRW<Chunk>, RefRW<MeshData>>())
            {
                chunk.ValueRW.Dispose();
                meshData.ValueRW.Dispose();
            }
        }
    }
}