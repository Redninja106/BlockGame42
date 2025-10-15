#include "sdl.slang"
#include "random.slang"

struct TileRecord
{
    uint id;
    uint accumulatedCount;
    uint3 accumulatedColor;
};

SDL_CS_RW_RESOURCE(0, RWStructuredBuffer<TileRecord> tileLookup);

SDL_CS_RESOURCE(0, Texture2D<unorm float4> positionTarget);
SDL_CS_RESOURCE(1, Texture2D<unorm float4> normalTarget);
SDL_CS_RESOURCE(2, Texture2D<uint> idTarget);
SDL_CS_RESOURCE(3, ByteAddressBuffer blockMasks);

SDL_CS_UNIFORM_BUFFER(0, uint lookupSize);
SDL_CS_UNIFORM_BUFFER(1, uint ticks);
SDL_CS_UNIFORM_BUFFER(2, float4 sundir);

static Random random;

[shader("compute")]
[numthreads(16, 16, 1)]
void main(uint3 dispatchThreadID: SV_DispatchThreadID)
{
    float3 position = positionTarget[dispatchThreadID.xy].xyz;
    float3 normal = normalTarget[dispatchThreadID.xy].xyz;
    uint id = idTarget[dispatchThreadID.xy];

    random.seed = ticks * 0x3f05aa5f +
                  (dispatchThreadID.x + 0x8523) * 0xb7e35a7d + 
                  (dispatchThreadID.y + 0x7423) * 0x3f05aa5f;
    random.Next();

    const int samples = 1;

    float3 totalColor = float3(0);
    for (int i = 0; i < samples; i++)
    {
        Ray sample_ray;
        sample_ray.origin = position + normal * .0001;
        sample_ray.direction = normalize(normal + random.NextUnitVector());
        sample_ray.inverseDirection = 1 / sample_ray.direction;
        sample_ray.length = 64;

        totalColor += ray_color(sample_ray);
    }
    totalColor *= (1.0 / samples);

    uint idx = find_or_insert(id);
    InterlockedAdd(tileLookup[idx].accumulatedColor.r, int(255 * totalColor.r));
    InterlockedAdd(tileLookup[idx].accumulatedColor.g, int(255 * totalColor.g));
    InterlockedAdd(tileLookup[idx].accumulatedColor.b, int(255 * totalColor.b));
    InterlockedAdd(tileLookup[idx].accumulatedCount, 1);
}

uint32_t hash(uint32_t x) 
{
    x = ((x >> 16) ^ x) * 0x45d9f3bu;
    x = ((x >> 16) ^ x) * 0x45d9f3bu;
    x = (x >> 16) ^ x;
    return x;
}

uint find_or_insert(uint id)
{
    const int max_search = 32;

    // linear probing
    uint searchIndex = hash(id) % lookupSize;
    for (int i = 0; i < max_search; i++)
    { 
        uint original;
        InterlockedCompareExchange(tileLookup[searchIndex].id, 0, id, original);
        if (original == 0 || original == id)
        {
            return searchIndex;
        }

        searchIndex++;

        if (searchIndex >= lookupSize)
        {
            searchIndex = 0;
        }
    }

    return searchIndex;
}

float3 ray_color(Ray ray)
{
    const int MaxBounces = 4;

    float3 transmission[MaxBounces];
    float3 emission[MaxBounces];

    for (int i = 0; i < MaxBounces; i++)
    {
        float hitT;
        float3 hitNormal;
        int3 hitVoxel;
        bool hit = raycast(ray, hitT, hitNormal, hitVoxel);
        if (hit)
        {
            emission[i] = float3(0, 0, 0);
            transmission[i] = float3(.5, .5, .5);

            ray.origin = ray.at(hitT) + hitNormal * 0.001;
            ray.direction = normalize(hitNormal + random.NextUnitVector());
            ray.inverseDirection = 1 / ray.direction;

            const float sunfuzz = 0.01;
            const float sunstrength = .75;

            ray.direction = normalize(sundir.xyz + sunfuzz * random.NextUnitVector());
            ray.inverseDirection = 1.0 / ray.direction;

            if (!raycast(ray, hitT, hitNormal, hitVoxel))
            {
                float sunbrightness = dot(ray.direction, sundir.xyz);
                emission[i] += float3(sunstrength * max(0, sunbrightness));
            }
        }

        const float sunfuzz = 0.01;
        const float sunstrength = .75;

        // sun emission!
        ray.direction = normalize(sundir.xyz + sunfuzz * random.NextUnitVector());
        ray.inverseDirection = 1.0 / ray.direction;

        emission[i] = float3(0, 0, 0);
        if (!raycast(ray, hitT, hitNormal, hitVoxel))
        {
            float sunbrightness = dot(ray.direction, sundir.xyz);
            emission[i] += float3(sunstrength * max(0, sunbrightness));
        }

        if (!hit)
        {
            const float skystrength = .25;

            transmission[i] = float3(0, 0, 0);

            // sky emission
            emission[i] += float3(skystrength); // + 0.5 * float3(.392f, .584f, .929f);
            break;
        }
    }

    float3 color = float3(0, 0, 0);
    for (int j = MaxBounces - 1; j >= 0; j--)
    {
        color = color * transmission[j] + emission[j];
    }
    return color;
}

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

bool raycast(Ray ray, out float t, out float3 normal, out int3 voxel)
{
    voxel = int3(floor(ray.origin)) + int3(2 * 32, 0, 2 * 32);
    int3 step = int3(sign(ray.direction));

    if (all(step == 0))
    {
        t = 0;
        normal = int3(0);
        return false;
    }

    float3 start = ray.at(0);
    float3 end = ray.at(ray.length);

    float3 d = end - start;
    float3 tDelta = step / d;
    float3 tMax = tDelta * float3(
		step.x > 0 ? 1 - frac(start.x) : frac(start.x),
		step.y > 0 ? 1 - frac(start.y) : frac(start.y),
        step.z > 0 ? 1 - frac(start.z) : frac(start.z)
    );

    int steps = 100;
    while (bool(--steps))
    {
        int byte = ReadVoxelByte(int3(voxel));

        if (byte == 0xFF)
        {
            t = max(max(tMax.x, tMax.y), tMax.z);
            return true;
        }
        else if (byte != 0)
        {
            // int3 mask = (ray.at(t) - voxel) > .5;
            // int bitIdx = mask.x << 2 | mask.y << 1 | mask.z << 0;

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
                voxel.x += step.x;
                tMax.x += tDelta.x;
                normal = float3(-step.x, 0, 0);
            }
            else
            {
                voxel.z += step.z;
                tMax.z += tDelta.z;
                normal = float3(0, 0, -step.z);
            }
        }
        else
        {
            if (step.y != 0 && tMax.y < tMax.z)
            {
                voxel.y += step.y;
                tMax.y += tDelta.y;
                normal = float3(0, -step.y, 0);
            }
            else
            {
                voxel.z += step.z;
                tMax.z += tDelta.z;
                normal = float3(0, 0, -step.z);
            }
        }
    }

    return false;
}