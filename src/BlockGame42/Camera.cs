using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42;
internal class Camera
{
    public Transform transform = new();
    public float fieldOfView = float.Pi / 3f;
    public int width, height;

    public float AspectRatio => width / (float)height;

    public void Update(int width, int height)
    {
        this.width = width;
        this.height = height;
    }

    public Matrix4x4 ViewMatrix()
    {
        return Matrix4x4.CreateLookAt(transform.Position, transform.Position + transform.Forward, Vector3.UnitY);
    }

    public Matrix4x4 ProjectionMatrix()
    {
        return Matrix4x4.CreatePerspectiveFieldOfView(fieldOfView, AspectRatio, 0.1f, 1000f);
    }
}
