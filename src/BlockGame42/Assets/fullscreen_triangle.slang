#include "sdl.slang"

static const float4 triangle[] = 
{
    float4(-1, -1, 0, 1),
    float4(-1, 3, 0, 1),
    float4(3, -1, 0, 1),
};

[shader("vertex")]
float4 main(uint id : SV_VertexID) : SV_Position
{
    return triangle[id];
}