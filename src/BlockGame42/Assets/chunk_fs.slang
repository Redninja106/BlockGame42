#include "random.slang"
#include "sdl.slang"

SDL_FS_UNIFORM_BUFFER(0, float4 color);

SDL_FS_RESOURCE(0, Texture2DArray texture);
SDL_FS_SAMPLER(0, SamplerState sampler);

SDL_FS_RESOURCE(1, ByteAddressBuffer blockMasks);

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

uint ReadVoxelByte(int3 pos)
{
    uint offset = pos.y * 128 * 128 + pos.z * 128 + pos.x; // 1 byte per voxel

    // Load 32-bit word and extract byte
    uint word = blockMasks.Load(offset & ~0b11); // align to 4 bytes
    uint byteIndex = offset & 0b11;              // which byte in the word
    return (word >> (byteIndex * 8)) & 0xFF;
}

struct Box
{
    float3 min;
    float3 max;
};

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

static const Box boxes[] = 
{
    Box(float3(0.0f, 0.0f, 0.0f), float3(0.5f, 0.5f, 0.5f)),
    Box(float3(0.5f, 0.0f, 0.0f), float3(1.0f, 0.5f, 0.5f)),
    Box(float3(0.0f, 0.0f, 0.5f), float3(0.5f, 0.5f, 1.0f)),
    Box(float3(0.5f, 0.0f, 0.5f), float3(1.0f, 0.5f, 1.0f)),
    Box(float3(0.0f, 0.5f, 0.0f), float3(0.5f, 1.0f, 0.5f)),
    Box(float3(0.5f, 0.5f, 0.0f), float3(1.0f, 1.0f, 0.5f)),
    Box(float3(0.0f, 0.5f, 0.5f), float3(0.5f, 1.0f, 1.0f)),
    Box(float3(0.5f, 0.5f, 0.5f), float3(1.0f, 1.0f, 1.0f)),
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

        // if (t == t1)
        //     normal = float3(-1, 0, 0);
        // else if (t == t2)
        //     normal = float3(1, 0, 0);
        // else if (t == t3)
        //     normal = float3(0, -1, 0);
        // else if (t == t4)
        //     normal = float3(0, 1, 0);
        // else if (t == t5)
        //     normal = float3(0, 0, -1);
        // else if (t == t6)
        //     normal = float3(0, 0, 1);
        // else
        //     normal = float3(0, 0, 0); // huh?

        return true;
    }

    return false;
}

float Frac(float f, float s)
{
    if (s > 0)
        return 1 - f + floor(f);
    else
        return f - floor(f);
}
float3 getMax(float3 start, float3 step)
{
    return float3(
		step.x > 0 ? 1 - frac(start.x) : frac(start.x),
		step.y > 0 ? 1 - frac(start.y) : frac(start.y),
        step.z > 0 ? 1 - frac(start.z) : frac(start.z)
    );
}

