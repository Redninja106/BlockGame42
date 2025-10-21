using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Rendering;
internal class MaterialIndex
{
    private static readonly uint MaterialDataSize = (uint)Unsafe.SizeOf<MaterialData>();

    private readonly GraphicsManager graphics;
    
    private List<Material> materials = [];

    // private TransferBuffer materialTransferBuffer;
    private DataBuffer materialBuffer;
    private MaterialData[] materialBufferData;

    public MaterialIndex(GraphicsManager graphics)
    {
        this.graphics = graphics;

        // materialTransferBuffer = graphics.device.CreateTransferBuffer(TransferBufferUsage.Upload, MaterialDataSize);

        EnsureBufferSize(256);
    }

    public int Get(Material material)
    {
        return materials.IndexOf(material);
    }

    public void Add(Material material)
    {
        materials.Add(material);
        materialBufferData[Get(material)] = material.Data;
        UploadMaterialData(Get(material), material.Data);
    }

    public DataBuffer GetMaterialBuffer()
    {
        return materialBuffer;
    }

    private void UploadMaterialData(int index, MaterialData data)
    {
        using TransferBuffer transferBuffer = graphics.device.CreateTransferBuffer(TransferBufferUsage.Upload, materialBuffer.Size);
        Span<MaterialData> transferBufferData = transferBuffer.Map<MaterialData>(false);
        materialBufferData.CopyTo(transferBufferData);
        transferBuffer.Unmap();

        CopyPass pass = graphics.CommandBuffer.BeginCopyPass();
        pass.UploadToDataBuffer(transferBuffer, 0, materialBuffer, 0, materialBuffer.Size, false);
        pass.End();
    }

    [MemberNotNull(nameof(materialBuffer), nameof(materialBufferData))]
    private void EnsureBufferSize(uint materialCount)
    {
        uint minSize = MaterialDataSize * materialCount;
        if (materialBuffer == null || materialBuffer.Size < minSize)
        {
            uint newSize = (materialBuffer?.Size * 2) ?? (materialCount * MaterialDataSize);
            materialBuffer?.Dispose();
            materialBuffer = graphics.device.CreateDataBuffer(DataBufferUsageFlags.GraphicsStorageRead | DataBufferUsageFlags.ComputeStorageRead, newSize);
            materialBufferData = new MaterialData[materialBuffer.Size / MaterialDataSize];
        }

        materialBufferData ??= new MaterialData[materialBuffer.Size / MaterialDataSize];
    }
}
