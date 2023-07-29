using Unity.Jobs;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using minescape.init;
using minescape.block;
using minescape.ESC.components;

namespace minescape.ESC.systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct GenerateChunkMeshDataSystem : ISystem
    {
        EntityQuery GenerateChunkMeshDataQuery;

        public void OnCreate(ref SystemState state)
        {
            GenerateChunkMeshDataQuery = state.GetEntityQuery(ComponentType.ReadOnly<NeedsMeshData>());
            state.RequireForUpdate(GenerateChunkMeshDataQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            var cbs = state.World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            var cb = cbs.CreateCommandBuffer();

            foreach (var (_, chunk, meshData, entity) in SystemAPI.Query<RefRO<NeedsMeshData>, RefRO<Chunk>, RefRW<MeshData>>().WithEntityAccess())
            {
                /*var northChunk = GetChunk(new ChunkCoord(chunk.coord.x, chunk.coord.z + 1));
                var southChunk = GetChunk(new ChunkCoord(chunk.coord.x, chunk.coord.z - 1));
                var eastChunk = GetChunk(new ChunkCoord(chunk.coord.x + 1, chunk.coord.z));
                var westChunk = GetChunk(new ChunkCoord(chunk.coord.x - 1, chunk.coord.z));*/
                new GenerateChunkMeshDataJob()
                {
                    coord = chunk.ValueRO.coord,
                    position = new int3(chunk.ValueRO.position.x, 0, chunk.ValueRO.position.z),
                    map = chunk.ValueRO.BlockMap,
                    /*north = northChunk.BlockMap,
                    south = southChunk.BlockMap,
                    east = eastChunk.BlockMap,
                    west = westChunk.BlockMap,*/
                    vertices = meshData.ValueRW.vertices,
                    triangles = meshData.ValueRW.triangles,
                    uvs = meshData.ValueRW.uvs,
                    vertexIndex = 0
                }.Schedule();
                cb.RemoveComponent<NeedsMeshData>(entity);
                cb.AddComponent<NeedsRendering>(entity);
            }
        }


        public void OnDestroy(ref SystemState state)
        {
            
        }
    }

    public partial struct GenerateChunkMeshDataJob : IJobEntity
    {
        [ReadOnly]
        public ChunkCoord coord;

        [ReadOnly]
        public int3 position;

        [ReadOnly]
        public NativeArray<byte> map;

        /*[ReadOnly]
        public NativeArray<byte> north;
        [ReadOnly]
        public NativeArray<byte> east;
        [ReadOnly]
        public NativeArray<byte> south;
        [ReadOnly]
        public NativeArray<byte> west;*/

        [WriteOnly]
        public NativeList<float3> vertices;
        [WriteOnly]
        public NativeList<int> triangles;
        [WriteOnly]
        public NativeList<float2> uvs;

        public int vertexIndex;

        public void Execute()
        {
            for (int x = 0; x < Constants.ChunkWidth; x++)
                for (int z = 0; z < Constants.ChunkWidth; z++)
                    for (int y = 0; y < Constants.ChunkHeight; y++)
                    {
                        int3 index3 = new(x, y, z);
                        int index = Chunk.ConvertToIndex(index3);
                        if (map[index] != 0)
                            AddBlockToChunk(index3);
                    }
        }

        bool IsBlockInWorld(int3 pos)
        {
            return pos.x >= 0 && pos.x < Constants.WorldSizeInBlocks &&
                   pos.y >= 0 && pos.y < Constants.ChunkHeight &&
                   pos.z >= 0 && pos.z < Constants.WorldSizeInBlocks;
        }

        void AddBlockToChunk(int3 pos)
        {
            int index = Chunk.ConvertToIndex(pos);
            var blockID = map[index];
            for (int i = 0; i < 6; i++)
            {
                if (blockID == 6 && i != 2) // only render top of water
                    continue;

                var adjacentIndex = pos + BlockData.faceCheck[i];
                if (!IsBlockInWorld(adjacentIndex + position)) // if out of world, skip
                    continue;

                var adjacentBlock = GetBlock(adjacentIndex);
                if (adjacentBlock != 0 && adjacentBlock != 6) // if adjacent block is not transparent, skip
                    continue;

                vertices.Add(pos + BlockData.verts[BlockData.tris[i, 0]]);
                vertices.Add(pos + BlockData.verts[BlockData.tris[i, 1]]);
                vertices.Add(pos + BlockData.verts[BlockData.tris[i, 2]]);
                vertices.Add(pos + BlockData.verts[BlockData.tris[i, 3]]);

                AddTexture(Blocks.blocks[blockID].Faces[i]);

                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);

                vertexIndex += 4;
            }
        }

        byte GetBlock(int3 pos)
        {
            if (!Chunk.IsBlockInChunk(pos.x, pos.y, pos.z))
            {
                /*if (pos.z >= Constants.ChunkWidth)
                {
                    int northIndex = Chunk.ConvertToIndex(pos.x, pos.y, 0);
                    return north[northIndex];
                }
                else if (pos.z < 0)
                {
                    int southIndex = Chunk.ConvertToIndex(pos.x, pos.y, 15);
                    return south[southIndex];
                }
                else if (pos.x >= Constants.ChunkWidth)
                {
                    int eastIndex = Chunk.ConvertToIndex(0, pos.y, pos.z);
                    return east[eastIndex];
                }
                else if (pos.x < 0)
                {
                    int westIndex = Chunk.ConvertToIndex(15, pos.y, pos.z);
                    return west[westIndex];
                }*/
                return 0;
            }

            int index = Chunk.ConvertToIndex(pos);
            return map[index];
        }

        void AddTexture(int textureId)
        {
            float y = textureId / BlockData.TextureAtlasSize;
            float x = textureId - (y * BlockData.TextureAtlasSize);

            x *= BlockData.NormalizedTextureSize;
            y *= BlockData.NormalizedTextureSize;

            uvs.Add(new float2(x, y));
            uvs.Add(new float2(x, y + BlockData.NormalizedTextureSize));
            uvs.Add(new float2(x + BlockData.NormalizedTextureSize, y));
            uvs.Add(new float2(x + BlockData.NormalizedTextureSize, y + BlockData.NormalizedTextureSize));
        }
    }
}