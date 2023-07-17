
public struct ChunkCoord
{
    public int x;
    public int z;

    public ChunkCoord(int _x, int _z)
    {
        x = _x;
        z = _z;
    }

    public bool Equals (ChunkCoord other)
    {
        return other.x == x && other.z == z;
    }
}