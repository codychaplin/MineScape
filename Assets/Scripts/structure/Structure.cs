using Unity.Mathematics;

namespace minescape.structure
{
    public struct Structure
    {
        public byte ID { get; }
        public byte Radius { get; }
        public int3 LocalPosition { get; set; }

        public Structure(byte id, byte radius)
        {
            ID = id;
            Radius = radius;
            LocalPosition = new();
        }

        public Structure(byte id, byte radius, int3 localPosition)
        {
            ID = id;
            Radius = radius;
            LocalPosition = localPosition;
        }
    }
}