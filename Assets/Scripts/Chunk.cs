using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public ChunkCoord coord;

    GameObject chunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    World world;
    int vertexIndex = 0;
    List<Vector3> vertices = new();
    List<int> triangles = new();
    List<Vector2> uvs = new();

    byte[,,] Map = new byte[Block.ChunkWidth, Block.ChunkHeight, Block.ChunkWidth];

    public bool IsActive
    {
        get { return chunkObject.activeSelf; }
        set { chunkObject.SetActive(value); }
    }

    public Vector3 position { get { return chunkObject.transform.position; } }

    public Chunk(World _world, ChunkCoord _coord)
    {
        world = _world;
        coord = _coord;
        chunkObject = new();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        meshRenderer.material = world.material;
        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = new Vector3(coord.x * Block.ChunkWidth, 0f, coord.z * Block.ChunkWidth);
        chunkObject.name = $"{coord.x},{coord.z}";

        PopulateMap();
        CreateChunk();
        CreateMesh();
    }

    /// <summary>
    /// Populates bool map with block types.
    /// </summary>
    void PopulateMap()
    {
        for (int x = 0; x < Block.ChunkWidth; x++)
        {
            for (int z = 0; z < Block.ChunkWidth; z++)
            {
                for (int y = 0; y < Block.ChunkHeight; y++)
                {
                    Map[x, y, z] = world.GetBlock(new Vector3(x, y, z) + position);
                }
            }
        }
    }

    /// <summary>
    /// Adds voxels to chunk.
    /// </summary>
    void CreateChunk()
    {
        for (int x = 0; x < Block.ChunkWidth; x++)
        {
            for (int z = 0; z < Block.ChunkWidth; z++)
            {
                for (int y = 0; y < Block.ChunkHeight; y++)
                {
                    if (world.BlockTypes[Map[x, y, z]].isSolid)
                        AddBlockToChunk(new Vector3(x, y, z));
                }
            }
        }
    }

    /// <summary>
    /// Checks if voxel is within the bounds of its chunk.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns>Whether voxel is within chunk.</returns>
    bool IsBlockInChunk(int x, int y, int z)
    {
        if (x < 0 || x >= Block.ChunkWidth ||
            y < 0 || y >= Block.ChunkHeight ||
            z < 0 || z >= Block.ChunkWidth)
            return false;
        else
            return true;
    }

    /// <summary>
    /// Gets value from map.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns>Whether block face should be rendered</returns>
    bool CheckBlock(Vector3 pos)
    {
        // round coordinates down
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        // if out of bounds
        if (!IsBlockInChunk(x, y, z))
            return world.BlockTypes[world.GetBlock(pos + position)].isSolid;

        // return value in map
        return world.BlockTypes[Map[x, y, z]].isSolid;
    }

    /// <summary>
    /// Generates exposed block faces.
    /// </summary>
    /// <param name="pos"></param>
    void AddBlockToChunk(Vector3 pos)
    {
        for (int i = 0; i < 6; i++)
        {
            if (CheckBlock(pos + Block.faceCheck[i]))
                continue;

            vertices.Add(pos + Block.verts[Block.tris[i,0]]);
            vertices.Add(pos + Block.verts[Block.tris[i,1]]);
            vertices.Add(pos + Block.verts[Block.tris[i,2]]);
            vertices.Add(pos + Block.verts[Block.tris[i,3]]);

            byte blockId = Map[(int)pos.x, (int)pos.y, (int)pos.z];
            AddTexture(world.BlockTypes[blockId].GetTextureId(i));

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 3);

            vertexIndex += 4;
        }
    }

    void AddTexture(int textureId)
    {
        float y = textureId / Block.TextureAtlasSize;
        float x = textureId - (y * Block.TextureAtlasSize);

        x *= Block.NormalizedTextureSize;
        y *= Block.NormalizedTextureSize;

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + Block.NormalizedTextureSize));
        uvs.Add(new Vector2(x + Block.NormalizedTextureSize, y));
        uvs.Add(new Vector2(x + Block.NormalizedTextureSize, y + Block.NormalizedTextureSize));
    }

    /// <summary>
    /// Uses voxel data to create mesh.
    /// </summary>
    void CreateMesh()
    {
        Mesh mesh = new()
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray()
        };
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }
}