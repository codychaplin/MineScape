namespace minescape.block
{
    public struct Block
    {
        public byte ID { get; }
        public bool IsSolid { get; }
        public bool IsTransparent { get; }
        public bool IsPlant { get; }
        public byte Back;
        public byte Front;
        public byte Top;
        public byte Bottom;
        public byte Left;
        public byte Right;

        public Block(byte _ID, byte back, byte front, byte top, byte bottom, byte left, byte right, bool isTransparent = false, bool isSolid = true, bool isPlant = false)
        {
            ID = _ID;
            Back = back;
            Front = front;
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
            IsTransparent = isTransparent;
            IsSolid = isSolid;
            IsPlant = isPlant;
        }

        public byte GetFace(int index)
        {
            switch (index)
            {
                case 0:
                    return Back;
                case 1:
                    return Front;
                case 2:
                    return Top;
                case 3:
                    return Bottom;
                case 4:
                    return Left;
                case 5:
                    return Right;
                default:
                    return 0;

            }
        }
    }
}