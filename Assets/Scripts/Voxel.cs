using UnityEngine;

public static class Voxel
{
    /// <summary>
    /// Stores all 8 corners (vertices) of a 1x1 cube.
    /// </summary>
    public static readonly Vector3[] verts = new Vector3[8]
    {
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 1.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 1.0f),
    };

    /// <summary>
    /// Stores arrays containing 2 triangles that make up the faces of a cube.
    /// Uses indexes of verts to create triangles.
    /// </summary>
    public static readonly int[,] tris = new int[6, 6]
    {
        {0, 3, 1, 1, 3, 2}, // back
		{5, 6, 4, 4, 6, 7}, // front
		{3, 7, 2, 2, 7, 6}, // top
		{1, 5, 0, 0, 5, 4}, // bottom
		{4, 7, 0, 0, 7, 3}, // left
		{1, 2, 5, 5, 2, 6} // right
	};

    /// <summary>
    /// 
    /// </summary>
    public static readonly Vector2[] uvs = new Vector2[6]
    {
        new Vector2 (0.0f, 0.0f),
        new Vector2 (0.0f, 1.0f),
        new Vector2 (1.0f, 0.0f),
        new Vector2 (1.0f, 0.0f),
        new Vector2 (0.0f, 1.0f),
        new Vector2 (1.0f, 1.0f)
    };
}