bool raycast(Ray ray, out float t, out float3 normal, out int3 voxel)
{
    voxel = int3(floor(ray.origin)) + int3(2 * 32, 0, 2 * 32);
    int3 step = int3(sign(ray.direction));

    if (step.x == 0 && step.y == 0 && step.z == 0)
    {
        t = 0;
        normal = int3(0);
        return false;
    }

    float3 start = ray.at(0);
    float3 end = ray.at(ray.length);

    float3 d = end - start;
    float3 tDelta = step / d;
    float3 tMax = tDelta * getMax(start, step);

    int dist = 1000;

    normal = int3(0);
    t = 0;
    while (bool(--dist))
    {
        // if (any(voxel < 0) || any(voxel >= box.max))
        // {
        //     return false;
        // }

        int byte = ReadVoxelByte(int3(voxel));

        if (byte == 0xFF)
        {
            return true;
        }
        else if (byte != 0)
        {
            // int3 mask = (ray.at(t) - voxel) > .5;
            // int bitIdx = mask.x << 2 | mask.y << 1 | mask.z << 0;
            return false;

            bool hit = false;
            Ray localRay = ray;
            localRay.origin -= voxel - int3(2 * 32, 0, 2 * 32);
            for (int i = 0; i < 8; i++)
            {
                if (((byte >> i) & 1) == 1)
                {
                    float boxT;
                    float3 boxNormal;
                    if (BoxRaycast(boxes[i], localRay, boxT, boxNormal))
                    {
                        if (boxT <= t)
                        {
                            hit = true;
                            t = boxT;
                            normal = boxNormal;
                        }
                        return true;
                    }
                }
            }

            if (hit)
            {
                return true;
            }
        }

        if (step.x != 0 && tMax.x < tMax.y)
        {
            if (step.x != 0 && tMax.x < tMax.z)
            {
                t = tMax.x;
                voxel.x += step.x;
                tMax.x += tDelta.x;
                normal = float3(-step.x, 0, 0);
            }
            else
            {
                t = tMax.z;
                voxel.z += step.z;
                tMax.z += tDelta.z;
                normal = float3(0, 0, -step.z);
            }
        }
        else
        {
            if (step.y != 0 && tMax.y < tMax.z)
            {
                t = tMax.y;
                voxel.y += step.y;
                tMax.y += tDelta.y;
                normal = float3(0, -step.y, 0);
            }
            else
            {
                t = tMax.z;
                voxel.z += step.z;
                tMax.z += tDelta.z;
                normal = float3(0, 0, -step.z);
            }
        }
    }

    return false;
}

struct FsOut
{
    float4 color : SV_Target0;
    uint id : SV_Target1;
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

static Random random;

float3 ray_color(Ray ray)
{
    const int MaxBounces = 5;

    float3 color = float3(1, 1, 1);
    for (int i = 0; i < MaxBounces; i++)
    {
        float hitT;
        float3 hitNormal;
        int3 hitVoxel;
        if (raycast(ray, hitT, hitNormal, hitVoxel))
        {
            color *= float3(.5);
        }
        else
        {
            //color *= float3(.392f, .584f, .929f);
        }
    }

    return color;
}

[shader("fragment")]
FsOut main(VsOut vsout)
{
    random.seed = (int)(vsout.worldPosition.x * 125451) + (int)(vsout.worldPosition.y * 16535);
    random.Cycle();

    float4 color = texture.Sample(sampler, float3(vsout.textureCoordinates, vsout.blockTextureId));

    // int3 coords = int3(floor(vsout.worldPosition.xyz + float3(.01)));
    // coords += int3(2 * 32, 0, 2 * 32);
    // int bs = ReadVoxelByte(coords);
    // return float4(bs / 255.0, 0, 0, 1);

    // Ray ray;
    // ray.direction = normalize(float3(.61314, 1.02842, .23875));
    // ray.origin = vsout.worldPosition + ray.direction * 0.001f;
    // ray.inverseDirection = 1.0 / ray.direction;
    // ray.length = 100;
    // 
    // float t;
    // float3 normal;
    // int3 voxel;
    // float b = 1;
    // if (raycast(ray, t, normal, voxel))
    // {
    //     b = .5;
    // }

    FsOut o;

    const int samples = 1;

    float3 totalColor = float3(0);
    for (int i = 0; i < samples; i++)
    {
        Ray sample_ray;
        sample_ray.origin = vsout.worldPosition + vsout.normal * .0001;
        sample_ray.direction = normalize(vsout.normal + random.NextUnitVector());
        sample_ray.inverseDirection = 1 / sample_ray.direction;
        sample_ray.length = 64;

        totalColor += ray_color(sample_ray);
    }
    totalColor *= (1.0 / samples);

    o.color = color * float4(totalColor, 1);

    float res = getres(linearize_depth(vsout.position.z));
    float2 uv = vsout.textureCoordinates;
    uv = floor(uv * res) / res;
    o.id = vsout.baseFaceID + ((int)(uv.y * res * res)) + (int)(uv.x * res);
    
    return o;
}
