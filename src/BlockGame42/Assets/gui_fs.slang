#include "sdl.slang"

struct VsOutput
{
    float4 position : SV_Position;
    float2 textureCoordinates : TEXCOORD0;
    float4 color : TEXCOORD1;
};

SDL_FS_RESOURCE(0, Texture2D texture);
SDL_FS_SAMPLER(0, SamplerState sampler);

[shader("fragment")]
float4 main(VsOutput input) : SV_Target
{
    return input.color * texture.Sample(sampler, input.textureCoordinates);
}