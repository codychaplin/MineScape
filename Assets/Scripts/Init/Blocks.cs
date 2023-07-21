using minescape.block;
using System.Collections.Generic;

namespace minescape.init
{
    public static class Blocks
    {
        public static List<Block> list = new()
        {
            new Block(0, "Air", new byte[6]) { IsSolid = false },
            new Block(1, "Bedrock", new byte[] {0, 0, 0, 0, 0, 0}),
            new Block(2, "Stone", new byte[] {1, 1, 1, 1, 1, 1}),
            new Block(3, "Dirt", new byte[] {3, 3, 3, 3, 3, 3}),
            new Block(4, "Grass", new byte[] {4, 4, 5, 3, 4, 4}),
            new Block(5, "Sand", new byte[] {9, 9, 9, 9, 9, 9}),
            new Block(6, "Water", new byte[] {10, 10, 10, 10, 10, 10})
        };
        
    }
}