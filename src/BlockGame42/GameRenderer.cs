using BlockGame42.GUI;
using BlockGame42.Rendering;

namespace BlockGame42;

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
