using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using minescape.ESC.components;
using Unity.Rendering;
using UnityEngine;

namespace minescape.ESC.systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct CreateChunkSystem : ISystem
    {
        bool isInitialized;

        public void OnCreate(ref SystemState state)
        {
            isInitialized = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            if(!isInitialized)
                CreateChunk(0, 0, ref state);
        }


        public void OnDestroy(ref SystemState state)
        {
            foreach (var (chunk, meshData) in SystemAPI.Query<RefRW<Chunk>, RefRW<MeshData>>())
            {
                chunk.ValueRW.Dispose();
                meshData.ValueRW.Dispose();
            }
        }

        void CreateChunk(int x, int z, ref SystemState state)
        {
            // create command buffer
            var cbs = state.World.GetOrCreateSystemManaged<BeginInitializationEntityCommandBufferSystem>();
            var cb = cbs.CreateCommandBuffer();

            // create entity
            EntityArchetype archetype = state.World.EntityManager.CreateArchetype(
                typeof(Chunk),
                typeof(MeshData),
                typeof(LocalTransform),
                typeof(LocalToWorld),
                typeof(RenderMesh)
                );
            Entity entity = cb.CreateEntity(archetype);

            // add components
            ChunkCoord coord = new(x, z);
            Chunk chunk = new(coord);
            float3 pos = new(coord.x * Constants.ChunkWidth, 0f, coord.z * Constants.ChunkWidth);
            MeshData meshData = new(0);
            LocalTransform transform = new() { Position = pos, Scale = 1 };
            LocalToWorld transformWorld = new() { Value = float4x4.Translate(pos) };

            cb.SetName(entity, chunk.ToString());
            cb.SetComponent(entity, chunk);
            cb.SetComponent(entity, meshData);
            cb.SetComponent(entity, transform);
            cb.SetComponent(entity, transformWorld);
            cb.AddComponent<NeedsBlockMap>(entity);

            isInitialized = true;
        }
    }
}