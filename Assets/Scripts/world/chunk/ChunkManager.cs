using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using minescape.jobs;
using minescape.scriptableobjects;

namespace minescape.world.chunk
{
    public class ChunkManager : MonoBehaviour
    {
        public World world;
        public Material textureMap;
        public Material transparentTextureMap;
        public bool renderMap;
        public bool renderChunks;

        public NoiseParameters elevation;
        public NoiseParameters relief;
        public NoiseParameters temperature;
        public NoiseParameters humidity;

        public Dictionary<ChunkCoord, Chunk> Chunks = new();

        public HashSet<ChunkCoord> activeChunks = new();
        public HashSet<ChunkCoord> newActiveChunks = new();

        public Queue<ChunkCoord> ChunksToCreate = new();
        Queue<ChunkCoord> ChunksWithStructures = new();
        Queue<ChunkCoord> ChunksToGetBorders = new();
        Queue<ChunkCoord> ChunksToRender = new();

        JobHandle previousSetBlocksInChunksHandle = new();
        JobHandle previousGenerateStructuresHandle = new();
        JobHandle previousGetBorderDataHandle = new();
        JobHandle previousGenerateMeshDataHandle = new();

        public List<MapChunk> MapChunks = new();

        private void Start()
        {
            if (renderMap)
                GenerateMap();
        }

        void Update()
        {
            if (renderMap)
                return;

            if (ChunksToCreate.Count > 0 || ChunksToGetBorders.Count > 0 || ChunksWithStructures.Count > 0 || ChunksToRender.Count > 0)
                CreateChunks();
        }

        /// <summary>
        /// Tries to get chunk if it exists, otherwise returns null.
        /// </summary>
        /// <param name="chunkCoord"></param>
        /// <returns>Chunk or null</returns>
        public Chunk TryGetChunk(ChunkCoord chunkCoord)
        {
            return Chunks.GetValueOrDefault(chunkCoord);
        }

        /// <summary>
        /// Creates a chunk and schedules a job to set the blocks.
        /// </summary>
        /// <param name="coord"></param>
        /// <returns>JobHandle</returns>
        public JobHandle CreateChunk(ChunkCoord coord)
        {
            Chunk chunk = new(textureMap, transparentTextureMap, world.transform, coord);
            Chunks.Add(coord, chunk);
            SetBlockDataJob job = new()
            {
                seed = world.Seed,
                elevationScale = elevation.scale,
                elevationOctaves = elevation.octaves,
                reliefScale = relief.scale,
                reliefOctaves = relief.octaves,
                persistance = elevation.persistance,
                lacunarity = elevation.lacunarity,
                minTerrainheight = elevation.minTerrainHeight,
                maxTerrainheight = elevation.maxTerrainHeight,
                position = new int3(chunk.position.x, 0, chunk.position.z),
                blockMap = chunk.BlockMap,
                biomeMap = chunk.BiomeMap,
                heightMap = chunk.HeightMap,
                structureMap = chunk.Structures
            };
            return job.Schedule();
        }

