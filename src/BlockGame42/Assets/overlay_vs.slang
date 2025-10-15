#include "sdl.slang"

struct GUIVertex
{
    float3 position : TEXCOORD0;
    float4 color : TEXCOORD1;
};

struct VsOutput
{
    float4 position : SV_Position;
    float4 color : TEXCOORD0;
};

SDL_VS_UNIFORM_BUFFER(0, float4x4 viewProj);

[shader("vertex")]
VsOutput main(GUIVertex vertex)
{
    VsOutput output;

    float4 pos = float4(vertex.position, 1);
    pos = mul(viewProj, pos);

    output.position = pos;
    output.color = vertex.color;

    return output;
}