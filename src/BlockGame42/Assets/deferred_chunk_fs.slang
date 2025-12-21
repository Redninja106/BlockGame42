#include "random.slang"
#include "sdl.slang"
#include "util.slang"

struct FsIn
{
    float4 position : SV_Position;
    float3 worldPosition : TEXCOORD0;
    float4 textureCoordinates : TEXCOORD1;
    float3 normal : TEXCOORD2;
};

struct FsOut
{
    float4 position : SV_Target0;
    float4 albedo : SV_Target1;
    float4 normal : SV_Target2;
    float4 specular : SV_Target3;
};

SDL_FS_SAMPLER(0, SamplerState sampler);
SDL_FS_SAMPLER(1, SamplerState sampler1);
SDL_FS_SAMPLER(2, SamplerState sampler2);
SDL_FS_RESOURCE(0, Texture2DArray<float4> albedoTextureArray);
SDL_FS_RESOURCE(1, Texture2DArray<float4> normalTextureArray);
SDL_FS_RESOURCE(2, Texture2DArray<float4> specularTextureArray);

SDL_FS_UNIFORM_BUFFER(0, float3 cameraPosition);

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

[shader("fragment")]
FsOut main(FsIn i)
{
    FsOut o;

    float3x3 tbn = calc_face_tbn_matrix(i.normal);

    // pack original normal into position.w
    float pos_w = asfloat((int)packSnorm4x8(float4(i.normal, 0)));
    o.position = float4(i.worldPosition, pos_w);

    float parallax_ao = 1.0;
    float3 tangent_world_position = mul(tbn, i.worldPosition);
    float3 tangent_camera_position = mul(tbn, cameraPosition);
    float3 tangent_view_dir = normalize(tangent_world_position - tangent_camera_position);
    float3 parallax_uv = ParallaxMapping(i.textureCoordinates.xyz, tangent_view_dir, parallax_ao);

    //if (any(parallax_uv.xy < 0) || any(parallax_uv.xy > 1))
    //{
    //    discard;
    //}

    o.specular = specularTextureArray.Sample(sampler, parallax_uv);
    o.albedo = albedoTextureArray.Sample(sampler, parallax_uv);

    if (o.albedo.a == 0)
    {
        discard;
    }

    float4 encoded_normal = normalTextureArray.Sample(sampler, parallax_uv);
    float3 normal = decode_normal(encoded_normal.xy);
    normal = normalize(mul(normal, tbn));
    normal = i.normal;

    o.normal = float4(
        encode_normal(normal),
        encoded_normal.z * parallax_ao, // include parallax AO here
        encoded_normal.w
    );

    return o;
}