        void CreateChunks()
        {
            // set temp job handle lists
            NativeList<JobHandle> SetBlocksInChunkHandles = new(Allocator.Temp);
            NativeList<JobHandle> GenerateStructuresHandles = new(Allocator.Temp);
            NativeList<JobHandle> GetBorderDataHandles = new(Allocator.Temp);
            NativeList<JobHandle> GenerateMeshDataHandles = new(Allocator.Temp);

            // cache adjacent chunk coords
            ChunkCoord north = new();
            ChunkCoord south = new();
            ChunkCoord east = new();
            ChunkCoord west = new();

            // set job handles
            JobHandle SetBlocksInChunkHandle = new();
            JobHandle GenerateStructuresHandle = new();
            JobHandle GetBorderDataHandle = new();
            JobHandle GenerateMeshDataHandle = new();

            // set block data
            while (ChunksToCreate.Count > 0)
            {
                // get chunk
                var coord = ChunksToCreate.Peek();
                if (!Chunks.TryGetValue(coord, out var chunk))
                {
                    SetBlocksInChunkHandles.Add(CreateChunk(coord));
                    continue;
                }

                chunk.isProcessing = true;

                // get adjacent chunk coords
                north.x = coord.x; north.z = coord.z + 1;
                south.x = coord.x; south.z = coord.z - 1;
                east.x = coord.x + 1; east.z = coord.z;
                west.x = coord.x - 1; west.z = coord.z;

                // get adjacent chunks
                if (!Chunks.ContainsKey(north))
                    SetBlocksInChunkHandles.Add(CreateChunk(north));
                if (!Chunks.ContainsKey(south))
                    SetBlocksInChunkHandles.Add(CreateChunk(south));
                if (!Chunks.ContainsKey(east))
                    SetBlocksInChunkHandles.Add(CreateChunk(east));
                if (!Chunks.ContainsKey(west))
                    SetBlocksInChunkHandles.Add(CreateChunk(west));

                // add to structures list
                if (!ChunksWithStructures.Contains(coord))
                    ChunksWithStructures.Enqueue(coord);
                if (!ChunksWithStructures.Contains(north))
                    ChunksWithStructures.Enqueue(north);
                if (!ChunksWithStructures.Contains(south))
                    ChunksWithStructures.Enqueue(south);
                if (!ChunksWithStructures.Contains(east))
                    ChunksWithStructures.Enqueue(east);
                if (!ChunksWithStructures.Contains(west))
                    ChunksWithStructures.Enqueue(west);

                // update queues
                ChunksToGetBorders.Enqueue(coord);
                ChunksToCreate.Dequeue();
            }
            SetBlocksInChunkHandles.Add(previousSetBlocksInChunksHandle);
            SetBlocksInChunkHandles.Add(previousGenerateStructuresHandle);
            SetBlocksInChunkHandles.Add(previousGetBorderDataHandle);
            SetBlocksInChunkHandles.Add(previousGenerateMeshDataHandle);
            SetBlocksInChunkHandle = JobHandle.CombineDependencies(SetBlocksInChunkHandles);
            SetBlocksInChunkHandles.Dispose();
            previousSetBlocksInChunksHandle = SetBlocksInChunkHandle;

            // generate structures
            while (ChunksWithStructures.Count > 0)
            {
                var coord = ChunksWithStructures.Peek();
                var chunk = Chunks[coord];
                if (chunk.generated)
                {
                    ChunksWithStructures.Dequeue();
                    continue;
                }

                GenerateStructuresJob generateStructuresJob = new()
                {
                    structures = chunk.Structures,
                    blockMap = chunk.BlockMap
                };
                GenerateStructuresHandles.Add(generateStructuresJob.Schedule(SetBlocksInChunkHandle));

                ChunksWithStructures.Dequeue();
            }
            GenerateStructuresHandle = JobHandle.CombineDependencies(GenerateStructuresHandles);
            GenerateStructuresHandles.Dispose();
            previousGenerateStructuresHandle = GenerateStructuresHandle;

            // set border data
            while (ChunksToGetBorders.Count > 0)
            {
                // get chunk
                var coord = ChunksToGetBorders.Peek();
                var chunk = Chunks[coord];

                // get adjacent chunks
                north.x = coord.x; north.z = coord.z + 1;
                south.x = coord.x; south.z = coord.z - 1;
                east.x = coord.x + 1; east.z = coord.z;
                west.x = coord.x - 1; west.z = coord.z;
                var northChunk = Chunks[north];
                var southChunk = Chunks[south];
                var eastChunk = Chunks[east];
                var westChunk = Chunks[west];

                GetBorderDataJob getBorderDataJob = new()
                {
                    chunk = chunk.BlockMap,
                    northChunk = northChunk.BlockMap,
                    southChunk = southChunk.BlockMap,
                    eastChunk = eastChunk.BlockMap,
                    westChunk = westChunk.BlockMap,
                    northFace = chunk.NorthFace,
                    southFace = chunk.SouthFace,
                    eastFace = chunk.EastFace,
                    westFace = chunk.WestFace,
                };
                GetBorderDataHandles.Add(getBorderDataJob.Schedule(GenerateStructuresHandle));

                // update queues
                ChunksToRender.Enqueue(coord);
                ChunksToGetBorders.Dequeue();
            }
            GetBorderDataHandle = JobHandle.CombineDependencies(GetBorderDataHandles);
            GetBorderDataHandles.Dispose();
            previousGetBorderDataHandle = GetBorderDataHandle;

            // set mesh data
            while (ChunksToRender.Count > 0)
            {
                // get chunk
                var coord = ChunksToRender.Peek();
                var chunk = Chunks[coord];

                // generate mesh
                GenerateMeshDataJob generateMeshDataJob = new()
                {
                    coord = chunk.coord,
                    position = new int3(chunk.position.x, 0, chunk.position.z),
                    map = chunk.BlockMap,
                    northFace = chunk.NorthFace,
                    southFace = chunk.SouthFace,
                    eastFace = chunk.EastFace,
                    westFace = chunk.WestFace,
                    vertices = chunk.vertices,
                    normals = chunk.normals,
                    triangles = chunk.triangles,
                    transparentTriangles = chunk.transparentTriangles,
                    colors = chunk.colors,
                    uvs = chunk.uvs,
                    vertexIndex = 0
                };

                // render chunk
                GenerateMeshDataHandle = generateMeshDataJob.Schedule(GetBorderDataHandle);
                GenerateMeshDataHandles.Add(GenerateMeshDataHandle);
                StartCoroutine(RenderChunk(GenerateMeshDataHandle, chunk));
                ChunksToRender.Dequeue();
            }
            previousGenerateMeshDataHandle = JobHandle.CombineDependencies(GenerateMeshDataHandles);
            GenerateMeshDataHandles.Dispose();
        }

