#include "sdl.slang"

struct GUIVertex
{
    float2 position : TEXCOORD0;
    float2 textureCoordinates : TEXCOORD1;
    float4 color : TEXCOORD2;
};

struct VsOutput
{
    float4 position : SV_Position;
    float2 textureCoordinates : TEXCOORD0;
    float4 color : TEXCOORD1;
};

SDL_VS_UNIFORM_BUFFER(0, float4x4 transform);

[shader("vertex")]
VsOutput main(GUIVertex vertex)
{
    VsOutput output;

    output.position = mul(transform, float4(vertex.position, 0, 1));
    output.textureCoordinates = vertex.textureCoordinates;
    output.color = vertex.color;

    return output;
}