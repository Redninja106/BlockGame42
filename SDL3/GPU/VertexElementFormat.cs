namespace SDL.GPU;

public enum VertexElementFormat
{
    Invalid,

    /* 32-bit Signed Integers */
    Int,
    Int2,
    Int3,
    Int4,

    /* 32-bit Unsigned Integers */
    UInt,
    UInt2,
    UInt3,
    UInt4,

    /* 32-bit Floats */
    Float,
    Float2,
    Float3,
    Float4,

    /* 8-bit Signed Integers */
    Byte2,
    Byte4,

    /* 8-bit Unsigned Integers */
    UByte2,
    UByte4,

    /* 8-bit Signed Normalized */
    Byte2Norm,
    Byte4Norm,

    /* 8-bit Unsigned Normalized */
    UByte2Norm,
    UByte4Norm,

    /* 16-bit Signed Integers */
    Short2,
    Short4,

    /* 16-bit Unsigned Integers */
    UShort2,
    UShort4,

    /* 16-bit Signed Normalized */
    Short2Norm,
    Short4Norm,

    /* 16-bit Unsigned Normalized */
    UShort2Norm,
    UShort4Norm,

    /* 16-bit Floats */
    Half2,
    Half4
}