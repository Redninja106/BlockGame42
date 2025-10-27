namespace BlockGame42.GUI;

struct Extent
{
    public Vector2 Min;
    public Vector2 Max;

    public Extent(Vector2 a, Vector2 b)
    {
        Min = Vector2.Min(a, b);
        Max = Vector2.Max(a, b);
    }

    public static Extent Union(Extent a, Extent b)
    {
        return new Extent()
        {
            Min = Vector2.Min(a.Min, b.Min),
            Max = Vector2.Min(a.Max, b.Max),
        };
    }
}
