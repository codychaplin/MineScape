namespace minescape.block
{
    public struct Block
    {
        public byte ID { get; }
        public string Name { get; }
        public bool IsSolid { get; }
        public bool IsTransparent { get; }
        public byte[] Faces { get; }

        public Block(byte _ID, string name, byte[] faces, bool isTransparent = false, bool isSolid = true)
        {
            ID = _ID;
            Name = name;
            Faces = faces;
            IsTransparent = isTransparent;
            IsSolid = isSolid;
            Faces = new byte[6];
        }
    }
}