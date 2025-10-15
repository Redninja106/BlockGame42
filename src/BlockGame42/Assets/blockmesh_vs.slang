#include "sdl.slang"

struct VsIn
{
    // 4 bit x position -- 7 bit y position -- 4 bit z position -- 1 bit u -- 1 bit v -- 4 bit ao -- 11 bit texid
    
};

struct VsOut
{
    float4 position : SV_Position;
    float2 textureCoordinates : TEXCOORD0;
    uint blockTextureId : TEXCOORD1;
    float4 ambientOcclusion : TEXCOORD2;
}

struct CameraData
{
    float4x4 view;
    float4x4 proj;
};

struct ChunkData
{
    float4x4 transform;
};

SDL_VS_UNIFORM_BUFFER(0, float4x4 world);
SDL_VS_UNIFORM_BUFFER(1, float4x4 viewProj);
// SDL_VS_UNIFORM_BUFFER(2, float4x4 proj);

[shader("vertex")]
VsOut main(uint2 vertex : TEXCOORD0)
{
    uint x     = (vertex.x & 0b11111111000000000000000000000000) >> 24;
    uint y     = (vertex.x & 0b00000000111111110000000000000000) >> 16;
    uint z     = (vertex.x & 0b00000000000000001111111100000000) >> 8;
    uint u     = (vertex.x & 0b00000000000000000000000011110000) >> 4;
    uint v     = (vertex.x & 0b00000000000000000000000000001111) >> 0;

    uint ao    = (vertex.y & 0b11110000000000000000000000000000) >> 28;
    uint texid = (vertex.y & 0b00001111111111100000000000000000) >> 17;

    VsOut o;
    float4 pos = float4(x * .25f, y * .25f, z * .25f, 1);
    o.textureCoordinates = float2(u * .25f, v * .25f);
    o.blockTextureId = texid;
    o.ambientOcclusion = float4(bool(ao & 0b1000) ? 1 : .6f,
                                bool(ao & 0b0100) ? 1 : .6f,
                                bool(ao & 0b0010) ? 1 : .6f,
                                bool(ao & 0b0001) ? 1 : .6f);
    
    // pos = mul(transform, pos);
    pos = mul(world, pos);
    pos = mul(viewProj, pos);
    // pos = mul(proj, pos);

    o.position = pos;

    return o;
}
