namespace SDL;

interface IMarshallable<TResult>
    where TResult : unmanaged
{
    TResult Marshal(ref MarshalAllocator allocator);

    static TResult Marshal<TMarshallable>(TMarshallable marshallable, ref MarshalAllocator allocator)
        where TMarshallable : IMarshallable<TResult>
    {
        return marshallable.Marshal(ref allocator);
    }
}
