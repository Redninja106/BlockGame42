using BlockGame42.Chunks;
using static System.Net.Mime.MediaTypeNames;

namespace BlockGame42.Blocks;

class SolidBlock : Block
{
    public Block HalfBlock { get; protected set; }

    public override BlockModel Model { get; }

    public override BlockPlacementHandler PlacementHandler => BlockPlacementHandler.Solid;

    public SolidBlock(string assetName, BlockStrength strength) : base(strength)
    {
        uint texid = Game.Textures.Get(assetName);
        Model = new SolidBlockModel(texid);

        this.HalfBlock = new HalfBlock(texid, strength);
    }
}