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
        public NoiseParameters topography;
        public NoiseParameters temperature;
        public NoiseParameters humidity;

        public Dictionary<ChunkCoord, Chunk> Chunks = new();

        public HashSet<ChunkCoord> activeChunks = new();
        public HashSet<ChunkCoord> newActiveChunks = new();
        public Queue<ChunkCoord> ChunksToCreate = new();

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

            if (ChunksToCreate.Count > 0)
                CreateChunks();
        }

        public Chunk GetChunkFromWorldPos(Vector3Int pos)
        {
            var coord = new ChunkCoord(pos.x / Constants.ChunkWidth, pos.z / Constants.ChunkWidth);
            return TryGetChunk(coord);
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
                topographyScale = topography.scale,
                topographyOctaves = topography.octaves,
                persistance = elevation.persistance,
                lacunarity = elevation.lacunarity,
                minTerrainheight = elevation.minTerrainHeight,
                maxTerrainheight = elevation.maxTerrainHeight,
                position = new int3(chunk.position.x, 0, chunk.position.z),
                blockMap = chunk.BlockMap,
                biomeMap = chunk.BiomeMap,
                heightMap = chunk.HeightMap
            };
            return job.Schedule();
        }

        void CreateChunks()
        {
            try
            {
                ChunkCoord north = new();
                ChunkCoord south = new();
                ChunkCoord east = new();
                ChunkCoord west = new();

                JobHandle dependency = new();
                while (ChunksToCreate.Count > 0)
                {
                    // initialize chunk
                    var coord = ChunksToCreate.Peek();
                    if (!Chunks.TryGetValue(coord, out var chunk))
                    {
                        dependency = CreateChunk(coord);
                        continue;
                    }

                    chunk.isProcessing = true;

                    // initialize adjacent chunks
                    north.x = coord.x; north.z = coord.z + 1;
                    south.x = coord.x; south.z = coord.z - 1;
                    east.x = coord.x + 1; east.z = coord.z;
                    west.x = coord.x - 1; west.z = coord.z;

                    NativeList<JobHandle> chunkAndAdjacentChunks = new(5, Allocator.TempJob) { dependency };
                    if (!Chunks.ContainsKey(north))
                        chunkAndAdjacentChunks.Add(CreateChunk(north));
                    if (!Chunks.ContainsKey(south))
                        chunkAndAdjacentChunks.Add(CreateChunk(south));
                    if (!Chunks.ContainsKey(east))
                        chunkAndAdjacentChunks.Add(CreateChunk(east));
                    if (!Chunks.ContainsKey(west))
                        chunkAndAdjacentChunks.Add(CreateChunk(west));

                    var SetBlockInChunkhandle = JobHandle.CombineDependencies(chunkAndAdjacentChunks);
                    chunkAndAdjacentChunks.Dispose();

                    // generate structures

                    // generate mesh data
                    var northChunk = Chunks[north];
                    var southChunk = Chunks[south];
                    var eastChunk = Chunks[east];
                    var westChunk = Chunks[west];

                    GenerateMeshDataJob generateMeshDataJob = new()
                    {
                        coord = chunk.coord,
                        position = new int3(chunk.position.x, 0, chunk.position.z),
                        map = chunk.BlockMap,
                        north = northChunk.BlockMap,
                        south = southChunk.BlockMap,
                        east = eastChunk.BlockMap,
                        west = westChunk.BlockMap,
                        vertices = chunk.vertices,
                        normals = chunk.normals,
                        triangles = chunk.triangles,
                        transparentTriangles = chunk.transparentTriangles,
                        colors = chunk.colors,
                        uvs = chunk.uvs,
                        vertexIndex = 0
                    };

                    // render chunk
                    dependency = generateMeshDataJob.Schedule(SetBlockInChunkhandle);
                    StartCoroutine(RenderChunk(dependency, chunk));
                    ChunksToCreate.Dequeue();
                }
            }
            catch (System.InvalidOperationException)
            {
                return; // suppresses stupid job scheduling error
            }
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

        void ReplaceSurfaceBlocks(Chunk chunk)
        {

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
                        topographyScale = topography.scale,
                        topographyOctaves = topography.octaves,
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