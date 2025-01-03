namespace Cutulu.Network
{
    public partial struct Packet
    {
        public short Key { get; set; }

        public byte[] Buffer { get; set; }
    }
}