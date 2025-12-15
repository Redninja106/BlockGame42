#include "sdl.slang"
#include "random.slang"
#include "hash.slang"

struct FsIn
{
    float4 position : SV_Position;
    float3 worldPosition : TEXCOORD0;
    float4 textureCoordinates : TEXCOORD1;
    float3 normal : TEXCOORD2;
    int baseFaceID : TEXCOORD3;
};

struct FsOut
{
    float4 position : SV_Target0;
    float4 texCoord : SV_Target1;
    float4 normal : SV_Target2;
};

SDL_FS_SAMPLER(0, SamplerState sampler);
SDL_FS_SAMPLER(1, SamplerState sampler1);
SDL_FS_SAMPLER(2, SamplerState sampler2);
SDL_FS_RESOURCE(0, Texture2DArray<float4> albedoTextureArray);
SDL_FS_RESOURCE(1, Texture2DArray<float4> normalTextureArray);
SDL_FS_RESOURCE(2, Texture2DArray<float4> specularTextureArray);

SDL_FS_UNIFORM_BUFFER(0, float3 cameraPosition);

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

float3 ParallaxMapping(float3 texCoords, float3 viewDir, out float ao)
{
    constexpr float step = 0.0025;
    constexpr float stepCount = 64;
    float3 p = 0;
    ao = 1;

    for (int i = 0; i < stepCount; i++)
    {
        float height = .75 + .25 * normalTextureArray.Sample(sampler, texCoords + float3(p.xy, 0)).a;

        if (1 - p.z <= height)
        {
            if (1 - p.z + step <= height)
            {
                ao = .8;
            }
            break;
        }

        p -= viewDir * step;
    }

    return texCoords + float3(p.xy, 0);
}

float3x3 calc_face_tbn_matrix(float3 hitNormal)
{
    int3 n = sign(hitNormal);
    float3 tangent = select(n.x != 0, n.x * float3(0, 0, 1), float3(1, 0, 0) * select(n.y != 0, n.y, -n.z));
    float3 bitangent = select(n.y != 0, n.y * float3(0, 0, 1), float3(0, 1, 0));
    return float3x3(tangent, bitangent, hitNormal);
}

[shader("fragment")]
FsOut main(FsIn i)
{
    FsOut o;

    float3x3 tbn = calc_face_tbn_matrix(i.normal);

    o.position = float4(i.worldPosition, 0);
    o.normal = float4(i.normal, 0);

    float ao;
    float3 parallax = ParallaxMapping(i.textureCoordinates.xyz, normalize(mul(tbn, i.worldPosition) - mul(tbn, cameraPosition)), ao);
    o.texCoord = float4(parallax, ao);

    return o;
}
