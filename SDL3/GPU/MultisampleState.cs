namespace SDL.GPU;

public struct MultisampleState
{
    public SampleCount SampleCount;
    public uint SampleMask;
    public bool EnableMask;
    public bool EnableAlphaToCoverage;

    public MultisampleState(SampleCount sampleCount, uint sampleMask, bool enableMask, bool enableAlphaToCoverage)
    {
        SampleCount = sampleCount;
        SampleMask = sampleMask;
        EnableMask = enableMask;
        EnableAlphaToCoverage = enableAlphaToCoverage;
    }

    internal SDL_GPUMultisampleState Marshal(ref MarshalAllocator allocator) => new()
    {
        sample_count = (SDL_GPUSampleCount)SampleCount,
        sample_mask = SampleMask,
        enable_mask = EnableMask,
        enable_alpha_to_coverage = EnableAlphaToCoverage,
    };
}
