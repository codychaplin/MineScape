using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using minescape.jobs;
using minescape.lighting;
using minescape.scriptableobjects;
using static UnityEditor.Searcher.SearcherWindow.Alignment;
using System.Drawing;
using UnityEditor.Rendering;

namespace minescape.world.chunk
{
    public class ChunkManager : MonoBehaviour
    {
        public World world;
        public bool renderMap;
        public bool renderChunks;
        public bool calculateLight;

        public NoiseParameters elevation;
        public NoiseParameters relief;
        public NoiseParameters temperature;
        public NoiseParameters humidity;
        public NoiseParameters caves;

        public Dictionary<ChunkCoord, Chunk> Chunks = new();

        public HashSet<ChunkCoord> activeChunks = new();
        public HashSet<ChunkCoord> newActiveChunks = new();

        public Queue<ChunkCoord> CreateChunksQueue = new();
        Queue<ChunkCoord> CreateStructuresQueue = new();
        Queue<ChunkCoord> RemoveSunlightQueue = new();
        Queue<ChunkCoord> CalculateSunlightQueue = new();
        Queue<ChunkCoord> PropagateSunlightQueue = new();
        Queue<ChunkCoord> GenerateMeshDataQueue = new();

        JobHandle previousSetBlocksInChunksHandle = new();
        JobHandle previousGenerateStructuresHandle = new();
        JobHandle previousCalculateSunlightHandle = new();
        JobHandle previousPropagateSunlightHandle = new();
        JobHandle previousGenerateMeshDataHandle = new();

        NativeQueue<FloodFillNode> BfsQueue;
        NativeQueue<FloodFillNode> BfsRemovalQueue;

        public List<MapChunk> MapChunks = new();

        private void Start()
        {
            if (renderMap)
                GenerateMap();

            BfsQueue = new(Allocator.Persistent);
            BfsRemovalQueue = new(Allocator.Persistent);
        }

        void Update()
        {
            if (renderMap)
                return;

            if (CreateChunksQueue.Count > 0 || CreateStructuresQueue.Count > 0 || CalculateSunlightQueue.Count > 0 || PropagateSunlightQueue.Count > 0 || GenerateMeshDataQueue.Count > 0)
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
            Chunk chunk = new(world, coord);
            Chunks.Add(coord, chunk);
            SetBlockDataJob job = new()
            {
                caveScale = caves.scale,
                caveThreshold = caves.lacunarity,
                seed = world.Seed,
                biomes = world.Biomes.biomes,
                structures = world.Structures.structures,
                elevation = world.Splines.Elevation,
                elevationScale = elevation.scale,
                elevationOctaves = elevation.octaves,
                reliefScale = relief.scale,
                reliefOctaves = relief.octaves,
                persistance = elevation.persistance,
                lacunarity = elevation.lacunarity,
                position = new int3(chunk.position.x, 0, chunk.position.z),
                blockMap = chunk.BlockMap,
                biomeMap = chunk.BiomeMap,
                heightMap = chunk.HeightMap,
                structureMap = chunk.Structures,
                isDirty = chunk.isDirty
            };
            return job.Schedule();
        }

        void CreateChunks()
        {
            // set temp job handle lists
            NativeList<JobHandle> SetBlocksInChunkHandles = new(Allocator.Temp);
            NativeList<JobHandle> GenerateStructuresHandles = new(Allocator.Temp);
            NativeList<JobHandle> CalculateSunlightHandles = new(Allocator.Temp);
            NativeList<JobHandle> PropagateSunlightHandles = new(Allocator.Temp);
            NativeList<JobHandle> GenerateMeshDataHandles = new(Allocator.Temp);

            // cache surrounding chunk coords
            ChunkCoordNeighbourhood neighbourhood = new(new ChunkCoord());

            // set job handles
            JobHandle SetBlocksInChunkHandle = new();
            JobHandle GenerateStructuresHandle = new();
            JobHandle CalculateSunlightHandle = new();
            JobHandle PropagateSunlightHandle = new();
            JobHandle GenerateMeshDataHandle = new();

            // generate chunks
            SetBlocksInChunkHandle = GenerateChunks(SetBlocksInChunkHandles, ref neighbourhood);
            SetBlocksInChunkHandles.Dispose();
            previousSetBlocksInChunksHandle = SetBlocksInChunkHandle;

            // generate structures
            GenerateStructuresHandle = GenerateStructures(GenerateStructuresHandles, ref SetBlocksInChunkHandle, ref neighbourhood);
            GenerateStructuresHandles.Dispose();
            previousGenerateStructuresHandle = GenerateStructuresHandle;

            // calculate sunlight
            CalculateSunlightHandle = CalculateSunlight(CalculateSunlightHandles, ref CalculateSunlightHandle, ref GenerateStructuresHandle);
            CalculateSunlightHandles.Dispose();
            previousCalculateSunlightHandle = CalculateSunlightHandle;

            // propagate sunlight
            PropagateSunlightHandle = PropagateSunlight(true, PropagateSunlightHandles, ref PropagateSunlightHandle, ref CalculateSunlightHandle, ref neighbourhood);
            PropagateSunlightHandles.Dispose();
            previousPropagateSunlightHandle = PropagateSunlightHandle;

            // generate mesh data
            previousGenerateMeshDataHandle = GenerateMeshData(GenerateMeshDataHandles, ref GenerateMeshDataHandle, ref PropagateSunlightHandle, ref neighbourhood);
            GenerateMeshDataHandles.Dispose();
        }

