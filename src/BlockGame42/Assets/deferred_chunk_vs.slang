#include "sdl.slang"

struct Face
{
    float3 position;    // 3x8 24
    float3 up;          // 3x8 24
    float3 right;       // 3x8 24
    float2 lightmap_uv; // 2x8 16

    uint texid;         // 11  11
    float reflective;
}

struct VsOut
{
    float4 position : SV_Position;
    float3 worldPosition : TEXCOORD0;
    float3 textureCoordinates : TEXCOORD1;
    float3 normal : TEXCOORD2;
    int baseFaceID : TEXCOORD3;
}

struct CameraData
{
    float4x4 view;
    float4x4 proj;
};

struct ChunkData
{
    float4x4 transform;
    uint baseFaceID;
};

SDL_VS_UNIFORM_BUFFER(0, ChunkData chunkData);
SDL_VS_UNIFORM_BUFFER(1, float4x4 viewProj);
// SDL_VS_UNIFORM_BUFFER(2, float4x4 proj);

[shader("vertex")]
VsOut main(uint2 vertex : TEXCOORD0, uint primitiveID : SV_VertexID)
{
    uint x     = (vertex.x & 0b11111111000000000000000000000000) >> 24;
    uint y     = (vertex.x & 0b00000000111111110000000000000000) >> 16;
    uint z     = (vertex.x & 0b00000000000000001111111100000000) >> 8;
    uint u     = (vertex.x & 0b00000000000000000000000011110000) >> 4;
    uint v     = (vertex.x & 0b00000000000000000000000000001111) >> 0;

    uint ao    = (vertex.y & 0b11110000000000000000000000000000) >> 28;
    uint texid = (vertex.y & 0b00001111111111100000000000000000) >> 17;
    uint n_x   = (vertex.y & 0b00000000000000011000000000000000) >> 15;
    uint n_y   = (vertex.y & 0b00000000000000000110000000000000) >> 13;
    uint n_z   = (vertex.y & 0b00000000000000000001100000000000) >> 11;

    VsOut o;
    o.normal = float3(n_x, n_y, n_z) - float3(1);
    float4 pos = float4(x * .25f, y * .25f, z * .25f, 1);
    o.textureCoordinates = float3(u * .25f, v * .25f, float(texid));

    // pos = mul(transform, pos);
    pos = mul(chunkData.transform, pos);
    o.worldPosition = pos.xyz;
    pos = mul(viewProj, pos);
    // pos = mul(proj, pos);
    o.baseFaceID = (chunkData.baseFaceID + primitiveID / 6) * 16 * 16;
    o.position = pos;

    return o;
}
