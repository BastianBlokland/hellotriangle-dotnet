namespace HT.Engine.Rendering.Memory
{
    internal readonly struct Block
    {
        internal readonly Chunk Container;
        internal readonly long Offset;
        internal readonly long Size;

        internal Block(Chunk container, long offset, long size)
        {
            Container = container;
            Offset = offset;
            Size = size;
        }

        internal void Free() => Container.Free(this);
    }
}