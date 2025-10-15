namespace BlockGame42;

struct Ray
{
    public Vector3 Origin;
    public Vector3 Direction;
    public Vector3 InverseDirection;
    public float Length;

    public Ray(Vector3 origin, Vector3 direction, float length)
    {
        Origin = origin;
        Direction = direction;
        InverseDirection = Vector3.One / direction;
        Length = length;
    }

    public Vector3 At(float t)
    {
        return Origin + Direction * t;
    }
}