        IEnumerator RenderChunk(JobHandle dependency, Chunk chunk)
        {
            while (!dependency.IsCompleted)
            {
                yield return null;
            }

            dependency.Complete();
            chunk.RenderChunk();

            if (chunk.coord.x - world.playerChunkCoord.x >= Constants.ViewDistance || chunk.coord.z - world.playerChunkCoord.z >= Constants.ViewDistance)
            {
                chunk.IsActive = false;
            }
        }

        public MapChunk GetMapChunk(ChunkCoord chunkCoord)
        {
            return MapChunks.FirstOrDefault(c => c.coord.Equals(chunkCoord));
        }

        public void GenerateMap()
        {
            int index = 0;
            int length = Constants.WorldSizeInMapChunks * Constants.WorldSizeInMapChunks;
            NativeArray<JobHandle> handles = new(length, Allocator.TempJob);
            for (int x = 0; x < Constants.WorldSizeInMapChunks; x++)
                for (int z = 0; z < Constants.WorldSizeInMapChunks; z++)
                {
                    MapChunk mapChunk = new(new ChunkCoord(x, z));
                    SetMapBlockDataJob job = new()
                    {
                        seed = world.Seed,
                        elevationScale = elevation.scale,
                        elevationOctaves = elevation.octaves,
                        reliefScale = relief.scale,
                        reliefOctaves = relief.octaves,
                        persistance = elevation.persistance,
                        lacunarity = elevation.lacunarity,
                        position = new int2(mapChunk.position.x, mapChunk.position.y),
                        biomeMap = mapChunk.BlockMap
                    };
                    handles[index++] = job.Schedule();
                    MapChunks.Add(mapChunk);
                }
            var dependency = JobHandle.CombineDependencies(handles);
            handles.Dispose();
            StartCoroutine(ConvertMapToPng(dependency));
        }

        IEnumerator ConvertMapToPng(JobHandle dependency)
        {
            while (!dependency.IsCompleted)
            {
                yield return null;
            }

            dependency.Complete();
            ConvertMapToPng();
        }

