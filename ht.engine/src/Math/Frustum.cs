using System;
using System.Runtime.InteropServices;

using static System.Math;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential, Size = SIZE)]
    public struct Frustum : IEquatable<Frustum>
    {
        public const int SIZE = sizeof(float) * 4;

        //Data
        public readonly float VerticalAngle;
        public readonly float HorizontalAngle;
        public readonly float NearDistance;
        public readonly float FarDistance;

        public Frustum(
            float verticalAngle,
            float horizontalAngle,
            float nearDistance,
            float farDistance)
        {
            //Sanity check the input
            if (verticalAngle <= 0f || verticalAngle >= System.Math.PI)
                throw new ArgumentOutOfRangeException(nameof(verticalAngle));
            if (horizontalAngle <= 0f || horizontalAngle >= System.Math.PI)
                throw new ArgumentOutOfRangeException(nameof(verticalAngle));
            if (nearDistance <= 0f || nearDistance >= farDistance)
                throw new ArgumentOutOfRangeException(nameof(nearDistance));
            if (farDistance <= 0f)
                throw new ArgumentOutOfRangeException(nameof(farDistance));
            VerticalAngle = verticalAngle;
            HorizontalAngle = horizontalAngle;
            NearDistance = nearDistance;
            FarDistance = farDistance;
        }

        //Creation
        public static Frustum CreateFromVerticalAngleAndAspect(
            float verticalAngle,
            float aspect,
            float nearDistance,
            float farDistance)
        {
            float horizontalAngle = (float)Atan(Tan(verticalAngle) * aspect);
            return new Frustum(
                verticalAngle: verticalAngle,
                horizontalAngle: horizontalAngle,
                nearDistance: nearDistance,
                farDistance: farDistance);
        }

        public static Frustum CreateFromHorizontalAngleAndAspect(
            float horizontalAngle,
            float aspect,
            float nearDistance,
            float farDistance)
        {
            float verticalAngle = (float)Atan(Tan(horizontalAngle) / aspect);
            return new Frustum(
                verticalAngle: verticalAngle,
                horizontalAngle: horizontalAngle,
                nearDistance: nearDistance,
                farDistance: farDistance);
        }

        //Equality
        public static bool operator ==(Frustum a, Frustum b) => a.Equals(b);

        public static bool operator !=(Frustum a, Frustum b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Frustum && Equals((Frustum)obj);

        public bool Equals(Frustum other) => 
            other.VerticalAngle == VerticalAngle &&
            other.HorizontalAngle == HorizontalAngle &&
            other.NearDistance == NearDistance &&
            other.FarDistance == FarDistance;

        public override int GetHashCode() => 
            VerticalAngle.GetHashCode() ^
            HorizontalAngle.GetHashCode() ^
            NearDistance.GetHashCode() ^
            FarDistance.GetHashCode();

        public override string ToString() => 
$@"(
    VerticalAngle: {VerticalAngle},
    HorizontalAngle: {HorizontalAngle},
    NearDistance: {NearDistance},
    FarDistance: {FarDistance}
)";
    }
}