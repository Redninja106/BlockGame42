using BlockGame42.Blocks;

namespace BlockGame42.Rendering;

class Material
{
    public MaterialData Data;

    public static Material CreateUniform(uint transmissionTextureId, uint emissionTextureId)
    {
        Material result = new();
        result.Data.Transmission.Fill(transmissionTextureId);
        result.Data.Emission.Fill(emissionTextureId);
        Game.Materials.Add(result);
        return result;
    }
}

struct MaterialData
{
    public DirectionalValue<uint> Transmission;
    public DirectionalValue<uint> Emission;
}