#include "sdl.slang"

struct TileRecord
{
    uint id;
    uint4 accumulatedColor;
};

SDL_FS_RESOURCE(0, Texture2DArray<float4> blockTextures);
SDL_FS_SAMPLER(0, SamplerState sampler);

SDL_FS_RESOURCE(1, Texture2D<float4> texCoordTarget);
SDL_FS_RESOURCE(2, Texture2D<uint> idTarget);

SDL_FS_RESOURCE(3, StructuredBuffer<TileRecord> tileLookup);

SDL_FS_UNIFORM_BUFFER(0, uint lookupSize);
SDL_FS_UNIFORM_BUFFER(1, float4x4 mat);

uint32_t hash(uint32_t x) {
    x = ((x >> 16) ^ x) * 0x45d9f3bu;
    x = ((x >> 16) ^ x) * 0x45d9f3bu;
    x = (x >> 16) ^ x;
    return x;
}

//static int conflicts = 0;

uint find(uint id)
{
    // linear probing
    uint searchIndex = hash(id) % lookupSize;
    for (int i = 0; i < 32; i++) 
    {
        if (tileLookup[searchIndex].id == id)
        {
            return searchIndex;
        }

        if (tileLookup[searchIndex].id == 0)
        {
            return 0xFFFFFFFF;
        }

        searchIndex++;
        //conflicts++;

        if (searchIndex > lookupSize)
        {
            searchIndex = 0;
        }
    }

    return searchIndex;
}

struct Box
{
    float3 min;
    float3 max;
};

bool BoxRaycast(Box box, Ray ray, out float t, out float3 normal)
{
    float t1 = (box.min.x - ray.origin.x) * ray.inverseDirection.x;
    float t2 = (box.max.x - ray.origin.x) * ray.inverseDirection.x;
    float t3 = (box.min.y - ray.origin.y) * ray.inverseDirection.y;
    float t4 = (box.max.y - ray.origin.y) * ray.inverseDirection.y;
    float t5 = (box.min.z - ray.origin.z) * ray.inverseDirection.z;
    float t6 = (box.max.z - ray.origin.z) * ray.inverseDirection.z;

    float tNear = max(max(min(t1, t2), min(t3, t4)), min(t5, t6));
    float tFar = min(min(max(t1, t2), max(t3, t4)), max(t5, t6));

    if (tNear <= tFar && tFar > 0 && tNear < ray.length)
    {
        t = tNear < 0 ? tFar : tNear;
        normal = 0;

        if (t == t1)
            normal = float3(-1, 0, 0);
        else if (t == t2)
            normal = float3(1, 0, 0);
        else if (t == t3)
            normal = float3(0, -1, 0);
        else if (t == t4)
            normal = float3(0, 1, 0);
        else if (t == t5)
            normal = float3(0, 0, -1);
        else if (t == t6)
            normal = float3(0, 0, 1);
        else
            normal = float3(0, 0, 0); // huh?

        return true;
    }

    return false;
}

[shader("fragment")]
float4 main(float4 position : SV_Position) : SV_Target
{
    float2 ndc = (position.xy / float2(1920, 1080)) * 2 - 1;
    ndc *= float2(1920.0 / 1080.0, -1);
    Ray ray;
    ray.origin = float3(0, 0, -2);
    ray.direction = normalize(float3(ndc, 1));
    ray.inverseDirection = 1 / ray.direction;
    ray.length = 64;

    ray.origin = mul(mat, float4(ray.origin, 1)).xyz;
    ray.direction = mul(mat, float4(ray.direction, 0)).xyz;
    ray.inverseDirection = 1 / ray.direction;

    float outT;
    float3 outNormal, outNormal2;

    if (BoxRaycast(Box(0, 1), ray, outT, outNormal2))
    {
    }

    if (hit_ray_mask(ray, 0x00FF00FF00FF00FF, 0, outT, abs(ray.inverseDirection), sign(ray.direction), outT, outNormal))
    {
        return float4(outNormal, 1);
    }
    else
    {
        // return float4(0);
    }

    uint2 location = uint2(position.xy);
    float4 texCoords = texCoordTarget[location];
    uint id = idTarget[location];

    float4 texColor = blockTextures.Sample(sampler, texCoords.xyz);
    float4 texEmmission = blockTextures.Sample(sampler, texCoords.xyw);
    
    if (id == 0)
    {
        return float4(.392f, .584f, .929f, 1);
    }
    
    // return float4(sin((id * 1234.678954) % 15634.51414), sin((id * 6243.16324) % 7123.72346), sin((id * 2645.6234) % 6234.172378), 1);
    // return float4((tileLookup[int(position.y) * 1920 + int(position.x)].accumulatedCount).xxx / 32.0, 1);

    uint idx = find(id);
    // return float4((conflicts).xxx / 32.0, 1);

    if (idx == 0xFFFFFFFF)
    {
        // emit green - >32 hash conflicts
        return float4(0, 1, 0, 1);
    }

    float3 color = texColor.xyz * float3(tileLookup[idx].accumulatedColor.xyz) / 255.0;
    uint accumulatedCount = tileLookup[idx].accumulatedColor.w;
    if (accumulatedCount == 0)
    {
        // emit red - no illumination samples
        return float4(1, 0, 0, 1);
    }
    color /= float(accumulatedCount);
    color += texEmmission.xyz;
    return float4(color, 1);
}

struct Ray
{
    float3 origin;
    float3 direction;
    float3 inverseDirection;
    float length;

    float3 at(float t)
    {
        return origin + direction * t;
    }
};

bool hit_ray_mask(Ray ray, uint64_t mask, int3 voxel, float t_entrance, float3 delta_t, int3 step, out float outT, out float3 outNormal)
{
    delta_t *= .25;

    float3 pos = ray.at(t_entrance + 0.00001);
    int3 voxel = int3(floor((pos) * 4));

    float3 t = delta_t * select(ray.direction > 0, 1 - frac(pos*4), frac(pos*4));

    bool3 voxelIncr = false;
    for (int i = 0; i < 10; i++)
    {
        if (any(voxel < 0 || voxel >= 4))
        {
            return false;
        }

        if (((mask >> (voxel.x + voxel.z * 4 + voxel.y * 16)) & 1) != 0)
        {
            outT = min3(t.x, t.y, t.z);
            outNormal = voxelIncr * -step;
            return true;
        }

        voxelIncr = t.xyz <= t.yxx && t.xyz <= t.zzy;
        t = float3(voxelIncr) * delta_t + t;
        voxel = voxelIncr * step + voxel;
    }
    return false;
}
