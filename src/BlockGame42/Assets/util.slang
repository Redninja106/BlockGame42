
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

float3 get_f0(float4 albedo, float4 specular)
{
    uint g = uint(specular.g * 255);

    if (g <= 229)
    {
        return specular.g;
    }

    if (g == 255 || g >= 230 + 8)
    {
        return albedo.xyz;
    }

    static const float3 MetalF0[8] =
    {
        float3(0.5312288253117692, 0.5123572424618409, 0.49582854571419704),
        float3(0.9442299660454834, 0.7761021173200883, 0.37340200459256084),
        float3(0.9122980315353793, 0.9138506314464109, 0.919680580953847),
        float3(0.555596817149677, 0.5545370757486425, 0.5547794275130405),
        float3(0.9259521962722096, 0.7209016380514688, 0.5041542417350733),
        float3(0.6324838129323365, 0.6259370736219204, 0.6414788995394),
        float3(0.6788492346579627, 0.6424005556526211, 0.588409633571299),
        float3(0.9619999988038569, 0.9494681120724692, 0.9221157109970759),
    };

    return MetalF0[g - 230];
}

float3 calc_reflectance(float4 albedo, float3 normal, float4 specular, float3 view_dir)
{
    float3 f0 = get_f0(albedo, specular);
    float cos_theta = saturate(dot(view_dir, normal));

    // schlick approx
    return f0 + (1.0 - f0) * pow(1.0 - cos_theta, 5);
}