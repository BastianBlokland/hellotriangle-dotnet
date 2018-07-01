using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

using static System.Math;
using static System.MathF;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    public readonly struct Float4x4 : IEquatable<Float4x4>
    {
        public const int SIZE = Float4.SIZE * 4;

        //Presets
        public static readonly Float4x4 Identity = Float4x4.CreateFromRows(
            row0: (1f, 0f, 0f, 0f),
            row1: (0f, 1f, 0f, 0f),
            row2: (0f, 0f, 1f, 0f),
            row3: (0f, 0f, 0f, 1f));

        //Properties
        public Float3 Translation => Column3.XYZ;

        //Column index accessor
        public Float4 this[int i]
        {
            get 
            {
                switch (i)
                {
                    case 0: return Column0;
                    case 1: return Column1;
                    case 2: return Column2;
                    case 3: return Column3;
                }
                throw new ArgumentOutOfRangeException(
                    $"[{nameof(Float4x4)}] No column at: {i}", nameof(i));
            }
        }

        //Alternative representation
        public Float4 Row0 => new Float4(Column0.X, Column1.X, Column2.X, Column3.X);
        public Float4 Row1 => new Float4(Column0.Y, Column1.Y, Column2.Y, Column3.Y);
        public Float4 Row2 => new Float4(Column0.Z, Column1.Z, Column2.Z, Column3.Z);
        public Float4 Row3 => new Float4(Column0.W, Column1.W, Column2.W, Column3.W);

        //Data
        public readonly Float4 Column0;
        public readonly Float4 Column1;
        public readonly Float4 Column2;
        public readonly Float4 Column3;

        public Float4x4(Float4 column0, Float4 column1, Float4 column2, Float4 column3)
        {
            Column0 = column0;
            Column1 = column1;
            Column2 = column2;
            Column3 = column3;
        }

        //Tranformations
        public Float3 TransformVector(Float3 vector)
            => new Float3(
                x: vector.X * Column0.X + vector.Y * Column1.X + vector.Z * Column2.X,
                y: vector.X * Column0.Y + vector.Y * Column1.Y + vector.Z * Column2.Y,
                z: vector.X * Column0.Z + vector.Y * Column1.Z + vector.Z * Column2.Z);

        public Float3 TransformPoint(Float3 point)
            => TransformVector(point) + Translation;

        //Creation
        public static Float4x4 CreateFromRows(Float4 row0, Float4 row1, Float4 row2, Float4 row3)
            => new Float4x4(
                column0: (row0.X, row1.X, row2.X, row3.X),
                column1: (row0.Y, row1.Y, row2.Y, row3.Y),
                column2: (row0.Z, row1.Z, row2.Z, row3.Z),
                column3: (row0.W, row1.W, row2.W, row3.W));

        public static Float4x4 CreateTranslation(Float3 translation) => CreateFromRows(
            row0: (1f, 0f, 0f, translation.X),
            row1: (0f, 1f, 0f, translation.Y),
            row2: (0f, 0f, 1f, translation.Z),
            row3: (0f, 0f, 0f, 1f));

        public static Float4x4 CreateScale(float scale) => CreateFromRows(
            row0: (scale, 0f,    0f,    0f),
            row1: (0f,    scale, 0f,    0f),
            row2: (0f,    0f,    scale, 0f),
            row3: (0f,    0f,    0f,    1f));

        public static Float4x4 CreateScale(Float3 scale) => CreateFromRows(
            row0: (scale.X, 0f,      0f,      0f),
            row1: (0f,      scale.Y, 0f,      0f),
            row2: (0f,      0f,      scale.Z, 0f),
            row3: (0f,      0f,      0f,      1f));

        public static Float4x4 CreateScale(Float3 scale, Float3 centerPoint) => CreateFromRows(
            //Note: This also does translation to match the centerPoint
            row0: (scale.X, 0f,      0f,      centerPoint.X * (1 - scale.X)),
            row1: (0f,      scale.Y, 0f,      centerPoint.Y * (1 - scale.Y)),
            row2: (0f,      0f,      scale.Z, centerPoint.Z * (1 - scale.Z)),
            row3: (0f,      0f,      0f,      1f));

        public static Float4x4 CreateRotationFromAxis(Float3 forward, Float3 up)
        {
            Float3 zAxis = Float3.FastNormalize(-forward);
            Float3 xAxis = Float3.FastNormalize(Float3.Cross(up, zAxis));
            Float3 yAxis = Float3.Cross(zAxis, xAxis);
            return CreateFromRows(
                row0: (xAxis.X, yAxis.X, zAxis.X, 0f),
                row1: (xAxis.Y, yAxis.Y, zAxis.Y, 0f),
                row2: (xAxis.Z, yAxis.Z, zAxis.Z, 0f),
                row3: (0f,      0f,      0f,      1f));
        }

        public static Float4x4 CreateRotationFromXAngle(float xAngle)
        {
            float cos = Cos(xAngle);
            float sin = Sin(xAngle);
            return CreateFromRows(
                row0: (1f, 0f,  0f,   0f),
                row1: (0f, cos, -sin, 0f),
                row2: (0f, sin, cos,  0f),
                row3: (0f, 0f,  0f,  1f));
        }

        public static Float4x4 CreateRotationFromYAngle(float yAngle)
        {
            float cos = Cos(yAngle);
            float sin = Sin(yAngle);
            return CreateFromRows(
                row0: (cos,  0f, sin, 0f),
                row1: (0f,   1f, 0f,  0f),
                row2: (-sin, 0f, cos, 0f),
                row3: (0f,   0f, 0f,  1f));
        }

        public static Float4x4 CreateRotationFromZAngle(float zAngle)
        {
            float cos = Cos(zAngle);
            float sin = Sin(zAngle);
            return CreateFromRows(
                row0: (cos, -sin, 0f, 0f),
                row1: (sin, cos,  0f,  0f),
                row2: (0f,  0f,   1f, 0f),
                row3: (0f,  0f,   0f,  1f));
        }

        /// <summary>
        /// Matrix to transform from view-space into clip-space
        /// Clip space:
        /// TopLeft: -1,-1
        /// BottomRight: 1,1
        /// MinDepth: 0,
        /// MaxDepth: 1
        /// </summary>
        public static Float4x4 CreatePerspectiveProjection(Frustum frustum)
        {
            float yScale = 1f / Tan(frustum.VerticalAngle * .5f);
            float xScale = 1f / Tan(frustum.HorizontalAngle * .5f);;
            float far = frustum.FarDistance;
            float near = frustum.NearDistance;
            return Float4x4.CreateFromRows(
                row0: (xScale,  0f,      0f,                 0f),
                row1: (0f,      -yScale, 0f,                 0f),
                row2: (0f,      0f,      far / (near - far), (near * far) / (near - far)),
                row3: (0f,      0f,      -1f,                0f));
        }

        //Arithmetic operators
        public static Float4x4 operator *(Float4x4 left, Float4x4 right)
        {
            Float4 row0 =   left.Row0.X * right.Row0 +
                            left.Row0.Y * right.Row1 +
                            left.Row0.Z * right.Row2 +
                            left.Row0.W * right.Row3;
            
            Float4 row1 =   left.Row1.X * right.Row0 +
                            left.Row1.Y * right.Row1 +
                            left.Row1.Z * right.Row2 +
                            left.Row1.W * right.Row3;
            
            Float4 row2 =   left.Row2.X * right.Row0 +
                            left.Row2.Y * right.Row1 +
                            left.Row2.Z * right.Row2 +
                            left.Row2.W * right.Row3;
            
            Float4 row3 =   left.Row3.X * right.Row0 +
                            left.Row3.Y * right.Row1 +
                            left.Row3.Z * right.Row2 +
                            left.Row3.W * right.Row3;
            
            return CreateFromRows(row0, row1, row2, row3);
        }

        //Equality
        public static bool operator ==(Float4x4 a, Float4x4 b) => a.Equals(b);

        public static bool operator !=(Float4x4 a, Float4x4 b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Float4x4 && Equals((Float4x4)obj);

        public bool Equals(Float4x4 other) => 
            other.Column0 == Column0 &&
            other.Column1 == Column1 &&
            other.Column2 == Column2 &&
            other.Column3 == Column3;

        public override int GetHashCode() => 
            Column0.GetHashCode() ^
            Column1.GetHashCode() ^
            Column2.GetHashCode() ^
            Column3.GetHashCode();

        public bool Approx(Float4x4 other, float maxDifference = .0001f) =>
            Column0.Approx(other.Column0, maxDifference) && 
            Column1.Approx(other.Column1, maxDifference) && 
            Column2.Approx(other.Column2, maxDifference) &&
            Column3.Approx(other.Column3, maxDifference);

        public override string ToString() => 
$@"(
    Row0: {Row0},
    Row1: {Row1},
    Row2: {Row2},
    Row3: {Row3}
)";
    }
}