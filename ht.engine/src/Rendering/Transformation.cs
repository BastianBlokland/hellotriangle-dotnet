using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using VulkanCore;

namespace HT.Engine.Rendering
{
    [StructLayout(LayoutKind.Sequential, Size = SIZE)]
    internal struct Transformation : IEquatable<Transformation>
    {
        public const int SIZE = Float4x4.SIZE * 3;

        //Data
        public readonly Float4x4 Model;
        public readonly Float4x4 View;
        public readonly Float4x4 Projection;

        public Transformation(Float4x4 model, Float4x4 view, Float4x4 projection)
        {
            Model = model;
            View = view;
            Projection = projection;
        }

        //Tuple deconstruction
        public void Deconstruct(out Float4x4 model, out Float4x4 view, out Float4x4 projection)
        {
            model = Model;
            view = View;
            projection = Projection;
        }

        //Equality
        public static bool operator ==(Transformation a, Transformation b) => a.Equals(b);

        public static bool operator !=(Transformation a, Transformation b) => !a.Equals(b);

        public override bool Equals(object obj) 
            => obj is Transformation && Equals((Transformation)obj);

        public bool Equals(Transformation other) 
            => other.Model == Model && other.View == View && other.Projection == Projection;

        public override int GetHashCode() 
            => Model.GetHashCode() ^ View.GetHashCode() ^ Projection.GetHashCode();

        public override string ToString()
            => $"(Model:\n{Model}, View:\n{View}, Projection:\n{Projection})";
    }
}