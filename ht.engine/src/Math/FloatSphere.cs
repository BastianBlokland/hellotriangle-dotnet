using System;
using System.Runtime.InteropServices;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    public readonly struct FloatSphere : IEquatable<FloatSphere>
    {
        public const int SIZE = Float3.SIZE + sizeof(float);

        //Presets
        public static readonly FloatSphere Zero = new FloatSphere(center: Float3.Zero, radius: 0f);
        public static readonly FloatSphere One = new FloatSphere(center: Float3.Zero, radius: 1f);

        //Properties
        public float HalfRadius => Radius * .5f;
        public float SquareRadius => Radius * Radius;

        //Data
        public readonly Float3 Center;
        public readonly float Radius;

        public FloatSphere(Float3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        //Tuple deconstruction syntax
        public void Deconstruct(out Float3 center, out float radius)
        {
            center = Center;
            radius = Radius;
        }

        //Utilities
        public bool Contains(Float3 point) => (point - Center).SquareMagnitude <= SquareRadius;

        public FloatBox GetBounds() => FloatBox.CreateFromCenterAndExtents(Center, (Radius * 2).XXX());

        //Creation
        public static FloatSphere CreateFromPoints(in ReadOnlySpan<Float3> points)
        {
            //Based on Ritter's Bounding sphere alogorithm
            //https://en.wikipedia.org/wiki/Bounding_sphere#Ritter's_bounding_sphere
            
            if (points.Length == 0)
                throw new ArgumentException(
                    $"[{nameof(FloatSphere)}] Requires at least one point", nameof(points));

            FloatBox boundingBox = FloatBox.CreateFromPoints(points);
            Float3 center = boundingBox.Center;
            Float3 halfSize = boundingBox.HalfSize;
            
            float radius = FloatUtils.Max(halfSize.X, halfSize.Y, halfSize.Z);
            float squareRadius = radius * radius;

            for (int i = 0; i < points.Length; i++)
            {
                Float3 toPoint = points[i] - center;
                float squareDistance = toPoint.SquareMagnitude;
                //If points falls outside of the sphere then update the sphere to contain the point
                if (squareDistance > squareRadius)
                {
                    float distance = FloatUtils.SquareRoot(squareDistance);
                    //Update radius
                    radius = (radius + distance) * .5f;
                    squareRadius = radius * radius;
                    //Update center
                    float offset = distance - radius;
                    center = (radius * center + offset * points[i]) / distance;
                }
            }
            return new FloatSphere(center, radius);
        }

        //Arithmetic operators
        public static bool operator ==(FloatSphere a, FloatSphere b) => a.Equals(b);

        public static bool operator !=(FloatSphere a, FloatSphere b) => !a.Equals(b);

        //Equality
        public override bool Equals(object obj) => obj is FloatSphere && Equals((FloatSphere)obj);

        public bool Equals(FloatSphere other) => other.Center == Center && other.Radius == Radius;

        public override int GetHashCode() => Center.GetHashCode() ^ Radius.GetHashCode();

        public override string ToString() => $"(Center: {Center}, Radius: {Radius})";
    }
}