using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.Engine.Rendering
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    internal readonly struct SceneData : IEquatable<SceneData>
    {
        public const int SIZE = Float4x4.SIZE;

        //Data
        public readonly Float4x4 ViewProjectionMatrix;
        
        internal SceneData(Float4x4 viewProjectionMatrix)
            => ViewProjectionMatrix = viewProjectionMatrix;

        //Tuple deconstruction
        public void Deconstruct(out Float4x4 viewProjectionMatrix)
            => viewProjectionMatrix = ViewProjectionMatrix;

        //Equality
        public static bool operator ==(SceneData a, SceneData b) => a.Equals(b);

        public static bool operator !=(SceneData a, SceneData b) => !a.Equals(b);

        public override bool Equals(object obj) 
            => obj is SceneData && Equals((SceneData)obj);

        public bool Equals(SceneData other) 
            => other.ViewProjectionMatrix == ViewProjectionMatrix;

        public override int GetHashCode() 
            => ViewProjectionMatrix.GetHashCode();

        public override string ToString()
            => $"(ViewProjectionMatrix:\n{ViewProjectionMatrix})";
    }
}