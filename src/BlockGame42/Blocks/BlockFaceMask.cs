namespace BlockGame42.Blocks;

[Flags]
enum BlockFaceMask
{
    Empty = 0,
    TopLeft = 0x0033,
    TopCenter = 0x0066,
    TopRight = 0x00CC,
    
    CenterLeft = 0x0330,
    Center = 0x0660,
    CenterRight = 0x0CC0,

    BottomLeft = 0x3300,
    BottomCenter = 0x6600,
    BottomRight = 0xCC00,
    
    TopHalf = 0x00FF,
    LeftHalf = 0x3333,
    RightHalf = 0xCCCC,
    BottomHalf = 0xFF00,
    Full = 0xFFFF,
}