        public void AlterBlock(Chunk chunk, int x, int y, int z)
        {
            NativeList<JobHandle> RemoveLightHandles = new(Allocator.Temp);
            NativeList<JobHandle> CalculateSunlightHandles = new(Allocator.Temp);
            NativeList<JobHandle> PropagateSunlightHandles = new(Allocator.Temp);
            NativeList<JobHandle> GenerateMeshDataHandles = new(Allocator.Temp);

            ChunkCoordNeighbourhood neighbourhood = new(new ChunkCoord());

            JobHandle RemoveLightHandle = new();
            JobHandle CalculateSunlightHandle = new();
            JobHandle PropagateSunlightHandle = new();
            JobHandle GenerateMeshDataHandle = new();

            RemoveSunlightQueue.Enqueue(chunk.coord);

            UpdateSurroundingChunks(chunk, neighbourhood, x, y, z);

            // remove light
            RemoveLightHandle = RemoveLight(new int3(x, y, z), RemoveLightHandles, ref neighbourhood);
            RemoveLightHandles.Dispose();

            // calculate sunlight
            CalculateSunlightHandle = CalculateSunlight(CalculateSunlightHandles, ref CalculateSunlightHandle, ref RemoveLightHandle);
            CalculateSunlightHandles.Dispose();

            // propagate sunlight
            PropagateSunlightHandle = PropagateSunlight(false, PropagateSunlightHandles, ref PropagateSunlightHandle, ref RemoveLightHandle, ref neighbourhood);
            PropagateSunlightHandles.Dispose();

            // set mesh data
            GenerateMeshData(GenerateMeshDataHandles, ref GenerateMeshDataHandle, ref PropagateSunlightHandle, ref neighbourhood);
            GenerateMeshDataHandles.Dispose();
        }

        void UpdateSurroundingChunks(Chunk chunk, ChunkCoordNeighbourhood neighbourhood, int x, int y, int z)
        {
            chunk.isDirty.Value = true;
            neighbourhood.SetCenter(chunk.coord.x, chunk.coord.z);
            neighbourhood.SetAllNeighbours();
            neighbourhood.AddNeighboursToQueue(ref GenerateMeshDataQueue);

            if (!Utils.IsBlockInChunk(x, y, z + 1)) // north
            {
                var north = Chunks[neighbourhood.North];
                north.isDirty.Value = true;
            }
            if (!Utils.IsBlockInChunk(x, y, z - 1)) // south
            {
                var south = Chunks[neighbourhood.South];
                south.isDirty.Value = true;
            }
            if (!Utils.IsBlockInChunk(x + 1, y, z)) // east
            {
                var east = Chunks[neighbourhood.East];
                east.isDirty.Value = true;
            }
            if (!Utils.IsBlockInChunk(x - 1, y, z)) // west
            {
                var west = Chunks[neighbourhood.West];
                west.isDirty.Value = true;
            }
        }

