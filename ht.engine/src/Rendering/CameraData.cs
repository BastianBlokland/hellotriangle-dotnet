using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.Engine.Rendering
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    internal readonly struct CameraData : IEquatable<CameraData>
    {
        public const int SIZE = 
            Float4x4.SIZE * 4 +
            sizeof(float) * 2;
        
        //Data
        public readonly Float4x4 CameraMatrix; //Inverse of view matrix
        public readonly Float4x4 ViewMatrix; //Inverse of camera matrix
        public readonly Float4x4 ProjectionMatrix;
        public readonly Float4x4 ViewProjectionMatrix; //Projection * View
        public readonly float NearClipDistance;
        public readonly float FarClipDistance;
        
        internal CameraData(
            Float4x4 cameraMatrix,
            Float4x4 projectionMatrix,
            float nearClipDistance,
            float farClipDistance)
        {
            Float4x4 viewMatrix = cameraMatrix.Invert();
            CameraMatrix = cameraMatrix;
            ViewMatrix = viewMatrix;
            ProjectionMatrix = projectionMatrix;
            ViewProjectionMatrix = projectionMatrix * viewMatrix;
            NearClipDistance = nearClipDistance;
            FarClipDistance = farClipDistance;
        }

        //Equality
        public static bool operator ==(CameraData a, CameraData b) => a.Equals(b);

        public static bool operator !=(CameraData a, CameraData b) => !a.Equals(b);

        public override bool Equals(object obj) 
            => obj is CameraData && Equals((CameraData)obj);

        public bool Equals(CameraData other) => 
            other.CameraMatrix == CameraMatrix &&
            other.ViewMatrix == ViewMatrix &&
            other.ProjectionMatrix == ProjectionMatrix &&
            other.NearClipDistance == NearClipDistance &&
            other.FarClipDistance == FarClipDistance;

        public override int GetHashCode() =>
            CameraMatrix.GetHashCode() ^
            ViewMatrix.GetHashCode() ^ 
            ProjectionMatrix.GetHashCode() ^
            NearClipDistance.GetHashCode() ^
            FarClipDistance.GetHashCode();

        public override string ToString() => 
$@"(
    CameraMatrix:
    {CameraMatrix},
    ViewMatrix:
    {ViewMatrix},
    ProjectionMatrix:
    {ProjectionMatrix},
    NearPlaneDistance: {NearClipDistance},
    FarPlaneDistance: {FarClipDistance}
)";
    }
}