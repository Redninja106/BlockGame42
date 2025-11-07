using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BlockGame42.Blocks;
using BlockGame42.GUI;
using BlockGame42.Rendering;
using SDL;
using SDL.GPU;

namespace BlockGame42;

class Game : Application
{
    private Window window = null!;
    // private Device device = null!;
    // private GraphicsPipeline pipeline = null!;
    // private DataBuffer vertexBuffer = null!;
    // private TransferBuffer transferBuffer = null!;


    IAssetSource assets = null!;
    public static GraphicsManager graphics = null!;
    private World world;
    public static Player player = null!;
    public static GameRenderer gameRenderer = null!;

    public static float TimeStep = 1 / 20f;

    public static Viewport viewport = null!;

    public static TextureIndex Textures { get; private set; } = null!;
    public static MaterialIndex Materials { get; private set; } = null!;

    public static BlockRegistry Blocks { get; private set; } = null!;

    protected override void OnInit()
    {
        window = new Window("Block Game", 1920, 1080, WindowFlags.Resizable);
        window.SetRelativeMouseMode(true);

        assets = new DirectoryAssetSource("Assets");
        graphics = new GraphicsManager(window, assets);

        Load();

        //  Textures = new(graphics);

        // device.ClaimWindow(window);

        //// TRANSFER BUFFER
        //transferBuffer = device.CreateTransferBuffer(TransferBufferUsage.Upload, 64 * 1024 * 1024);
        //var mapped = transferBuffer.Map<Vector4>(false);
        //Vector4[] vertices = [
        //    new(.5f, -.5f, 0, 1),
        //     new(-.5f, -.5f, 0, 1),
        //     new(0, .5f, 0, 1),
        //     ];
        //vertices.CopyTo(mapped);
        //transferBuffer.Unmap();

        //// DATA BUFFER
        //vertexBuffer = device.CreateDataBuffer<Vector4>(DataBufferUsageFlags.Vertex, vertices.Length);
        //var commandBuffer = device.AcquireCommandBuffer();
        //var copyPass = commandBuffer.BeginCopyPass();
        //copyPass.UploadToBuffer(new() { TransferBuffer = transferBuffer, Offset = 0 }, new() { Buffer = vertexBuffer, Offset = 0, Size = vertexBuffer.Size }, false);
        //copyPass.End();
        //commandBuffer.Submit();

        //var pipelineOptions = new GraphicsPipelineCreateInfo()
        //{
        //    VertexShader = graphics.shaders.Get("chunk_vs"),
        //    FragmentShader = graphics.shaders.Get("chunk_fs"),
        //    PrimitiveType = PrimitiveType.TriangleList,
        //    VertexInputState = new()
        //    {
        //        VertexBufferDescriptions = [new() { Slot = 0, Pitch = (uint)Unsafe.SizeOf<Vector4>(), InputRate = VertexInputRate.Vertex }],
        //        VertexAttributes = [new() { Format = VertexElementFormat.Float4 }]
        //    },
        //    DepthStencilState = new()
        //    {
        //    },
        //    MultisampleState = new()
        //    {
        //    },
        //    RasterizerState = new()
        //    {
        //        CullMode = CullMode.None,
        //        FillMode = FillMode.Fill,
        //    },
        //    TargetInfo = new()
        //    {
        //        ColorTargetDescriptions = [new() { Format = device.GetSwapchainTextureFormat(window) }],
        //    }
        //};

        //pipeline = device.CreateGraphicsPipeline(pipelineOptions);

        viewport = new();
    }

    public void Load()
    {
        graphics.AcquireCommandBuffer();
        //graphics.transferBatcher.BeginBatch(graphics.CommandBuffer);

        Textures = new(graphics);
        Materials = new(graphics);

        Blocks = new();
        world = new(graphics);
        player = new(world);
        player.Transform.Position = new Vector3(0, 50, 0);
        player.Transform.Rotation = Quaternion.Identity;
        gameRenderer = new(graphics, assets, world, player);

        //graphics.transferBatcher.EndBatch();

        Textures.GenerateMipmaps(graphics.CommandBuffer);

        graphics.CommandBuffer.Submit();

    }

    float accumulatedTickTime;
    public static float TickProgress;

