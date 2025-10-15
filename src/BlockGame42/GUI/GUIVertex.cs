namespace BlockGame42.GUI;

struct GUIVertex
{
    public Vector2 position;
    public Vector2 textureCoordinates;
    public uint color;

    public GUIVertex(Vector2 position, Vector2 textureCoordinates, uint color)
    {
        this.position = position;
        this.textureCoordinates = textureCoordinates;
        this.color = color;
    }
}