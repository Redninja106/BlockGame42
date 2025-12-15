namespace BlockGame42.Blocks;

struct BlockStrength
{
    public byte Compression;
    public byte Tension;
    public byte Lateral;

    public BlockStrength(byte compressive, byte tension, byte lateral)
    {
        Compression = compressive;
        Tension = tension;
        Lateral = lateral;
    }
}