    protected override void OnFrame(float deltaTime)
    {
        graphics.AcquireCommandBuffer();

        float framerate = 1f / deltaTime;
        deltaTime = float.Min(deltaTime, 1 / 30f);

        accumulatedTickTime += deltaTime;

        player.Update(deltaTime);
        player.Camera.Update(window.Width, window.Height);

        TickProgress = accumulatedTickTime / TimeStep;
        world.Update();

        while (accumulatedTickTime > 1 / 20f)
        {
            window.SetTitle($"Block Game - {framerate:N}FPS");
            accumulatedTickTime -= 1 / 20f;
            world.Tick();
        }

        world.Chunks.BuildStaleChunks();

        if (graphics.BeginFrame())
        {
            gameRenderer.Render();
            graphics.EndFrame();
        }

        //float t = GetTicks() / 1000f;

        //Matrices matrices;
        //matrices.world = Matrix4x4.CreateRotationY(t) * Matrix4x4.CreateScale(5);
        //matrices.view = Matrix4x4.CreateLookAt(new(0, 5, 5), new(0, 0, 0), new(0, 1, 0));
        //matrices.proj = Matrix4x4.CreatePerspectiveFieldOfView(float.Pi / 2f, 1f, 0.01f, 100f);
        //commandBuffer.PushVertexUniformData<Matrices>(0, new(ref matrices));

        //Vector4 color = new(.5f + .5f * float.Sin(t * 2.5234f), .5f + .5f * float.Cos(t * .95134f + .5234f), .5f + .5f * float.Cos(t * 1.5234f + .08123f), 1);
        //commandBuffer.PushFragmentUniformData<Vector4>(0, new(ref color));

        //pass.BindVertexBuffers(0, [new() { Buffer = vertexBuffer, Offset = 0 }]);
        //pass.BindGraphicsPipeline(pipeline);
        //pass.DrawPrimitives(3, 1, 0, 0);

        //pass.End();

        //commandBuffer.Submit();
    }
}

struct FaceInfo
{
    Vector3 position;
    Vector3 right;
    Vector3 up;
    Vector3 forward;
    uint textureID;
}


class GameRenderer(GraphicsManager graphics, IAssetSource assets, World world, Player player)
{
    public readonly World world = world;
    public readonly Player player = player;

    public BlockMeshRenderer BlockMeshRenderer { get; } = new(graphics);
    public ChunkRenderer ChunkRenderer { get; } = new(graphics);
    public GUIRenderer GUIRenderer { get; } = new(graphics);
    public OverlayRenderer OverlayRenderer { get; } = new(graphics);
    public Font Font { get; } = new(graphics, assets, "SpaceGrotesk-Regular");

    public Viewport Viewport { get; } = new();

    public void Render()
    {
        graphics.RenderTargets.Clear(graphics.CommandBuffer);

        ChunkRenderer.Render(player.Camera, world.Chunks);

        OverlayRenderer.Begin(player.Camera);
        if (player.hoveredCoordinates != null)
        {
            OverlayRenderer.PushBox(new(player.hoveredCoordinates.Value.ToVector(), player.hoveredCoordinates.Value.ToVector() + Vector3.One), 0xFF555555);
        }
        OverlayRenderer.PushLine(new(0, 25, 0), new(1, 25, 0), 0xFF0000FF);
        OverlayRenderer.PushLine(new(0, 25, 0), new(0, 26, 0), 0xFF00FF00);
        OverlayRenderer.PushLine(new(0, 25, 0), new(0, 25, 1), 0xFFFF0000);
        
        OverlayRenderer.Flush();

        GUIRenderer.BeginFrame(world.Graphics.Window.Width, world.Graphics.Window.Height);

        DrawCrosshair();
        Game.player.Render();

        GUIRenderer.EndFrame();
    }

    void DrawCrosshair()
    {
        const float thickness = 1f;
        const float size = 5f;

        float cx = world.Graphics.Window.Width / 2f, cy = world.Graphics.Window.Height / 2f;

        GUIRenderer.PushLine(new(cx - size, cy), new(cx + size, cy), 0xFFE8E8E8, thickness);
        GUIRenderer.PushLine(new(cx, cy - size), new(cx, cy + size), 0xFFE8E8E8, thickness);
    }
}
