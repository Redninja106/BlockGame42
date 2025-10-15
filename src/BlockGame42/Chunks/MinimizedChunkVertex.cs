using SDL.GPU;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace BlockGame42.Chunks;

struct MinimizedChunkVertex
{
    public uint x_y_z_u_v;
    public uint ao_texid;

    public static readonly VertexAttribute[] Attributes = [new VertexAttribute(0, 0, VertexElementFormat.UInt2, 0)];
    public static uint Size => (uint)Unsafe.SizeOf<MinimizedChunkVertex>();

    public MinimizedChunkVertex(Vector3 position, Vector2 textureCoordinates, uint blockTextureId, Vector4 ambientOcclusion, Vector3 normal)
    {
        uint x = (uint)(position.X * 4) & 0b11111111;
        uint y = (uint)(position.Y * 4) & 0b11111111;
        uint z = (uint)(position.Z * 4) & 0b11111111;
        uint u = (uint)(textureCoordinates.X * 4) & 0b111;
        uint v = (uint)(textureCoordinates.Y * 4) & 0b111;

        x_y_z_u_v = 0;
        x_y_z_u_v |= x << 24;
        x_y_z_u_v |= y << 16;
        x_y_z_u_v |= z << 8;
        x_y_z_u_v |= u << 4;
        x_y_z_u_v |= v << 0;

        uint ao = 0;
        ao |= ((uint)ambientOcclusion.W & 0b1) << 0;
        ao |= ((uint)ambientOcclusion.Z & 0b1) << 1;
        ao |= ((uint)ambientOcclusion.Y & 0b1) << 2;
        ao |= ((uint)ambientOcclusion.X & 0b1) << 3;

        uint texid = blockTextureId & 0b11111111111;

        uint n_x = (uint)(normal.X + 1f) & 0b11;
        uint n_y = (uint)(normal.Y + 1f) & 0b11;
        uint n_z = (uint)(normal.Z + 1f) & 0b11;

        ao_texid = 0;
        ao_texid |= ao << 28;
        ao_texid |= texid << 17;
        ao_texid |= n_x << 15;
        ao_texid |= n_y << 13;
        ao_texid |= n_z << 11;
    }

}
