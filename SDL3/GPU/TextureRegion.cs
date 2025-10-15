namespace SDL.GPU;

public unsafe struct TextureRegion
{
    public Texture Texture;
    public uint MipLevel;
    public uint Layer;
    public uint X;
    public uint Y;
    public uint Z;
    public uint W;
    public uint H;
    public uint D;

    public TextureRegion(Texture texture, uint mipLevel, uint layer, uint x, uint y, uint z, uint w, uint h, uint d)
    {
        Texture = texture;
        MipLevel = mipLevel;
        Layer = layer;
        X = x;
        Y = y;
        Z = z;
        W = w;
        H = h;
        D = d;
    }

    public SDL_GPUTextureRegion Marshal()
    {
        return new SDL_GPUTextureRegion
        {
            texture = (SDL_GPUTexture*)Texture.Handle,
            mip_level = MipLevel,
            layer = Layer,
            x = X,
            y = Y,
            z = Z,
            w = W,
            h = H,
            d = D
        };
    }
}