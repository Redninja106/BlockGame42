using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BlockGame42.Blocks;
using BlockGame42.Chunks;
using BlockGame42.GUI;
using BlockGame42.Rendering;
using Protor;
using SDL;
using SDL.GPU;

namespace BlockGame42;

class GameClient : Application
{
    private Window window = null!;
    // private Device device = null!;
    // private GraphicsPipeline pipeline = null!;
    // private DataBuffer vertexBuffer = null!;
    // private TransferBuffer transferBuffer = null!;


    // IAssetSource assets = null!;
    // public static GraphicsManager graphics = null!;
    // private World world;
    // public static Player player = null!;
    // public static GameRenderer gameRenderer = null!;

    public static float TimeStep = 1 / 20f;

    // public static Viewport viewport = null!;

    // public static TextureIndex Textures { get; private set; } = null!;
    // public static MaterialIndex Materials { get; private set; } = null!;

    // public static BlockRegistry Blocks { get; private set; } = null!;

    public GraphicsContext Graphics { get; private set; }
    public InteractionContext Interaction { get; private set; }
    public GUIViewport Viewport { get; private set; }
    public GameRenderer Renderer { get; private set; }
    public World World { get; private set; }
    public ClientChunkManager ChunkManager { get; private set; }

    protected override void OnInit()
    {
        window = new Window("Block Game", 1920, 1080, WindowFlags.Resizable);

        var assets = new DirectoryAssetSource("Assets");
        Graphics = new GraphicsContext(window, assets);

        Renderer = new(Graphics);

        Interaction = new();

        ChunkManager = new(this);

        Load(assets);

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

        // viewport = new();
    }

    public void Load(IAssetSource assets)
    {
        //graphics.transferBatcher.BeginBatch(graphics.CommandBuffer);

        Registry.AddAssembly(Assembly.GetExecutingAssembly());
        Registry.Load();

        Renderer.Load(assets);

        // blocks = new(context);
        World = new World();
        var player = new PlayerEntity(World);
        player.Transform.Position = new Vector3(0, 30, 0);
        player.Transform.Rotation = Quaternion.Identity;
        
        World.AddEntity(player);
        Interaction.Player = player;

        ChunkManager.Initialize();

        // Interaction.Player = new(World);

        //graphics.transferBatcher.EndBatch();


    }

    float accumulatedTickTime;
    public static float TickProgress;

    protected override void OnFrame(float deltaTime)
    {
        window.SetRelativeMouseMode(window.HasKeyboardFocus());

        Graphics.AcquireCommandBuffer();

        float framerate = 1f / deltaTime;
        deltaTime = float.Min(deltaTime, 1 / 30f);

        accumulatedTickTime += deltaTime;

        Interaction.Player.Update(deltaTime);
        Interaction.Player.Camera.Update(window.Width, window.Height);

        ChunkManager.Update(Interaction.Player);

        TickProgress = accumulatedTickTime / TimeStep;
        World.Update();

        while (accumulatedTickTime > 1 / 20f)
        {
            window.SetTitle($"Block Game - {framerate:N}FPS");
            accumulatedTickTime -= 1 / 20f;
            World.Tick();
        }

        // World.Chunks.BuildStaleChunks();

        if (Graphics.BeginFrame())
        {
            Renderer.Render(Interaction.Player.Camera, World);
            Graphics.EndFrame();
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


class InteractionContext
{
    public PlayerEntity Player { get; set; }
}
