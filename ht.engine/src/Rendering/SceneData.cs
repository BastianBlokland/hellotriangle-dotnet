using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.Engine.Rendering
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    internal readonly struct SceneData : IEquatable<SceneData>
    {
        public const int SIZE = Float4x4.SIZE * 2;

        //Data
        public readonly Float4x4 ViewMatrix;
        public readonly Float4x4 ProjectionMatrix;
        
        internal SceneData(Float4x4 viewMatrix, Float4x4 projectionMatrix)
        {
            ViewMatrix = viewMatrix;
            ProjectionMatrix = projectionMatrix;
        }

        //Tuple deconstruction
        public void Deconstruct(out Float4x4 viewMatrix, out Float4x4 projectionMatrix)
        {
            viewMatrix = ViewMatrix;
            projectionMatrix = ProjectionMatrix;
        }

        //Equality
        public static bool operator ==(SceneData a, SceneData b) => a.Equals(b);

        public static bool operator !=(SceneData a, SceneData b) => !a.Equals(b);

        public override bool Equals(object obj) 
            => obj is SceneData && Equals((SceneData)obj);

        public bool Equals(SceneData other) => 
            other.ViewMatrix == ViewMatrix &&
            other.ProjectionMatrix == ProjectionMatrix;

        public override int GetHashCode() => 
            ViewMatrix.GetHashCode() ^ ProjectionMatrix.GetHashCode();

        public override string ToString()
            => $"(ViewMatrix:\n{ViewMatrix}\nProjectionMatrix:\n{ProjectionMatrix})";
    }
}