        JobHandle GenerateChunks(NativeList<JobHandle> SetBlocksInChunkHandles, ref ChunkCoordNeighbourhood neighbourhood)
        {
            while (CreateChunksQueue.Count > 0)
            {
                // get chunk
                var coord = CreateChunksQueue.Peek();
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
                if (!CreateStructuresQueue.Contains(neighbourhood.Center))
                    CreateStructuresQueue.Enqueue(neighbourhood.Center);
                if (!CreateStructuresQueue.Contains(neighbourhood.North))
                    CreateStructuresQueue.Enqueue(neighbourhood.North);
                if (!CreateStructuresQueue.Contains(neighbourhood.NorthEast))
                    CreateStructuresQueue.Enqueue(neighbourhood.NorthEast);
                if (!CreateStructuresQueue.Contains(neighbourhood.East))
                    CreateStructuresQueue.Enqueue(neighbourhood.East);
                if (!CreateStructuresQueue.Contains(neighbourhood.SouthEast))
                    CreateStructuresQueue.Enqueue(neighbourhood.SouthEast);
                if (!CreateStructuresQueue.Contains(neighbourhood.South))
                    CreateStructuresQueue.Enqueue(neighbourhood.South);
                if (!CreateStructuresQueue.Contains(neighbourhood.SouthWest))
                    CreateStructuresQueue.Enqueue(neighbourhood.SouthWest);
                if (!CreateStructuresQueue.Contains(neighbourhood.West))
                    CreateStructuresQueue.Enqueue(neighbourhood.West);
                if (!CreateStructuresQueue.Contains(neighbourhood.NorthWest))
                    CreateStructuresQueue.Enqueue(neighbourhood.NorthWest);

                // update queues
                CalculateSunlightQueue.Enqueue(neighbourhood.Center);
                CreateChunksQueue.Dequeue();
            }

            // add handles from previous frame's jobs (if any) and combine dependencies
            SetBlocksInChunkHandles.Add(previousSetBlocksInChunksHandle);
            SetBlocksInChunkHandles.Add(previousGenerateStructuresHandle);
            SetBlocksInChunkHandles.Add(previousCalculateSunlightHandle);
            SetBlocksInChunkHandles.Add(previousPropagateSunlightHandle);
            SetBlocksInChunkHandles.Add(previousGenerateMeshDataHandle);
            return JobHandle.CombineDependencies(SetBlocksInChunkHandles);
        }

