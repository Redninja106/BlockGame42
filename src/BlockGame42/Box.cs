using BlockGame42.Chunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42;
struct Box
{
    public Vector3 Min;
    public Vector3 Max;

    public Box(Vector3 a, Vector3 b)
    {
        Min = Vector3.Min(a, b);
        Max = Vector3.Max(a, b);
    }

    public Box Offset(Vector3 offset)
    {
        return new(Min + offset, Max + offset);
    }

    public bool Raycast(Ray ray, out float tNear, out float tFar, out Coordinates normal)
    {
        Vector3 t1 = (Min - ray.Origin) * ray.InverseDirection;
        Vector3 t2 = (Max - ray.Origin) * ray.InverseDirection;

        Vector3 tmin = Vector3.Min(t1, t2);
        Vector3 tmax = Vector3.Max(t1, t2);

        tNear = float.Max(float.Max(tmin.X, tmin.Y), tmin.Z);
        tFar = float.Min(float.Min(tmax.X, tmax.Y), tmax.Z);

        normal = new Coordinates(0, 1, 0);

        if (tFar >= float.Max(tNear, 0))
        {
            int xMask = (tmin.X == tNear) ? -1 : 0;
            int yMask = (tmin.Y == tNear) ? -1 : 0;
            int zMask = (tmin.Z == tNear) ? -1 : 0;

            normal = new Coordinates(
                xMask * float.Sign(ray.Direction.X),
                yMask * float.Sign(ray.Direction.Y),
                zMask * float.Sign(ray.Direction.Z)
            );

            return true;
        }
        else
        {
            normal = default;
            return false;
        }

        //if (tFar < 0 || tNear > tFar)
        //{
        //    normal = default;
        //    return false;
        //}

        //if (tNear == t1) normal = new Coordinates(-1, 0, 0);
        //else if (tNear == t2) normal = new Coordinates(1, 0, 0);
        //else if (tNear == t3) normal = new Coordinates(0, -1, 0);
        //else if (tNear == t4) normal = new Coordinates(0, 1, 0);
        //else if (tNear == t5) normal = new Coordinates(0, 0, -1);
        //else normal = new Coordinates(0, 0, 1);

        //return true;
    }

    public bool Intersects(Box other)
    {
        if (Max.X < other.Min.X || Min.X > other.Max.X)
            return false;
        if (Max.Y < other.Min.Y || Min.Y > other.Max.Y)
            return false;
        if (Max.Z < other.Min.Z || Min.Z > other.Max.Z)
            return false;

        return true;
    }
}
