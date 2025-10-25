using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Rendering;
internal class TileLookupManager
{
    private readonly GraphicsManager graphics;

    private readonly DataBuffer checksums;
    private readonly DataBuffer payloads;

    public uint PhaseCount { get; }
    public uint TilesPerPhase { get; }
    public uint TotalRecords { get; }
    public uint FramesPerPhase { get; }

    public uint ChecksumPhaseSizeInBytes { get; }
    public uint PayloadPhaseSizeInBytes { get; }

    public uint CurrentFrame { get; private set; }
    public uint CurrentPhase { get; private set; } = 1;


    public TileLookupManager(GraphicsManager graphics, uint phaseCount, uint tilesPerPhase, uint framesPerPhase)
    {
        this.graphics = graphics;

        this.PhaseCount = phaseCount;
        this.TilesPerPhase = tilesPerPhase;
        this.TotalRecords = PhaseCount * TilesPerPhase;
        this.FramesPerPhase = framesPerPhase;

        ChecksumPhaseSizeInBytes = TilesPerPhase * sizeof(uint);
        PayloadPhaseSizeInBytes = TilesPerPhase * sizeof(uint) * 4;

        checksums = graphics.device.CreateDataBuffer(
            DataBufferUsageFlags.GraphicsStorageRead | DataBufferUsageFlags.ComputeStorageWrite, 
            TotalRecords * sizeof(int)
            );

        payloads = graphics.device.CreateDataBuffer(
            DataBufferUsageFlags.GraphicsStorageRead | DataBufferUsageFlags.ComputeStorageWrite,
            TotalRecords * 4 * sizeof(int)
            );

        Console.WriteLine($"tile lookup is {(checksums.Size + payloads.Size) >> 20}MB ({checksums.Size >> 20}MB checksums, {payloads.Size >> 20}MB payloads)");

    }

    public DataBuffer GetChecksums()
    {
        return checksums;
    }

    public DataBuffer GetPayloads() 
    { 
        return payloads; 
    }

    public void ClearPhase(uint phase)
    {
        graphics.ClearDataBufferRange(checksums, phase * ChecksumPhaseSizeInBytes, ChecksumPhaseSizeInBytes, false);
        graphics.ClearDataBufferRange(payloads, phase * PayloadPhaseSizeInBytes, PayloadPhaseSizeInBytes, false);
    }

    public void PhaseTick()
    {
        CurrentFrame++;

        if (CurrentFrame >= FramesPerPhase)
        {
            CurrentFrame = 0;
            CurrentPhase++;

            if (CurrentPhase >= PhaseCount)
            {
                CurrentPhase = 0;
            }
    
            // clear the new phase for rendering
            ClearPhase(CurrentPhase);
        }

    }
}
