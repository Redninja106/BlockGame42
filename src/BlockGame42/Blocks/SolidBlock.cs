using BlockGame42.Chunks;
using BlockGame42.Rendering;
using static System.Net.Mime.MediaTypeNames;

namespace BlockGame42.Blocks;

class SolidBlock : Block
{
    public Block HalfBlock { get; protected set; }

    public override BlockModel Model { get; }

    public override BlockPlacementHandler PlacementHandler => BlockPlacementHandler.Solid;

    public SolidBlock(string? assetName, string? emissionAssetName, BlockStrength strength) : base(strength)
    {
        uint texid = Game.Textures.Get(assetName);
        uint emit_texid = Game.Textures.Get(emissionAssetName);
        Model = new SolidBlockModel(Material.CreateUniform(texid, emit_texid));

        this.HalfBlock = new HalfBlock(texid, strength);
    }
}