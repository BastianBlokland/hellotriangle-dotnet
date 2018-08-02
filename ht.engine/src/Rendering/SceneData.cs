using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.Engine.Rendering
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    internal readonly struct SceneData : IEquatable<SceneData>
    {
        public const int SIZE = 
            Float4x4.SIZE * 4 + 
            sizeof(int) +
            sizeof(float) * 2;
        
        //Data
        public readonly Float4x4 CameraMatrix; //Inverse of view matrix
        public readonly Float4x4 ViewMatrix; //Inverse of camera matrix
        public readonly Float4x4 ProjectionMatrix;
        public readonly Float4x4 ViewProjectionMatrix; //Projection * View
        public readonly int Frame;
        public readonly float Time;
        public readonly float DeltaTime;
        
        internal SceneData(
            Float4x4 cameraMatrix,
            Float4x4 projectionMatrix,
            int frame,
            float time,
            float deltaTime)
        {
            Float4x4 viewMatrix = cameraMatrix.Invert();
            CameraMatrix = cameraMatrix;
            ViewMatrix = viewMatrix;
            ProjectionMatrix = projectionMatrix;
            ViewProjectionMatrix = projectionMatrix * viewMatrix;
            Frame = frame;
            Time = time;
            DeltaTime = deltaTime;
        }

        //Equality
        public static bool operator ==(SceneData a, SceneData b) => a.Equals(b);

        public static bool operator !=(SceneData a, SceneData b) => !a.Equals(b);

        public override bool Equals(object obj) 
            => obj is SceneData && Equals((SceneData)obj);

        public bool Equals(SceneData other) => 
            other.CameraMatrix == CameraMatrix &&
            other.ViewMatrix == ViewMatrix &&
            other.ProjectionMatrix == ProjectionMatrix &&
            other.Frame == Frame &&
            other.Time == Time &&
            other.DeltaTime == DeltaTime;

        public override int GetHashCode() =>
            CameraMatrix.GetHashCode() ^
            ViewMatrix.GetHashCode() ^ 
            ProjectionMatrix.GetHashCode() ^
            Frame.GetHashCode() ^
            Time.GetHashCode() ^
            DeltaTime.GetHashCode();

        public override string ToString() => 
$@"(
    CameraMatrix:
    {CameraMatrix},
    ViewMatrix:
    {ViewMatrix},
    ProjectionMatrix:
    {ProjectionMatrix},
    Frame: {Frame},
    Time: {Time},
    DeltaTime: {DeltaTime}
)";
    }
}