        void ConvertMapToPng()
        {
            /*ColorUtility.TryParseHtmlString("#80b497", out var tundra);
            ColorUtility.TryParseHtmlString("#91bd59", out var plains);
            ColorUtility.TryParseHtmlString("#bfb755", out var savanna);
            ColorUtility.TryParseHtmlString("#f5b352", out var desert);
            ColorUtility.TryParseHtmlString("#8ab689", out var borealForest);
            ColorUtility.TryParseHtmlString("#86b783", out var taiga);
            ColorUtility.TryParseHtmlString("#88bb67", out var shrubland);
            ColorUtility.TryParseHtmlString("#79c05a", out var temperateForest);
            ColorUtility.TryParseHtmlString("#6a7039", out var swamp);
            ColorUtility.TryParseHtmlString("#507a32", out var seasonalForest);
            ColorUtility.TryParseHtmlString("#59c93c", out var tropicalForest);
            ColorUtility.TryParseHtmlString("#decea2", out var beach);
            ColorUtility.TryParseHtmlString("#0a2d9b", out var coldOcean);
            ColorUtility.TryParseHtmlString("#0a4f9b", out var ocean);
            ColorUtility.TryParseHtmlString("#0a7a9b", out var warmOcean);*/
            ColorUtility.TryParseHtmlString("#c6def1", out var tundra);
            ColorUtility.TryParseHtmlString("#83e377", out var plains);
            ColorUtility.TryParseHtmlString("#f1c453", out var savanna);
            ColorUtility.TryParseHtmlString("#f29e4c", out var desert);
            ColorUtility.TryParseHtmlString("#2c699a", out var borealForest);
            ColorUtility.TryParseHtmlString("#048ba8", out var taiga);
            ColorUtility.TryParseHtmlString("#0db39e", out var shrubland);
            ColorUtility.TryParseHtmlString("#b9e769", out var temperateForest);
            ColorUtility.TryParseHtmlString("#16db93", out var swamp);
            ColorUtility.TryParseHtmlString("#208b3a", out var seasonalForest);
            ColorUtility.TryParseHtmlString("#155d27", out var tropicalForest);
            ColorUtility.TryParseHtmlString("#decea2", out var beach);
            ColorUtility.TryParseHtmlString("#0a2d9b", out var coldOcean);
            ColorUtility.TryParseHtmlString("#0a4f9b", out var ocean);
            ColorUtility.TryParseHtmlString("#0a7a9b", out var warmOcean);
            Dictionary<byte, Color32> biomes = new()
            {
                { 0, tundra },
                { 1, plains },
                { 2, savanna },
                { 3, desert },
                { 4, borealForest },
                { 5, taiga },
                { 6, shrubland },
                { 7, temperateForest },
                { 8, swamp },
                { 9, seasonalForest },
                { 10, tropicalForest },
                { 11, beach },
                { 12, coldOcean },
                { 13, ocean },
                { 14, warmOcean }
            };

            string path = $"Assets/Resources/Textures/map.png";
            Texture2D texture = new(Constants.WorldSizeInMapBlocks, Constants.WorldSizeInMapBlocks);

            for (int x = 0; x < Constants.WorldSizeInMapChunks; x++)
            {
                for (int y = 0; y < Constants.WorldSizeInMapChunks; y++)
                {
                    MapChunk mapChunk = GetMapChunk(new ChunkCoord(x, y));
                    int offsetX = x * Constants.MapChunkWidth;
                    int offsetY = y * Constants.MapChunkWidth;
                    for (int chunkX = 0; chunkX < Constants.MapChunkWidth; chunkX++)
                        for (int chunkY = 0; chunkY < Constants.MapChunkWidth; chunkY++)
                        {
                            int index = MapChunk.ConvertToIndex(chunkX, chunkY);
                            var block = mapChunk.BlockMap[index];
                            texture.SetPixel(chunkX + offsetX, chunkY + offsetY, biomes[block]);
                        }
                }
            }

            texture.Apply();
            world.image.texture = texture;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            byte[] bytes = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);
        }
    }
}