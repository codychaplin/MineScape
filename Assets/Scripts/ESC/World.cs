using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine.Rendering;
using minescape.ESC.components;

namespace minescape.ECS
{
    public class World : MonoBehaviour
    {
        public Material TextureMap;
        EntityManager entityManager;
        // Start is called before the first frame update
        void Start()
        {
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            entityManager = world.EntityManager;
            for (int x = 0; x < 2; x++)
                for (int z = 0; z < 2; z++)
                CreateChunk(x, z, entityManager);
        }

        // Update is called once per frame
        void Update()
        {

        }

        void CreateChunk(int x, int z, EntityManager entityManager)
        {
            EntityArchetype archetype = entityManager.CreateArchetype(
                typeof(Chunk),
                typeof(MeshData),
                typeof(LocalTransform)
                );
            Entity entity = entityManager.CreateEntity(archetype);

            // add components
            ChunkCoord coord = new(x, z);
            Chunk chunk = new(coord);
            float3 pos = new(coord.x * Constants.ChunkWidth, 0f, coord.z * Constants.ChunkWidth);
            MeshData meshData = new(0);
            LocalTransform transform = new() { Position = pos, Scale = 1 };

            var desc = new RenderMeshDescription(shadowCastingMode: ShadowCastingMode.Off, renderingLayerMask: 1);
            var renderMeshArray = new RenderMeshArray(new Material[] { TextureMap }, new Mesh[] { new Mesh() });
            RenderMeshUtility.AddComponents(entity, entityManager, desc, renderMeshArray, MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));

            var renderBounds = entityManager.GetComponentData<RenderBounds>(entity);
            renderBounds.Value.Extents = new float3(Constants.ChunkWidth, Constants.ChunkHeight, Constants.ChunkWidth);
            entityManager.SetComponentData(entity, renderBounds);

            entityManager.SetName(entity, chunk.ToString());
            entityManager.SetComponentData(entity, chunk);
            entityManager.SetComponentData(entity, meshData);
            entityManager.SetComponentData(entity, transform);
            entityManager.AddComponent<NeedsBlockMap>(entity);
        }
    }
}