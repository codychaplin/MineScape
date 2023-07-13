using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;

    void Start()
    {
        int vertexIndex = 0;
        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector2> uvs = new();

        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                int triangleIndex = Voxel.tris[i, j];
                vertices.Add(Voxel.verts[triangleIndex]);
                triangles.Add(vertexIndex);
                uvs.Add(Voxel.uvs[j]);
                vertexIndex++;
            }
        }

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