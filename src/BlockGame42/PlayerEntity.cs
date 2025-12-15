using BlockGame42.Blocks;
using BlockGame42.Chunks;
using Protor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42;
internal class PlayerEntity : Entity
{
    float cx, cy;
    Vector3 velocity;
    Box collider = new(new(-.3f, 0, -.3f), new(.3f, 1.8f, .3f));
    bool airborne;

    public float Acceleration = 50f;
    public float MaxSpeed = 5f;
    public float Gravity = -20f;
    public float JumpVelocity = 7f;

    public Coordinates? hoveredCoordinates;

    public Camera Camera { get; } = new();
    MouseButtonFlags lastMouseButtons;
    bool[]? lastKeyboardState;

    public PlayerEntity(World world) : base(world)
    {
    }

    public void Update(float deltatime)
    {
        MouseButtonFlags mouseButtons = Mouse.GetRelativeState(out float mouseX, out float mouseY);
        // var window = (World. as GameClient).Graphics.Window;

        KeyboardState keyboard = Keyboard.GetState();

        if (Mouse.TryCapture(!keyboard[Scancode.LAlt]))
        {
            if (!keyboard[Scancode.LAlt])
            {
                cx += 0.001f * mouseY;
                cy -= 0.001f * mouseX;
            }
        }

        Transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, cy) * Quaternion.CreateFromAxisAngle(Vector3.UnitX, cx);
        
        Vector3 targetVelocity = Vector3.Zero;

        if (keyboard[Scancode.W]) targetVelocity += MaxSpeed * Vector3.UnitZ;
        if (keyboard[Scancode.A]) targetVelocity += MaxSpeed * Vector3.UnitX;
        if (keyboard[Scancode.S]) targetVelocity -= MaxSpeed * Vector3.UnitZ;
        if (keyboard[Scancode.D]) targetVelocity -= MaxSpeed * Vector3.UnitX;
        // if (keyboard[Scancode.C]) targetVelocity -= Vector3.UnitY;
        // if (keyboard[Scancode.Space]) targetVelocity += Vector3.UnitY;

        targetVelocity = Vector3.Transform(targetVelocity, Quaternion.CreateFromAxisAngle(Vector3.UnitY, cy));

        this.velocity = Vector3.UnitY * this.velocity.Y + new Vector3(1, 0, 1) * Step(new Vector3(1, 0, 1) * this.velocity, targetVelocity, Acceleration * deltatime);

        Camera.transform = this.Transform.Translated(new Vector3(0, 1.5f, 0));

        if ((mouseButtons & MouseButtonFlags.Right) != 0 && (lastMouseButtons & MouseButtonFlags.Right) == 0)
        {
            Ray ray = new(this.Camera.transform.Position, this.Camera.transform.Forward, 100);
            if (World.Raycast(ray, out float t, out Coordinates hitCoords, out Coordinates normal))
            {
                World.SetBlock(hitCoords, BlockRegistry.Air);
            }
        }


        if (!airborne && keyboard[Scancode.Space])
        {
            this.velocity.Y = JumpVelocity;
            airborne = true;
        }
        
        this.velocity.Y += Gravity * deltatime;

        if (!TryMove(Vector3.UnitX * this.velocity.X * deltatime))
        {
            this.velocity.X = 0;
        }

        if (!TryMove(Vector3.UnitY * this.velocity.Y * deltatime))
        {
            this.velocity.Y = 0;
            airborne = false;
        }
        else
        {
            airborne = true;
        }

        if (!TryMove(Vector3.UnitZ * this.velocity.Z * deltatime))
        {
            this.velocity.Z = 0;
        }

        {
            Ray ray = new(Camera.transform.Position, Camera.transform.Forward, 100);
            if (World.Raycast(ray, out float t, out Coordinates hitCoords, out Coordinates normal))
            {
                hoveredCoordinates = hitCoords;
            }
            else
            {
                hoveredCoordinates = null;
            }
        }

        if (keyboard[Scancode.L])
        {
            // Game.gameRenderer.ChunkRenderer.sundir = new Vector4(-this.Transform.Forward, 1);
        }

        if ((mouseButtons & MouseButtonFlags.Middle) != 0 && (lastMouseButtons & MouseButtonFlags.Middle) == 0)
        {
            placementIdx++;
            placementIdx %= placementArray.Length;
        }

        if (keyboard[Scancode.E] && !(lastKeyboardState?[(int)Scancode.E] ?? false))
        {
            placementIdx++;
            placementIdx %= placementArray.Length;
        }

        if ((mouseButtons & MouseButtonFlags.Left) != 0 && (lastMouseButtons & MouseButtonFlags.Left) == 0)
        {
            Block placingBlock = placementArray[placementIdx];
            placingBlock.PlacementHandler.OnPlaceBlock(World, Camera, placingBlock);
        }
        
        lastMouseButtons = mouseButtons;
        lastKeyboardState = keyboard.ToArray();
    }

    string[] nameArray = ["stone", "glowstone", "dirt", "iron block", "player", "redstone lamp on"];
    Block[] placementArray = [BlockRegistry.Stone, BlockRegistry.Glowstone, BlockRegistry.Dirt, BlockRegistry.IronBlock, Registry.Get<Block>("redstone_lamp_on")];
    int placementIdx = 0;

    public void Render(GameRenderer renderer)
    {
        Ray ray = new(Camera.transform.Position, Camera.transform.Forward, 100);
        renderer.GUIRenderer.PushText(renderer.Font, nameArray[placementIdx], new(5,30), 0xFFFFFFFF);
        
        if (World.Raycast(ray, out float t, out Coordinates hitCoords, out Coordinates normal))
        {
            renderer.GUIRenderer.PushText(renderer.Font, World.GetBlockReference(hitCoords).Support.ToString(), new(renderer.Graphics.Window.Width/2f, renderer.Graphics.Window.Height/2f), 0xFFFFFFFF);
        }
    }

    private static Vector3 Step(Vector3 vector, Vector3 target, float distance)
    {
        Vector3 delta = target - vector;
        if (delta.LengthSquared() < distance * distance)
        {
            return target;
        }
        return vector + distance * Vector3.Normalize(delta);
    }

    private bool TryMove(Vector3 movement)
    {
        if (World.Intersect(this.collider, this.Transform.Position + movement))
        {
            return false;
        }
        this.Transform.Position += movement;
        return true;
    }
}
