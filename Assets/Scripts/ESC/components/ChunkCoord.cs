using Unity.Entities;

namespace minescape.ESC.components
{
    public struct ChunkCoord : IComponentData
    {
        public readonly int x;
        public readonly int z;

        public ChunkCoord(int _x, int _z)
        {
            x = _x;
            z = _z;
        }

        public bool Equals(ChunkCoord other)
        {
            return other.x == x && other.z == z;
        }

        public override string ToString()
        {
            return $"Coord: {x},{z}";
        }
    }
}