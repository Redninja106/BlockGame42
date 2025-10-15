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
}
