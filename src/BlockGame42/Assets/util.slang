
float3x3 calc_face_tbn_matrix(float3 hitNormal)
{
    int3 n = sign(hitNormal);
    float3 tangent = select(n.x != 0, n.x * float3(0, 0, 1), float3(1, 0, 0) * select(n.y != 0, n.y, -n.z));
    float3 bitangent = select(n.y != 0, n.y * float3(0, 0, 1), float3(0, 1, 0));
    return float3x3(
        tangent, bitangent, hitNormal
    );
}

float3 decode_normal(float2 xy_encoded_normal_01)
{
    float3 n;
    n.xy = xy_encoded_normal_01.xy * 2 - 1;
    n.z = sqrt(1 - dot(n.xy, n.xy));
    return n;
}

float2 encode_normal(float3 normal)
{
    return normalize(normal).xy * .5 + .5;
}