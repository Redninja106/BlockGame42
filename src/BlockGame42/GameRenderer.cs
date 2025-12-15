using BlockGame42.GUI;
using BlockGame42.Rendering;
using SDL.GPU;

namespace BlockGame42;

class GameRenderer(GraphicsContext graphics)
{
    public GraphicsContext Graphics => graphics;
    public BlockMeshRenderer BlockMeshRenderer { get; } = new(graphics);
    public ChunkRenderer ChunkRenderer { get; } = new(graphics);
    public GUIRenderer GUIRenderer { get; } = new(graphics);
    public OverlayRenderer Overlays { get; } = new(graphics);
    public Font Font { get; private set; }

    // public MaterialManager Materials { get; } = new(graphics);
    public MaterialManager Textures { get; } = new(graphics, 16, 16);

    public GUIViewport Viewport { get; } = new();

    public void Render(Camera camera, World world)
    {
        graphics.RenderTargets.Clear(graphics.CommandBuffer);

        ChunkRenderer.Render(this, camera, world);

        Overlays.Begin(camera);
        // if (client.Interaction.Player.hoveredCoordinates != null)
        // {
        //     Overlays.PushBox(new(client.Interaction.Player.hoveredCoordinates.Value.ToVector(), client.Interaction.Player.hoveredCoordinates.Value.ToVector() + Vector3.One), 0xFF555555);
        // }
        Overlays.PushLine(new(0, 25, 0), new(1, 25, 0), 0xFF0000FF);
        Overlays.PushLine(new(0, 25, 0), new(0, 26, 0), 0xFF00FF00);
        Overlays.PushLine(new(0, 25, 0), new(0, 25, 1), 0xFFFF0000);
        
        Overlays.Flush();

        GUIRenderer.BeginFrame(graphics.Window.Width, graphics.Window.Height);

        DrawCrosshair();
        // Game.player.Render();

        GUIRenderer.EndFrame();
    }

    void DrawCrosshair()
    {
        const float thickness = 1f;
        const float size = 5f;

        float cx = graphics.Window.Width / 2f, cy = graphics.Window.Height / 2f;

        GUIRenderer.PushLine(new(cx - size, cy), new(cx + size, cy), 0xFFE8E8E8, thickness);
        GUIRenderer.PushLine(new(cx, cy - size), new(cx, cy + size), 0xFFE8E8E8, thickness);
    }

    public void Load(IAssetSource assets)
    {
        Graphics.AcquireCommandBuffer();
        Textures.Load();
        Textures.GenerateMipmaps(Graphics.CommandBuffer);
        // Materials.Load();
        Font = new(graphics, assets, "SpaceGrotesk-Regular");
        Graphics.CommandBuffer.Submit();
    }
}
