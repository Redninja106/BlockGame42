#include "sdl.slang"

struct VsOutput
{
    float4 position : SV_Position;
    float4 color : TEXCOORD0;
};

[shader("fragment")]
float4 main(VsOutput input) : SV_Target
{
    return input.color;
}