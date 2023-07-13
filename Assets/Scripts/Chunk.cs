using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;

    int vertexIndex = 0;
    List<Vector3> vertices = new();
    List<int> triangles = new();
    List<Vector2> uvs = new();

    bool[,,] Map = new bool[Voxel.ChunkWidth, Voxel.ChunkHeight, Voxel.ChunkWidth]; 

    void Start()
    {
        PopulateMap();
        CreateChunkMesh();
        CreateMesh();
    }

    void PopulateMap()
    {
        for (int x = 0; x < Voxel.ChunkWidth; x++)
        {
            for (int z = 0; z < Voxel.ChunkWidth; z++)
            {
                for (int y = 0; y < Voxel.ChunkHeight; y++)
                {
                    Map[x, y, z] = true;
                }
            }
        }
    }

    void CreateChunkMesh()
    {
        for (int x = 0; x < Voxel.ChunkWidth; x++)
        {
            for (int z = 0; z < Voxel.ChunkWidth; z++)
            {
                for (int y = 0; y < Voxel.ChunkHeight; y++)
                {
                    AddVoxelToChunk(new Vector3(x, y, z));
                }
            }
        }
    }

    bool CheckVoxel(Vector3 pos)
    {
        // round coordinates down
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        // if out of bounds
        if (x < 0 || x >= Voxel.ChunkWidth ||
            y < 0 || y >= Voxel.ChunkHeight ||
            z < 0 || z >= Voxel.ChunkWidth)
            return false;

        // return value in map
        return Map[x, y, z];
    }

    void AddVoxelToChunk(Vector3 pos)
    {
        for (int i = 0; i < 6; i++)
        {
            if (CheckVoxel(pos + Voxel.faceCheck[i]))
                continue;

            vertices.Add(pos + Voxel.verts[Voxel.tris[i,0]]);
            vertices.Add(pos + Voxel.verts[Voxel.tris[i,1]]);
            vertices.Add(pos + Voxel.verts[Voxel.tris[i,2]]);
            vertices.Add(pos + Voxel.verts[Voxel.tris[i,3]]);

            uvs.Add(Voxel.uvs[0]);
            uvs.Add(Voxel.uvs[1]);
            uvs.Add(Voxel.uvs[2]);
            uvs.Add(Voxel.uvs[3]);

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 3);

            vertexIndex += 4;
        }
    }

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