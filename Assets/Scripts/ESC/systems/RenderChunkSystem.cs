using Unity.Entities;
using minescape.ESC.components;
using Unity.Rendering;
using UnityEngine;
using System.Linq;
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
                var material = Resources.Load<Material>("Materials/TextureMap");

                var verticesArray = meshData.ValueRO.vertices.ToArray(Allocator.Temp);
                var trianglesArray = meshData.ValueRO.triangles.ToArray(Allocator.Temp);
                var uvsArray = meshData.ValueRO.uvs.ToArray(Allocator.Temp);

                Mesh mesh = new();
                mesh.SetVertices(verticesArray);
                mesh.SetTriangles(trianglesArray.ToArray(), 0);
                mesh.SetUVs(0, uvsArray);
                mesh.RecalculateNormals();

                RenderMesh renderMesh = new()
                {
                    mesh = mesh,
                    material = material,
                    subMesh = 0
                };

                cb.SetSharedComponentManaged(entity, renderMesh);
                cb.RemoveComponent<NeedsRendering>(entity);
            }
        }


        public void OnDestroy(ref SystemState state)
        {
            
        }
    }
}