        JobHandle GenerateStructures(NativeList<JobHandle> GenerateStructuresHandles, ref JobHandle previousHandle, ref ChunkCoordNeighbourhood neighbourhood)
        {
            JobHandle previousDependency = previousHandle;

            NativeArray<byte> tempNorth = new(0, Allocator.Persistent);
            NativeArray<byte> tempNorthEast = new(0, Allocator.Persistent);
            NativeArray<byte> tempEast = new(0, Allocator.Persistent);
            NativeArray<byte> tempSouthEast = new(0, Allocator.Persistent);
            NativeArray<byte> tempSouth = new(0, Allocator.Persistent);
            NativeArray<byte> tempSouthWest = new(0, Allocator.Persistent);
            NativeArray<byte> tempWest = new(0, Allocator.Persistent);
            NativeArray<byte> tempNorthWest = new(0, Allocator.Persistent);

            while (CreateStructuresQueue.Count > 0)
            {
                // get chunk, if already generated, dequeue
                var coord = CreateStructuresQueue.Peek();
                var chunk = Chunks[coord];
                if (chunk.generated)
                {
                    CreateStructuresQueue.Dequeue();
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

                CreateStructuresQueue.Dequeue();
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

        JobHandle CalculateSunlight(NativeList<JobHandle> CalculateSunlightHandles, ref JobHandle CalculateSunlightHandle, ref JobHandle previousHandle)
        {
            while (CalculateSunlightQueue.Count > 0)
            {
                var coord = CalculateSunlightQueue.Peek();
                var chunk = Chunks[coord];

                CalculateSunlightJob calculateSunlightJob = new()
                {
                    run = calculateLight,
                    blockMap = chunk.BlockMap,
                    lightMap = chunk.LightMap
                };
                CalculateSunlightHandles.Add(calculateSunlightJob.Schedule(previousHandle));

                PropagateSunlightQueue.Enqueue(coord);
                CalculateSunlightQueue.Dequeue();
            }

            return JobHandle.CombineDependencies(CalculateSunlightHandles);
        }

        JobHandle PropagateSunlight(bool onGenerate, NativeList<JobHandle> PropagateSunlightHandles, ref JobHandle PropagateSunlightHandle, ref JobHandle previousHandle,
            ref ChunkCoordNeighbourhood neighbourhood)
        {
            PropagateSunlightHandle = previousHandle;
            while (PropagateSunlightQueue.Count > 0)
            {
                var coord = PropagateSunlightQueue.Peek();
                var chunk = Chunks[coord];
                neighbourhood.SetCenter(coord.x, coord.z);
                neighbourhood.SetAllNeighbours();
                var northChunk = Chunks[neighbourhood.North];
                var northEastChunk = Chunks[neighbourhood.NorthEast];
                var eastChunk = Chunks[neighbourhood.East];
                var SouthEastChunk = Chunks[neighbourhood.SouthEast];
                var southChunk = Chunks[neighbourhood.South];
                var southWestChunk = Chunks[neighbourhood.SouthWest];
                var westChunk = Chunks[neighbourhood.West];
                var northWestChunk = Chunks[neighbourhood.NorthWest];

                if (onGenerate)
                {
                    PropagateSunlightOnStartJob propagateSunlightOnStartJob = new()
                    {
                        run = calculateLight,
                        coord = coord,
                        blockMap = chunk.BlockMap,
                        lightMap = chunk.LightMap,
                        northBlockMap = northChunk.BlockMap,
                        northLightMap = northChunk.LightMap,
                        northEastBlockMap = northEastChunk.BlockMap,
                        northEastLightMap = northEastChunk.LightMap,
                        eastBlockMap = eastChunk.BlockMap,
                        eastLightMap = eastChunk.LightMap,
                        southEastBlockMap = SouthEastChunk.BlockMap,
                        southEastLightMap = SouthEastChunk.LightMap,
                        southBlockMap = southChunk.BlockMap,
                        southLightMap = southChunk.LightMap,
                        southWestBlockMap = southWestChunk.BlockMap,
                        southWestLightMap = southWestChunk.LightMap,
                        westBlockMap = westChunk.BlockMap,
                        westLightMap = westChunk.LightMap,
                        northWestBlockMap = northWestChunk.BlockMap,
                        northWestLightMap = northWestChunk.LightMap,
                        bfsQueue = BfsQueue,
                        onGenerate = onGenerate
                    };
                    PropagateSunlightHandle = propagateSunlightOnStartJob.Schedule(PropagateSunlightHandle);
                    PropagateSunlightHandles.Add(PropagateSunlightHandle);

                    GenerateMeshDataQueue.Enqueue(coord);
                    PropagateSunlightQueue.Dequeue();
                }
                else
                {
                    PropagateSunlightJob propagateSunlightJob = new()
                    {
                        run = calculateLight,
                        coord = coord,
                        blockMap = chunk.BlockMap,
                        lightMap = chunk.LightMap,
                        northBlockMap = northChunk.BlockMap,
                        northLightMap = northChunk.LightMap,
                        northIsDirty = northChunk.isDirty,
                        northEastBlockMap = northEastChunk.BlockMap,
                        northEastLightMap = northEastChunk.LightMap,
                        northEastIsDirty = northEastChunk.isDirty,
                        eastBlockMap = eastChunk.BlockMap,
                        eastLightMap = eastChunk.LightMap,
                        eastIsDirty = eastChunk.isDirty,
                        southEastBlockMap = SouthEastChunk.BlockMap,
                        southEastLightMap = SouthEastChunk.LightMap,
                        southEastIsDirty = SouthEastChunk.isDirty,
                        southBlockMap = southChunk.BlockMap,
                        southLightMap = southChunk.LightMap,
                        southIsDirty = southChunk.isDirty,
                        southWestBlockMap = southWestChunk.BlockMap,
                        southWestLightMap = southWestChunk.LightMap,
                        southWestIsDirty = southWestChunk.isDirty,
                        westBlockMap = westChunk.BlockMap,
                        westLightMap = westChunk.LightMap,
                        westIsDirty = westChunk.isDirty,
                        northWestBlockMap = northWestChunk.BlockMap,
                        northWestLightMap = northWestChunk.LightMap,
                        northWestIsDirty = northWestChunk.isDirty,
                        bfsQueue = BfsQueue,
                        onGenerate = onGenerate
                    };
                    PropagateSunlightHandle = propagateSunlightJob.Schedule(PropagateSunlightHandle);
                    PropagateSunlightHandles.Add(PropagateSunlightHandle);

                    GenerateMeshDataQueue.Enqueue(coord);
                    PropagateSunlightQueue.Dequeue();
                }
            }

            return JobHandle.CombineDependencies(PropagateSunlightHandles);
        }

        JobHandle RemoveLight(int3 position, NativeList<JobHandle> RemoveLightHandles, ref ChunkCoordNeighbourhood neighbourhood)
        {
            while (RemoveSunlightQueue.Count > 0)
            {
                var coord = RemoveSunlightQueue.Peek();
                var chunk = Chunks[coord];
                neighbourhood.SetCenter(coord.x, coord.z);
                neighbourhood.SetAllNeighbours();
                var northChunk = Chunks[neighbourhood.North];
                var northEastChunk = Chunks[neighbourhood.NorthEast];
                var eastChunk = Chunks[neighbourhood.East];
                var SouthEastChunk = Chunks[neighbourhood.SouthEast];
                var southChunk = Chunks[neighbourhood.South];
                var southWestChunk = Chunks[neighbourhood.SouthWest];
                var westChunk = Chunks[neighbourhood.West];
                var northWestChunk = Chunks[neighbourhood.NorthWest];

                RemoveLightJob removeLightJob = new()
                {
                    run = calculateLight,
                    position = position,
                    blockMap = chunk.BlockMap,
                    lightMap = chunk.LightMap,
                    northMap = northChunk.LightMap,
                    northIsDirty = northChunk.isDirty,
                    northEastMap = northEastChunk.LightMap,
                    northEastIsDirty = northEastChunk.isDirty,
                    eastMap = eastChunk.LightMap,
                    eastIsDirty = eastChunk.isDirty,
                    southEastMap = SouthEastChunk.LightMap,
                    southEastIsDirty = SouthEastChunk.isDirty,
                    southMap = southChunk.LightMap,
                    southIsDirty = southChunk.isDirty,
                    southWestMap = southWestChunk.LightMap,
                    southWestIsDirty = southWestChunk.isDirty,
                    westMap = westChunk.LightMap,
                    westIsDirty = westChunk.isDirty,
                    northWestMap = northWestChunk.LightMap,
                    northWestIsDirty = northWestChunk.isDirty,
                    bfsRemovalQueue = BfsRemovalQueue,
                    bfsQueue = BfsQueue
                };
                RemoveLightHandles.Add(removeLightJob.Schedule(previousGenerateMeshDataHandle));

                PropagateSunlightQueue.Enqueue(coord);
                RemoveSunlightQueue.Dequeue();
            }

            return JobHandle.CombineDependencies(RemoveLightHandles);
        }

        JobHandle GenerateMeshData(NativeList<JobHandle> GenerateMeshDataHandles, ref JobHandle GenerateMeshDataHandle,
            ref JobHandle previousHandle, ref ChunkCoordNeighbourhood neighbourhood)
        {
            while (GenerateMeshDataQueue.Count > 0)
            {
                var coord = GenerateMeshDataQueue.Peek();
                var chunk = GetChunkForMeshGen(coord);
                neighbourhood.SetCenter(coord.x, coord.z);
                neighbourhood.SetAdjacentNeighbours();
                var northChunk = GetChunkForMeshGen(neighbourhood.North);
                var eastChunk = GetChunkForMeshGen(neighbourhood.East);
                var southChunk = GetChunkForMeshGen(neighbourhood.South);
                var westChunk = GetChunkForMeshGen(neighbourhood.West);

                GenerateMeshDataJob generateMeshDataJob = new()
                {
                    useLight = calculateLight,
                    coord = chunk.coord,
                    position = new int3(chunk.position.x, 0, chunk.position.z),
                    blocks = world.Blocks.blocks,
                    biomes = world.Biomes.biomes,
                    blockMap = chunk.BlockMap,
                    lightMap = chunk.LightMap,
                    biomeMap = chunk.BiomeMap,
                    northBlockMap = northChunk.BlockMap,
                    eastBlockMap = eastChunk.BlockMap,
                    southBlockMap = southChunk.BlockMap,
                    westBlockMap = westChunk.BlockMap,
                    northLightMap = northChunk.LightMap,
                    eastLightMap = eastChunk.LightMap,
                    southLightMap = southChunk.LightMap,
                    westLightMap = westChunk.LightMap,
                    triangles = chunk.triangles,
                    transparentTriangles = chunk.transparentTriangles,
                    plantTriangles = chunk.plantTriangles,
                    vertices = chunk.vertices,
                    normals = chunk.normals,
                    colours = chunk.colours,
                    uvData = chunk.uvData,
                    plantHitboxVertices = chunk.plantHitboxVertices,
                    plantHitboxTriangles = chunk.plantHitboxTriangles,
                    isDirty = chunk.isDirty,
                    vertexIndex = 0,
                    hitboxVertexIndex = 0,
                };

                // render chunk
                GenerateMeshDataHandle = generateMeshDataJob.Schedule(previousHandle);
                GenerateMeshDataHandles.Add(GenerateMeshDataHandle);
                StartCoroutine(RenderChunk(GenerateMeshDataHandle, chunk));
                GenerateMeshDataQueue.Dequeue();
            }

            return JobHandle.CombineDependencies(GenerateMeshDataHandles);
        }

        Chunk GetChunkForMeshGen(ChunkCoord coord)
        {
            var chunk = Chunks[coord];
            chunk.InitializeMeshCollections();
            return chunk;
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

        public void Dispose()
        {
            BfsQueue.Dispose();
            BfsRemovalQueue.Dispose();
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