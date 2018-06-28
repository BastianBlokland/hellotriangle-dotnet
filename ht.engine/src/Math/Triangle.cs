using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    public struct Triangle
    {
        public const int SIZE = Float3.SIZE * 3;

        //Properties
        public Float3 Normal => GetNormal(A, B, C);

        //Data
        public readonly Float3 A;
        public readonly Float3 B;
        public readonly Float3 C;

        public Triangle(Float3 a, Float3 b, Float3 c)
        {
            A = a;
            B = b;
            C = c;
        }

        //Tuple deconstruction syntax
        public void Deconstruct(out Float3 a, out Float3 b, out Float3 c)
        {
            a = A;
            b = B;
            c = B;
        }

        public static Float3 GetNormal(Float3 a, Float3 b, Float3 c)
        {
            //Take cross product of two edges in this triangle
            var normal = Float3.Cross(b - a, c - a);
            return Float3.FastNormalize(normal);
        }

        //Equality
        public static bool operator ==(Triangle a, Triangle b) => a.Equals(b);

        public static bool operator !=(Triangle a, Triangle b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Triangle && Equals((Triangle)obj);

        public bool Equals(Triangle other) => 
            other.A == A &&
            other.B == B &&
            other.C == C;

        public override int GetHashCode() => 
            A.GetHashCode() ^
            B.GetHashCode() ^
            C.GetHashCode();

        public bool Approx(Triangle other, float maxDifference = .0001f) =>
            A.Approx(other.A, maxDifference) && 
            B.Approx(other.B, maxDifference) && 
            C.Approx(other.C, maxDifference);

        public override string ToString() =>
$@"
(
    A: {A},
    B: {B},
    C: {C}
)";
    }
}