
using System.Runtime.InteropServices;

namespace SDL.GPU;

public struct DepthStencilState
{
    public CompareOp CompareOp;
    public StencilOpState BackStencilState;
    public StencilOpState FrontStencilState;
    public byte CompareMask;
    public byte WriteMask;
    public bool EnableDepthTest;
    public bool EnableDepthWrite;
    public bool EnableStencilTest;

    public DepthStencilState(CompareOp compareOp, StencilOpState backStencilState, StencilOpState frontStencilState, byte compareMask, byte writeMask, bool enableDepthTest, bool enableDepthWrite, bool enableStencilTest)
    {
        CompareOp = compareOp;
        BackStencilState = backStencilState;
        FrontStencilState = frontStencilState;
        CompareMask = compareMask;
        WriteMask = writeMask;
        EnableDepthTest = enableDepthTest;
        EnableDepthWrite = enableDepthWrite;
        EnableStencilTest = enableStencilTest;
    }

    internal SDL_GPUDepthStencilState Marshal(ref MarshalAllocator allocator)
    {
        return new SDL_GPUDepthStencilState
        {
            compare_op = (SDL_GPUCompareOp)CompareOp,
            back_stencil_state = BackStencilState.Marshal(),
            front_stencil_state = FrontStencilState.Marshal(),
            compare_mask = CompareMask,
            write_mask = WriteMask,
            enable_depth_test = EnableDepthTest,
            enable_depth_write = EnableDepthWrite,
            enable_stencil_test = EnableStencilTest
        };
    }
}
