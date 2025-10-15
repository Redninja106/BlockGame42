#include "random.slang"
#include "sdl.slang"

struct FsIn
{
    float4 position : SV_Position;
    float3 worldPosition : TEXCOORD0;
    float3 textureCoordinates : TEXCOORD1;
    float3 normal : TEXCOORD2;
    int baseFaceID : TEXCOORD3;
};

struct FsOut
{
    float4 position : SV_Target0;
    float4 texCoord : SV_Target1;
    float4 normal : SV_Target2;
    uint id : SV_Target3;
}

float linearize_depth(float d, float zNear = 0.1, float zFar = 1000)
{
    return zNear * zFar / (zFar + d * (zNear - zFar));
}

float getres(float d)
{
    if (d < 16) return 16;
    if (d < 32) return 8;
    if (d < 128) return 4;
    if (d < 256) return 2;
    return 1;
}

[shader("fragment")]
FsOut main(FsIn i)
{
    FsOut o;

    o.position = float4(i.worldPosition, 0);
    o.texCoord = float4(i.textureCoordinates, 0);
    o.normal =   float4(i.normal, 0);

    float res = getres(linearize_depth(i.position.z));
    float2 uv = i.textureCoordinates.xy;
    uv = floor(uv * res) / res;
    o.id = i.baseFaceID + ((int)(uv.y * 16 * 16)) + (int)(uv.x * 16);

    return o;
}
