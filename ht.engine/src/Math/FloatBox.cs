using System;
using System.Runtime.InteropServices;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    public readonly struct FloatBox : IEquatable<FloatBox>
    {
        public const int SIZE = Float3.SIZE * 2;

        //Presets
        public static readonly FloatBox Zero = new FloatBox();
        public static readonly FloatBox NDC = new FloatBox(min: (-1f, -1f, 0f), max: (1f, 1f, 1f));

        //Properties
        public Float3 Size => new Float3(Max.X - Min.X, Max.Y - Min.Y, Max.Z - Min.Z);
        public Float3 HalfSize => Size * .5f;
        public Float3 Center => Min + HalfSize;

        //Data
        public readonly Float3 Min;
        public readonly Float3 Max;

        public FloatBox(Float3 min, Float3 max)
        {
            if (min.X > max.X || min.Y > max.Y)
                throw new ArgumentException($"[{nameof(FloatBox)}] Is inside out!");
            Min = min;
            Max = max;
        }

        //Tuple deconstruction syntax
        public void Deconstruct(out Float3 min, out Float3 max)
        {
            min = Min;
            max = Max;
        }

        //Utilities
        public bool Contains(Float3 point) =>
            point.X > Min.X && point.X < Max.X && 
            point.Y > Min.Y && point.Y < Max.Y && 
            point.Z > Min.Z && point.Z < Max.Z;

        public bool Intersect(FloatBox other) =>
            Min.X < other.Max.X && Max.X > other.Min.X &&
            Min.Y < other.Max.Y && Max.Y > other.Min.Y &&
            Min.Z < other.Max.Z && Max.Z > other.Min.Z;

        public FloatBox Transform(Float4x4 matrix)
        {
            //Get the 8 corners of this box
            Span<Float3> points = stackalloc Float3[8];
            GetPoints(points);

            //Transform the 8 corners
            //Note: the matrix can contains skewing (like with perspective projection) so we need to 
            //transform all corners
            for (int i = 0; i < points.Length; i++)
                points[i] = matrix.TransformPoint(points[i]);

            //Create a new box around the transformed points. Note: if the matrix contained
            //rotation or skewing then this will create a tight as possible bounds around the points.
            return CreateFromPoints(points);
        }

        public void GetPoints(Span<Float3> points)
        {
            if (points.Length != 8)
                throw new Exception($"[{nameof(FloatBox)}] Need 8 points to represent a box");
            Float3 center = Center;
            Float3 halfSize = HalfSize;
            //Top
            points[0] = center + (+halfSize.X, +halfSize.Y, +halfSize.Z);
            points[1] = center + (-halfSize.X, +halfSize.Y, +halfSize.Z);
            points[2] = center + (+halfSize.X, +halfSize.Y, -halfSize.Z);
            points[3] = center + (-halfSize.X, +halfSize.Y, -halfSize.Z);
            //Bottom
            points[4] = center + (+halfSize.X, -halfSize.Y, +halfSize.Z);
            points[5] = center + (-halfSize.X, -halfSize.Y, +halfSize.Z);
            points[6] = center + (+halfSize.X, -halfSize.Y, -halfSize.Z);
            points[7] = center + (-halfSize.X, -halfSize.Y, -halfSize.Z);
        }

        //Creation
        public static FloatBox CreateFromCenterAndExtents(Float3 origin, Float3 size)
        {
            Float3 halfSize = size * .5f;
            return new FloatBox(
                min: origin - halfSize,
                max: origin + halfSize);
        }

        public static FloatBox CreateFromPoints(in ReadOnlySpan<Float3> points)
        {
            Float3 min = Float3.Max;
            Float3 max = Float3.Min;
            for (int i = 0; i < points.Length; i++)
            {
                Float3 point = points[i];
                //Check for min
                if (point.X < min.X)
                    min = new Float3(point.X, min.Y, min.Z);
                if (point.Y < min.Y)
                    min = new Float3(min.X, point.Y, min.Z);
                if (point.Z < min.Z)
                    min = new Float3(min.X, min.Y, point.Z);
                //Check for max
                if (point.X > max.X)
                    max = new Float3(point.X, max.Y, max.Z);
                if (point.Y > max.Y)
                    max = new Float3(max.X, point.Y, max.Z);
                if (point.Z > max.Z)
                    max = new Float3(max.X, max.Y, point.Z);
            }
            return new FloatBox(min, max);
        }

        //Arithmetic operators
        public static bool operator ==(FloatBox a, FloatBox b) => a.Equals(b);

        public static bool operator !=(FloatBox a, FloatBox b) => !a.Equals(b);

        //Equality
        public override bool Equals(object obj) => obj is FloatBox && Equals((FloatBox)obj);

        public bool Equals(FloatBox other) => other.Min == Min && other.Max == Max;

        public override int GetHashCode() => Min.GetHashCode() ^ Max.GetHashCode();

        public override string ToString() => $"(Min: {Min}, Max: {Max})";
    }
}