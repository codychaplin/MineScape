using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.UI;
using Unity.Mathematics;
using Unity.Collections;
using minescape;
using minescape.jobs;
using minescape.world.chunk;
using minescape.scriptableobjects;

public class NoiseTest : MonoBehaviour
{
    public RawImage image;
    public NoiseParameters elevation;
    public NoiseParameters relief;
    public NoiseParameters topography;
    public List<MapChunk> chunks = new();

    void Start()
    {
        GenerateMap();
    }

    void OnApplicationQuit()
    {
        foreach (var mapChunk in chunks)
        {
            mapChunk.Dispose();
        }
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
                NoiseJob job = new()
                {
                    elevationScale = elevation.scale,
                    elevationOctaves = elevation.octaves,
                    reliefScale = relief.scale,
                    reliefOctaves = relief.octaves,
                    topographyScale = topography.scale,
                    topographyOctaves = topography.octaves,
                    persistance = elevation.persistance,
                    lacunarity = elevation.lacunarity,
                    position = new int2(mapChunk.position.x, mapChunk.position.y),
                    map = mapChunk.BlockMap
                };
                handles[index++] = job.Schedule();
                chunks.Add(mapChunk);
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

    public MapChunk GetChunk(ChunkCoord chunkCoord)
    {
        return chunks.FirstOrDefault(c => c.coord.Equals(chunkCoord));
    }

    void ConvertMapToPng()
    {
        string path = $"Assets/Resources/Textures/test.png";
        Texture2D texture = new(Constants.WorldSizeInMapBlocks, Constants.WorldSizeInMapBlocks);

        for (int x = 0; x < Constants.WorldSizeInMapChunks; x++)
        {
            for (int y = 0; y < Constants.WorldSizeInMapChunks; y++)
            {
                MapChunk chunk = GetChunk(new ChunkCoord(x, y));
                int offsetX = x * Constants.MapChunkWidth;
                int offsetY = y * Constants.MapChunkWidth;
                for (int chunkX = 0; chunkX < Constants.MapChunkWidth; chunkX++)
                    for (int chunkY = 0; chunkY < Constants.MapChunkWidth; chunkY++)
                    {
                        int index = MapChunk.ConvertToIndex(chunkX, chunkY);
                        var block = chunk.BlockMap[index];
                        Color32 colour = new(block, block, block, 255);
                        texture.SetPixel(chunkX + offsetX, chunkY + offsetY, colour);
                    }
            }
        }

        texture.Apply();
        image.texture = texture;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;
        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
    }
}
