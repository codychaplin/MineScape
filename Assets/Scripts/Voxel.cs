using UnityEngine;

public static class Voxel
{
    public static readonly int ChunkWidth = 16;
    public static readonly int ChunkHeight = 16;

    /// <summary>
    /// Stores all 8 corners (vertices) of a 1x1 cube.
    /// </summary>
    public static readonly Vector3[] verts = new Vector3[8]
    {
        new Vector3(0.0f, 0.0f, 0.0f), // 0
        new Vector3(1.0f, 0.0f, 0.0f), // 1
        new Vector3(1.0f, 1.0f, 0.0f), // 2
        new Vector3(0.0f, 1.0f, 0.0f), // 3
        new Vector3(0.0f, 0.0f, 1.0f), // 4
        new Vector3(1.0f, 0.0f, 1.0f), // 5
        new Vector3(1.0f, 1.0f, 1.0f), // 6
        new Vector3(0.0f, 1.0f, 1.0f)  // 7
    };

    /// <summary>
    /// Stores arrays containing 2 triangles that make up the faces of a cube.
    /// Uses indexes of verts to create triangles.
    /// </summary>
    public static readonly int[,] tris = new int[6, 4]
    {
        {0, 3, 1, 2}, // back
		{5, 6, 4, 7}, // front
		{3, 7, 2, 6}, // top
		{1, 5, 0, 4}, // bottom
		{4, 7, 0, 3}, // left
		{1, 2, 5, 6}  // right
	};

    /// <summary>
    /// Stores array representing the direction UVs are created on a block face.
    /// </summary>
    public static readonly Vector2[] uvs = new Vector2[4]
    {
        // bottom-left -> top-left -> bottom-right
        new Vector2 (0.0f, 0.0f),
        new Vector2 (0.0f, 1.0f),
        new Vector2 (1.0f, 0.0f),
        new Vector2 (1.0f, 1.0f)
    };

    /// <summary>
    /// Used to get surrounding voxel coordinates.
    /// </summary>
    public static readonly Vector3[] faceCheck = new Vector3[6]
    {
        new Vector3(0.0f, 0.0f, -1.0f), // back
        new Vector3(0.0f, 0.0f, 1.0f), // front
        new Vector3(0.0f, 1.0f, 0.0f), // top
        new Vector3(0.0f, -1.0f, 0.0f), // bottom
        new Vector3(-1.0f, 0.0f, 0.0f), // left
        new Vector3(1.0f, 0.0f, 0.0f), // right
    };
}