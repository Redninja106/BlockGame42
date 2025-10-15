#include "sdl.slang"

SDL_FS_UNIFORM_BUFFER(0, float4 color);

SDL_FS_RESOURCE(0, Texture2DArray texture);
SDL_FS_SAMPLER(0, SamplerState sampler);


struct VsOut
{
    float4 position : SV_Position;
    float2 textureCoordinates : TEXCOORD0;
    uint blockTextureId : TEXCOORD1;
    float4 ambientOcclusion : TEXCOORD2;
}

[shader("fragment")]
float4 main(VsOut vsout) : SV_Target
{
    float4 color = texture.Sample(sampler, float3(vsout.textureCoordinates, vsout.blockTextureId));

    float2 uv = vsout.textureCoordinates;
    uv = floor(uv * 16) / 16;
    float top = lerp(vsout.ambientOcclusion.x, vsout.ambientOcclusion.y, uv.x);
    float bottom = lerp(vsout.ambientOcclusion.z, vsout.ambientOcclusion.w, uv.x);
    float ao = lerp(top, bottom, uv.y);
    //color.xyz *= ao;
    return color;
}

