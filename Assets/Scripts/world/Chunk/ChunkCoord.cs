using System;
using System.Collections.Generic;

namespace minescape.world.chunk
{
    public struct ChunkCoord : IEquatable<ChunkCoord>
    {
        public int x;
        public int z;

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

    struct ChunkCoordComparer : IEqualityComparer<ChunkCoord>
    {
        public bool Equals(ChunkCoord x, ChunkCoord y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(ChunkCoord obj)
        {
            int hash = 17;
            hash = hash * 31 + obj.x.GetHashCode();
            hash = hash * 31 + obj.z.GetHashCode();
            return hash;
        }
    }
}