namespace SDL.GPU;

public enum DataBufferUsageFlags : uint
{
    Vertex = (1u << 0),
    Index = (1u << 1),
    Indirect = (1u << 2),
    GraphicsStorageRead = (1u << 3),
    ComputeStorageRead = (1u << 4),
    ComputeStorageWrite = (1u << 5),
}
