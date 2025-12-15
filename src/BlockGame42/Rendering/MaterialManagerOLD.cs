using BlockGame42.Blocks.Models;
using Protor;
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
internal class MaterialManagerOLD
{
    private static readonly uint MaterialDataSize = (uint)Unsafe.SizeOf<MaterialData>();

    private readonly GraphicsContext graphics;
    
    private List<MaterialData> materials = [];

    private DataBuffer materialBuffer;
    private MaterialData[] materialBufferData;

    public MaterialManagerOLD(GraphicsContext graphics)
    {
        this.graphics = graphics;
    }

    public void Load()
    {
        uint index = 0;
        BlockModel[] models = Registry.GetAll<BlockModel>();
        materialBufferData = new MaterialData[models.Length];
        foreach (var model in models)
        {
            materialBufferData[index].TextureIDs.Fill(model.Material.TexID);
            // model.MaterialID = index;
            index++;
        }

        using TransferBuffer transferBuffer = graphics.device.CreateTransferBuffer(TransferBufferUsage.Upload, MaterialDataSize * (uint)models.Length);
        var transferBufferData = transferBuffer.Map<MaterialData>(false);
        materialBufferData.CopyTo(transferBufferData);
        transferBuffer.Unmap();

        materialBuffer = graphics.device.CreateDataBuffer<MaterialData>(DataBufferUsageFlags.GraphicsStorageRead | DataBufferUsageFlags.ComputeStorageRead, models.Length);
        var copyPass = graphics.CommandBuffer.BeginCopyPass();
        copyPass.UploadToDataBuffer(transferBuffer, 0, materialBuffer, 0, transferBuffer.Size, false);
        copyPass.End();
    }

    //public int Get(MaterialData material)
    //{
    //    return materials.IndexOf(material);
    //}

    //public void Add(BlockTextures material)
    //{
    //    materials.Add(material);
    //    materialBufferData[Get(material)] = material.Data;
    //    UploadMaterialData(Get(material), material.Data);
    //}

    public DataBuffer GetMaterialBuffer()
    {
        return materialBuffer;
    }

    //private void UploadMaterialData(int index, Material data)
    //{
    //    using TransferBuffer transferBuffer = graphics.device.CreateTransferBuffer(TransferBufferUsage.Upload, materialBuffer.Size);
    //    Span<Material> transferBufferData = transferBuffer.Map<Material>(false);
    //    materialBufferData.CopyTo(transferBufferData);
    //    transferBuffer.Unmap();

    //    CopyPass pass = graphics.CommandBuffer.BeginCopyPass();
    //    pass.UploadToDataBuffer(transferBuffer, 0, materialBuffer, 0, materialBuffer.Size, false);
    //    pass.End();
    //}

    //[MemberNotNull(nameof(materialBuffer), nameof(materialBufferData))]
    //private void EnsureBufferSize(uint materialCount)
    //{
    //    uint minSize = MaterialDataSize * materialCount;
    //    if (materialBuffer == null || materialBuffer.Size < minSize)
    //    {
    //        uint newSize = (materialBuffer?.Size * 2) ?? (materialCount * MaterialDataSize);
    //        materialBuffer?.Dispose();
    //        materialBuffer = graphics.device.CreateDataBuffer(DataBufferUsageFlags.GraphicsStorageRead | DataBufferUsageFlags.ComputeStorageRead, newSize);
    //        materialBufferData = new Material[materialBuffer.Size / MaterialDataSize];
    //    }

    //    materialBufferData ??= new Material[materialBuffer.Size / MaterialDataSize];
    //}
}
