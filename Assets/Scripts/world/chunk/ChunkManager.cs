using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using minescape.jobs;
using minescape.scriptableobjects;
using NUnit.Framework.Internal;
using UnityEditor.Experimental.GraphView;

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

            // cache surrounding chunk coords
            ChunkCoordNeighbourhood neighbourhood = new(new ChunkCoord());

            // set job handles
            JobHandle SetBlocksInChunkHandle = new();
            JobHandle GenerateStructuresHandle = new();
            JobHandle GetBorderDataHandle = new();
            JobHandle GenerateMeshDataHandle = new();

            // generate chunks
            SetBlocksInChunkHandle = GenerateChunks(SetBlocksInChunkHandles, ref neighbourhood);
            SetBlocksInChunkHandles.Dispose();
            previousSetBlocksInChunksHandle = SetBlocksInChunkHandle;

            // generate structures
            GenerateStructuresHandle = GenerateStructures(GenerateStructuresHandles, ref SetBlocksInChunkHandle, ref neighbourhood);
            GenerateStructuresHandles.Dispose();
            previousGenerateStructuresHandle = GenerateStructuresHandle;

            // set border data
            GetBorderDataHandle = GetBorderData(GetBorderDataHandles, ref GenerateStructuresHandle, ref neighbourhood);
            GetBorderDataHandles.Dispose();
            previousGetBorderDataHandle = GetBorderDataHandle;

            // set mesh datas
            previousGenerateMeshDataHandle = GenerateMeshData(GenerateMeshDataHandles, ref GenerateMeshDataHandle, ref GetBorderDataHandle);
            GenerateMeshDataHandles.Dispose();
        }

        JobHandle GenerateChunks(NativeList<JobHandle> SetBlocksInChunkHandles, ref ChunkCoordNeighbourhood neighbourhood)
        {
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

                // mark chunk as incomplete
                chunk.isProcessing = true;

                // get surrounding chunk coords
                neighbourhood.SetCenter(coord.x, coord.z);
                neighbourhood.SetAllNeighbours();

                // get surrounding chunks
                if (!Chunks.ContainsKey(neighbourhood.North))
                    SetBlocksInChunkHandles.Add(CreateChunk(neighbourhood.North));
                if (!Chunks.ContainsKey(neighbourhood.NorthEast))
                    SetBlocksInChunkHandles.Add(CreateChunk(neighbourhood.NorthEast));
                if (!Chunks.ContainsKey(neighbourhood.East))
                    SetBlocksInChunkHandles.Add(CreateChunk(neighbourhood.East));
                if (!Chunks.ContainsKey(neighbourhood.SouthEast))
                    SetBlocksInChunkHandles.Add(CreateChunk(neighbourhood.SouthEast));
                if (!Chunks.ContainsKey(neighbourhood.South))
                    SetBlocksInChunkHandles.Add(CreateChunk(neighbourhood.South));
                if (!Chunks.ContainsKey(neighbourhood.SouthWest))
                    SetBlocksInChunkHandles.Add(CreateChunk(neighbourhood.SouthWest));
                if (!Chunks.ContainsKey(neighbourhood.West))
                    SetBlocksInChunkHandles.Add(CreateChunk(neighbourhood.West));
                if (!Chunks.ContainsKey(neighbourhood.NorthWest))
                    SetBlocksInChunkHandles.Add(CreateChunk(neighbourhood.NorthWest));

                // add to structures list
                if (!ChunksWithStructures.Contains(neighbourhood.Center))
                    ChunksWithStructures.Enqueue(neighbourhood.Center);
                if (!ChunksWithStructures.Contains(neighbourhood.North))
                    ChunksWithStructures.Enqueue(neighbourhood.North);
                if (!ChunksWithStructures.Contains(neighbourhood.NorthEast))
                    ChunksWithStructures.Enqueue(neighbourhood.NorthEast);
                if (!ChunksWithStructures.Contains(neighbourhood.East))
                    ChunksWithStructures.Enqueue(neighbourhood.East);
                if (!ChunksWithStructures.Contains(neighbourhood.SouthEast))
                    ChunksWithStructures.Enqueue(neighbourhood.SouthEast);
                if (!ChunksWithStructures.Contains(neighbourhood.South))
                    ChunksWithStructures.Enqueue(neighbourhood.South);
                if (!ChunksWithStructures.Contains(neighbourhood.SouthWest))
                    ChunksWithStructures.Enqueue(neighbourhood.SouthWest);
                if (!ChunksWithStructures.Contains(neighbourhood.West))
                    ChunksWithStructures.Enqueue(neighbourhood.West);
                if (!ChunksWithStructures.Contains(neighbourhood.NorthWest))
                    ChunksWithStructures.Enqueue(neighbourhood.NorthWest);

                // update queues
                ChunksToGetBorders.Enqueue(neighbourhood.Center);
                ChunksToCreate.Dequeue();
            }

            SetBlocksInChunkHandles.Add(previousSetBlocksInChunksHandle);
            SetBlocksInChunkHandles.Add(previousGenerateStructuresHandle);
            SetBlocksInChunkHandles.Add(previousGetBorderDataHandle);
            SetBlocksInChunkHandles.Add(previousGenerateMeshDataHandle);
            return JobHandle.CombineDependencies(SetBlocksInChunkHandles);
        }

        JobHandle GenerateStructures(NativeList<JobHandle> GenerateStructuresHandles, ref JobHandle SetBlocksInChunkHandle, ref ChunkCoordNeighbourhood neighbourhood)
        {
            JobHandle previousDependency = SetBlocksInChunkHandle;

            NativeArray<byte> tempNorth = new(0, Allocator.TempJob);
            NativeArray<byte> tempNorthEast = new(0, Allocator.TempJob);
            NativeArray<byte> tempEast = new(0, Allocator.TempJob);
            NativeArray<byte> tempSouthEast = new(0, Allocator.TempJob);
            NativeArray<byte> tempSouth = new(0, Allocator.TempJob);
            NativeArray<byte> tempSouthWest = new(0, Allocator.TempJob);
            NativeArray<byte> tempWest = new(0, Allocator.TempJob);
            NativeArray<byte> tempNorthWest = new(0, Allocator.TempJob);

            while (ChunksWithStructures.Count > 0)
            {
                // get chunk, if already generated, dequeue
                var coord = ChunksWithStructures.Peek();
                var chunk = Chunks[coord];
                if (chunk.generated)
                {
                    ChunksWithStructures.Dequeue();
                    continue;
                }

                // get surrounding chunk coords
                neighbourhood.SetCenter(coord.x, coord.z);
                neighbourhood.SetAllNeighbours();

                // try get surrounding chunks
                Chunks.TryGetValue(neighbourhood.North, out var northChunk);
                Chunks.TryGetValue(neighbourhood.NorthEast, out var northEastChunk);
                Chunks.TryGetValue(neighbourhood.East, out var eastChunk);
                Chunks.TryGetValue(neighbourhood.SouthEast, out var southEastChunk);
                Chunks.TryGetValue(neighbourhood.South, out var southChunk);
                Chunks.TryGetValue(neighbourhood.SouthWest, out var southWestChunk);
                Chunks.TryGetValue(neighbourhood.West, out var westChunk);
                Chunks.TryGetValue(neighbourhood.NorthWest, out var northWestChunk);

                // if chunk is null, assign empty NativeArray<byte> to maps
                GenerateStructuresJob generateStructuresJob = new()
                {
                    structures = chunk.Structures,
                    blockMap = chunk.BlockMap,
                    northMap = northChunk?.BlockMap ?? tempNorth,
                    northEastMap = northEastChunk?.BlockMap ?? tempNorthEast,
                    eastMap = eastChunk?.BlockMap ?? tempEast,
                    southEastMap = southEastChunk?.BlockMap ?? tempSouthEast,
                    southMap = southChunk?.BlockMap ?? tempSouth,
                    southWestMap = southWestChunk?.BlockMap ?? tempSouthWest,
                    westMap = westChunk?.BlockMap ?? tempWest,
                    northWestMap = northWestChunk?.BlockMap ?? tempNorthWest
                };
                var newDependency = generateStructuresJob.Schedule(previousDependency);
                GenerateStructuresHandles.Add(newDependency);
                previousDependency = newDependency;

                ChunksWithStructures.Dequeue();
            }

            var GenerateStructuresHandle = JobHandle.CombineDependencies(GenerateStructuresHandles);

            // simply deallocates temp arrays after structure jobs complete
            DeallocateFillerJob job = new()
            {
                tempNorth = tempNorth,
                tempNorthEast = tempNorthEast,
                tempEast = tempEast,
                tempSouthEast = tempSouthEast,
                tempSouth = tempSouth,
                tempSouthWest = tempSouthWest,
                tempWest = tempWest,
                tempNorthWest = tempNorthWest
            };
            job.Schedule(GenerateStructuresHandle);

            return GenerateStructuresHandle;
        }

        JobHandle GetBorderData(NativeList<JobHandle> GetBorderDataHandles, ref JobHandle GenerateStructuresHandle, ref ChunkCoordNeighbourhood neighbourhood)
        {
            while (ChunksToGetBorders.Count > 0)
            {
                // get chunk
                var coord = ChunksToGetBorders.Peek();
                var chunk = Chunks[coord];
                neighbourhood.SetCenter(coord.x, coord.z);
                neighbourhood.SetAdjacentNeighbours();

                var northChunk = Chunks[neighbourhood.North];
                var eastChunk = Chunks[neighbourhood.East];
                var southChunk = Chunks[neighbourhood.South];
                var westChunk = Chunks[neighbourhood.West];

                GetBorderDataJob getBorderDataJob = new()
                {
                    chunk = chunk.BlockMap,
                    northChunk = northChunk.BlockMap,
                    eastChunk = eastChunk.BlockMap,
                    southChunk = southChunk.BlockMap,
                    westChunk = westChunk.BlockMap,
                    northFace = chunk.NorthFace,
                    eastFace = chunk.EastFace,
                    southFace = chunk.SouthFace,
                    westFace = chunk.WestFace,
                };
                GetBorderDataHandles.Add(getBorderDataJob.Schedule(GenerateStructuresHandle));

                // update queues
                ChunksToRender.Enqueue(coord);
                ChunksToGetBorders.Dequeue();
            }
            return JobHandle.CombineDependencies(GetBorderDataHandles);
        }

        JobHandle GenerateMeshData(NativeList<JobHandle> GenerateMeshDataHandles, ref JobHandle GenerateMeshDataHandle, ref JobHandle GetBorderDataHandle)
        {
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
                    eastFace = chunk.EastFace,
                    southFace = chunk.SouthFace,
                    westFace = chunk.WestFace,
                    vertices = chunk.vertices,
                    normals = chunk.normals,
                    triangles = chunk.triangles,
                    transparentTriangles = chunk.transparentTriangles,
                    uvs = chunk.uvs,
                    vertexIndex = 0
                };

                // render chunk
                GenerateMeshDataHandle = generateMeshDataJob.Schedule(GetBorderDataHandle);
                GenerateMeshDataHandles.Add(GenerateMeshDataHandle);
                StartCoroutine(RenderChunk(GenerateMeshDataHandle, chunk));
                ChunksToRender.Dequeue();
            }

            return JobHandle.CombineDependencies(GenerateMeshDataHandles);
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