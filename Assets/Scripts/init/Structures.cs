using minescape.structure;
using System.Collections.Generic;

namespace minescape.init
{
    public static class Structures
    {
        public static Structure TREE = new(1, 2);
        public static Structure CACTUS = new(2, 0);

        public static Dictionary<byte, Structure> structures = new()
        {
            { 1, TREE },
            { 2, CACTUS }
        };
    }
}