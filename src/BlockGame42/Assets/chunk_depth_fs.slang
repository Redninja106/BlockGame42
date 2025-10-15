#include "sdl.slang"

struct VsOut
{
    float4 position : SV_Position;
    float2 textureCoordinates : TEXCOORD0;
    uint blockTextureId : TEXCOORD1;
    float4 ambientOcclusion : TEXCOORD2;
    float3 worldPosition : TEXCOORD3;
    float3 normal : TEXCOORD4;
    int baseFaceID : TEXCOORD5;
};

[shader("fragment")]
void main(VsOut vsout)
{
}