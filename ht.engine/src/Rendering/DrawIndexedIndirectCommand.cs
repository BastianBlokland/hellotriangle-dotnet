using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.Engine.Rendering
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    internal readonly struct DrawIndexedIndirectCommand : IEquatable<DrawIndexedIndirectCommand>
    {
        public const int SIZE = sizeof(uint) * 4 + sizeof(int);
        
        //Data
        internal readonly uint IndexCount;
        internal readonly uint InstanceCount;
        internal readonly uint FirstIndex;
        internal readonly int VertexOffset;
        internal readonly uint FirstInstance;

        public DrawIndexedIndirectCommand(
            uint indexCount,
            uint instanceCount,
            uint firstIndex,
            int vertexOffset,
            uint firstInstance)
        {
            IndexCount = indexCount;
            InstanceCount = instanceCount;
            FirstIndex = firstIndex;
            VertexOffset = vertexOffset;
            FirstInstance = firstInstance;
        }

        //Equality
        public static bool operator ==(DrawIndexedIndirectCommand a, DrawIndexedIndirectCommand b)
            => a.Equals(b);

        public static bool operator !=(DrawIndexedIndirectCommand a, DrawIndexedIndirectCommand b)
            => !a.Equals(b);

        public override bool Equals(object obj) 
            => obj is DrawIndexedIndirectCommand && Equals((DrawIndexedIndirectCommand)obj);

        public bool Equals(DrawIndexedIndirectCommand other) => 
            other.IndexCount == IndexCount &&
            other.InstanceCount == InstanceCount &&
            other.FirstIndex == FirstIndex &&
            other.VertexOffset == VertexOffset &&
            other.FirstInstance == FirstInstance;

        public override int GetHashCode() => 
            IndexCount.GetHashCode() ^
            InstanceCount.GetHashCode() ^
            FirstIndex.GetHashCode() ^
            VertexOffset.GetHashCode() ^
            FirstInstance.GetHashCode();

        public override string ToString() => 
$@"(
    IndexCount: {IndexCount},
    InstanceCount: {InstanceCount},
    FirstIndex: {FirstIndex},
    VertexOffset: {VertexOffset},
    FirstInstance: {FirstInstance}
)";
    }
}