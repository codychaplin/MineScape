using minescape.block;
using System.Collections.Generic;

namespace minescape.init
{
    public static class Blocks
    {
        public static Block AIR = new(0, "Air", new byte[6], true, false);
        public static Block BEDROCK = new(1, "Bedrock", new byte[] { 0, 0, 0, 0, 0, 0 });
        public static Block STONE = new(2, "Stone", new byte[] { 1, 1, 1, 1, 1, 1 });
        public static Block DIRT = new(3, "Dirt", new byte[] { 3, 3, 3, 3, 3, 3 });
        public static Block GRASS = new(4, "Grass", new byte[] { 4, 4, 5, 3, 4, 4 });
        public static Block SAND = new(5, "Sand", new byte[] { 9, 9, 9, 9, 9, 9 });
        public static Block WATER = new(6, "Water", new byte[] { 10, 10, 10, 10, 10, 10 }, true);

        public static Dictionary<byte, Block> blocks = new()
        {
            { 0, AIR },
            { 1, BEDROCK },
            { 2, STONE },
            { 3, DIRT },
            { 4, GRASS },
            { 5, SAND },
            { 6, WATER }
        };
        
    }
}