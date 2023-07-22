namespace minescape.block
{
    public class Block
    {
        public byte ID { get; }
        public string Name { get; }
        public bool IsSolid { get; }
        public bool IsTransparent { get; }
        public byte[] Faces = new byte[6];

        public Block(byte _ID, string name, byte[] faces, bool isTransparent = false, bool isSolid = true)
        {
            ID = _ID;
            Name = name;
            Faces = faces;
            IsTransparent = isTransparent;
            IsSolid = isSolid;
        }
    }
}