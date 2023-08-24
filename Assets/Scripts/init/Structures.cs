using minescape.structures;
using System.Collections.Generic;

namespace minescape.init
{
    public static class Structures
    {
        public static Structure TREE = new(1, 2, Type.Tree);
        public static Structure CACTUS = new(2, 0, Type.Cactus);

        public static Dictionary<byte, Structure> structures = new()
        {
            { 1, TREE },
            { 2, CACTUS }
        };
    }
}