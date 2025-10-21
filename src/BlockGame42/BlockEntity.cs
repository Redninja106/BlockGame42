using BlockGame42.Blocks;
using BlockGame42.Chunks;
using BlockGame42.Rendering;
using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42;
internal class BlockEntity : Entity
{
    private Block block;
    private BlockState state;

    private BlockMesh mesh;
    private Matrix4x4 meshTransform;

    private Vector3 velocity;

    public BlockEntity(World world, Block block, BlockState state, Coordinates location) : base(world)
    {
        this.block = block;
        this.state = state;

        mesh = block.Model.GetMesh(state, out meshTransform);

        Teleport(new Transform() { Position = location.ToVector() });
    }

    public override void Draw(CommandBuffer commandBuffer, RenderPass pass)
    {
        Matrix4x4 transform = this.meshTransform * this.InterpolatedTransform.Matrix();
        commandBuffer.PushVertexUniformData(0, ref transform);
        mesh.Draw(commandBuffer, pass);

        base.Draw(commandBuffer, pass);
    }

    public override void Tick()
    {
        this.velocity += new Vector3(0, -1, 0) * Game.TimeStep;
        Transform newTransform = Transform.Translated(velocity);

        Coordinates posBelow = Coordinates.Floor(newTransform.Position);
        Block? below = World.GetBlock(posBelow, out var belowState);
        if (below is not EmptyBlock)
        {
            if (below is HalfBlock half && !state.HalfBlock.Doubled && !belowState.HalfBlock.Doubled)
            {
                World.SetBlock(posBelow, half, new BlockState() { HalfBlock = new(true, belowState.HalfBlock.Direction) });
            }
            else
            {
                World.SetBlock(Coordinates.Floor(this.Transform.Position), this.block, this.state);
            }

            World.RemoveEntity(this);
        }
        else
        {
            this.Transform = newTransform;
        }

        base.Tick();
    }
}

class BlockMeshRenderer
{
    GraphicsPipeline pipeline;

    public BlockMeshRenderer(GraphicsManager graphics)
    {
        pipeline = graphics.device.CreateGraphicsPipeline(new()
        {
            VertexShader = graphics.shaders.Get("blockmesh_vs"),
            FragmentShader = graphics.shaders.Get("blockmesh_fs"),
            TargetInfo = new GraphicsPipelineTargetInfo([
                new ColorTargetDescription(graphics.device.GetSwapchainTextureFormat(graphics.Window), default),
                ],
                TextureFormat.D24_UNorm_S8_UInt,
                true
                ),
            PrimitiveType = PrimitiveType.TriangleList,
            VertexInputState = new VertexInputState([
                new VertexBufferDescription(0, MinimizedChunkVertex.Size, VertexInputRate.Vertex, 0)
                ], MinimizedChunkVertex.Attributes),
            RasterizerState = new RasterizerState(FillMode.Fill, CullMode.Back, FrontFace.CounterClockwise, 0, 0, 0, false, true),
            MultisampleState = new MultisampleState(SampleCount._1, 0, false, false),
            DepthStencilState = new DepthStencilState(CompareOp.Less, default, default, 0, 0, true, true, false),
        });
    }

    public void RenderMesh(BlockMesh mesh, Matrix4x4 transform)
    {
    }
}

class BlockMesh
{
    private GraphicsManager graphics;
    private DataBuffer vertexBuffer;
    private int vertexCount;

    public BlockMesh(GraphicsManager graphics, MinimizedChunkVertex[] vertices)
    {
        this.graphics = graphics;

        uint sizeInBytes = (uint)(MinimizedChunkVertex.Size * vertices.Length);
        vertexBuffer = graphics.device.CreateDataBuffer(DataBufferUsageFlags.Vertex, sizeInBytes);

        using TransferBuffer transferBuffer = graphics.device.CreateTransferBuffer(TransferBufferUsage.Upload, sizeInBytes);

        Span<MinimizedChunkVertex> mappedBuffer = transferBuffer.Map<MinimizedChunkVertex>(false);
        vertices.CopyTo(mappedBuffer);
        transferBuffer.Unmap();

        CopyPass pass = graphics.CommandBuffer.BeginCopyPass();
        pass.UploadToDataBuffer(transferBuffer, 0, vertexBuffer, 0, sizeInBytes, false);
        pass.End();

        vertexCount = vertices.Length;
    }

    public static BlockMesh CreateFromModel(GraphicsManager graphics, BlockModel model, BlockState blockState)
    {
        BlockMeshBuilder meshBuilder = new();

        Material material = model.GetMaterial(blockState);
        for (int i = 0; i < 6; i++)
        {
            meshBuilder.AppendPartialBlockFace(
                model.GetFaceMask(blockState, (Direction)i),
                ChunkMesh.faceOffsets[i].ToVector(),
                ChunkMesh.rightDirs[i].ToVector(),
                ChunkMesh.upDirs[i].ToVector(),
                ChunkMesh.forwardDirs[i].ToVector(),
                material.Data.Transmission[(Direction)i],
                material.Data.Emission[(Direction)i]
                );
        }

        model.AddInternalFaces(blockState, meshBuilder);

        return new BlockMesh(graphics, meshBuilder.vertices.ToArray());
    }

    public static BlockMesh CreateFromItemTextureData(GraphicsManager graphics, TextureData itemTexture)
    {
        throw new NotImplementedException();
    }

    public void Draw(CommandBuffer commandBuffer, RenderPass pass)
    {
        pass.BindVertexBuffers(0, [new(this.vertexBuffer)]);
        pass.DrawPrimitives((uint)this.vertexCount, 1, 0, 0);
    }
}

