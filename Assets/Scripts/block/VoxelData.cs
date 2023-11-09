using Unity.Mathematics;

namespace minescape.block
{
    public static class VoxelData
    {
        /// <summary>
        /// Stores all 8 corners (vertices) of a 1x1 cube.
        /// </summary>
        public static readonly float3[] verts = new float3[8]
        {
            new float3(0.0f, 0.0f, 0.0f), // 0
            new float3(1.0f, 0.0f, 0.0f), // 1
            new float3(1.0f, 1.0f, 0.0f), // 2
            new float3(0.0f, 1.0f, 0.0f), // 3
            new float3(0.0f, 0.0f, 1.0f), // 4
            new float3(1.0f, 0.0f, 1.0f), // 5
            new float3(1.0f, 1.0f, 1.0f), // 6
            new float3(0.0f, 1.0f, 1.0f)  // 7
        };

        /// <summary>
        /// Stores arrays containing 2 triangles that make up the faces of a cube.
        /// Uses indexes of verts to create triangles.
        /// </summary>
        public static readonly int[] tris = new int[24] // int[6,4]
        { 0, 3, 1, 2,
          5, 6, 4, 7,
          3, 7, 2, 6,
          1, 5, 0, 4,
          4, 7, 0, 3,
          1, 2, 5, 6 };

        /// <summary>
        /// Stores 2 sets of triangles for plants.
        /// </summary>
        public static readonly int[] plantTris = new int[8] // int[2,4]
        { 0, 3, 5, 6,
          4, 7, 1, 2 };

        public static readonly int[] packedNormals = new int[6]
       {
            3, // back 00 00 11 (0,0,-1)
            1, // front 00 00 01 (0,0,1)
            4, // top 00 01 00 (0,1,0)
            12, // bottom 00 11 00 (0,-1,0)
            48, // left 11 00 00 (-1,0,0)
            16, // right 01 00 00 (1,0,0)
       };

        /// <summary>
        /// Stores array representing the direction UVs are created on a block face.
        /// </summary>
        public static readonly float2[] uvs = new float2[4]
        {
            // bottom-left -> top-left -> bottom-right
            new float2(0.0f, 0.0f),
            new float2(0.0f, 1.0f),
            new float2(1.0f, 0.0f),
            new float2(1.0f, 1.0f)
        };

        /// <summary>
        /// Used to get surrounding voxel coordinates.
        /// </summary>
        public static readonly int3[] faceCheck = new int3[6]
        {
            new int3(0, 0, -1), // back
            new int3(0, 0, 1), // front
            new int3(0, 1, 0), // top
            new int3(0, -1, 0), // bottom
            new int3(-1, 0, 0), // left
            new int3(1, 0, 0), // right
        };

        /// <summary>
        /// Used to get surrounding voxel coordinates.
        /// </summary>
        public static readonly int3[] plantFaceCheck = new int3[2]
        {
            new int3(1, 0, -1), // back
            new int3(-1, 0, 1), // front
        };
    }
}