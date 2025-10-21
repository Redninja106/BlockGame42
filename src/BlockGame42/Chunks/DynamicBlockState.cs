using BlockGame42.Blocks;

namespace BlockGame42.Chunks;

struct DynamicBlockState
{
    public byte Mask;

    public DynamicBlockState(byte mask)
    {
        Mask = mask;
    }

    /// <summary>
    /// YZX ORDER
    /// </summary>
    public bool this[int index]
	{
		get 
		{

			int mask = (1 << index);
            return (this.Mask & mask) != 0; 
		}
		set
        {
            int mask = (1 << index);
            this.Mask = (byte)(value ? this.Mask | mask : this.Mask & ~mask);
		}
	}

    public ulong GetBlockMask64()
    {
                                                //==--==--==--==--
        byte mask = this.Mask;                  //====----====----
        ulong result = 0;                       //========--------
        if ((mask & 0b00000001) != 0) result |= 0x0000000000330033;
        if ((mask & 0b00000010) != 0) result |= 0x0000000000CC00CC;
        if ((mask & 0b00000100) != 0) result |= 0x0000000033003300;
        if ((mask & 0b00001000) != 0) result |= 0x00000000CC00CC00;
        if ((mask & 0b00010000) != 0) result |= 0x0033003300000000;
        if ((mask & 0b00100000) != 0) result |= 0x00CC00CC00000000;
        if ((mask & 0b01000000) != 0) result |= 0x3300330000000000;
        if ((mask & 0b10000000) != 0) result |= 0xCC00CC0000000000;
        return result;
    }
}
