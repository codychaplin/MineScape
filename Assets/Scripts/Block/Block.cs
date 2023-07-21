namespace minescape.block
{
    public class Block
    {
        public byte ID { get; }
        public string Name { get; }
        public bool IsSolid { get; set; } = true;
        public byte[] Faces = new byte[6];

        public Block(byte _ID, string name, byte[] faces)
        {
            ID = _ID;
            Name = name;
            Faces = faces;
        }
    }
}