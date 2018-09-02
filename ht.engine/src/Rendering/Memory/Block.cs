using System;

namespace HT.Engine.Rendering.Memory
{
    internal readonly struct Block
    {
        //Properties
        internal long EndOffset => Offset + Size;

        //Data
        internal readonly Chunk Container;
        internal readonly long Offset;
        internal readonly long Size;

        internal Block(Chunk container, long offset, long size)
        {
            Container = container;
            Offset = offset;
            Size = size;
        }
        
        internal IntPtr Map(long offset = 0) => Container.Map(this, offset);
        
        internal void Flush() => Container.Flush(this);

        internal void Unmap() => Container.Unmap();

        internal void Free() => Container.Free(this);
    }
}