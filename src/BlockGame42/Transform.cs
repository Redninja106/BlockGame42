using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42;
internal struct Transform
{
    public Vector3 Position = Vector3.Zero;
    public Quaternion Rotation = Quaternion.Identity;

    public readonly Vector3 Forward => Vector3.Transform(Vector3.UnitZ, Rotation);

    public Transform()
    {
    }

    public Transform Translated(Vector3 translation)
    {
        return this with
        {
            Position = this.Position + translation,
        };
    }

    public readonly Matrix4x4 Matrix()
    {
        return Matrix4x4.CreateTranslation(Position) * Matrix4x4.CreateFromQuaternion(Rotation);
    }

    public static Transform Lerp(Transform a, Transform b, float t)
    {
        return new()
        {
            Position = Vector3.Lerp(a.Position, b.Position, t),
            Rotation = Quaternion.Slerp(a.Rotation, b.Rotation, t)
        };

    }
}
