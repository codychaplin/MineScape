using Unity.Mathematics;

namespace minescape.structures
{
    public struct Structure
    {
        public byte ID { get; }
        public byte Radius { get; }
        public int3 LocalPosition { get; set; }
        public Type Type { get; }

        public Structure(byte id, byte radius, Type type)
        {
            ID = id;
            Radius = radius;
            Type = type;
            LocalPosition = new();
        }

        public Structure(byte id, byte radius, Type type, int3 localPosition)
        {
            ID = id;
            Radius = radius;
            Type = type;
            LocalPosition = localPosition;
        }
    }

    public enum Type
    {
        Tree,
        Cactus,
        Building
    }
}