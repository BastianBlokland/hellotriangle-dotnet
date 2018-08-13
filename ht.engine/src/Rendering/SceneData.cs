using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.Engine.Rendering
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    internal readonly struct SceneData : IEquatable<SceneData>
    {
        public const int SIZE = 
            sizeof(int) +
            sizeof(float) * 2;
        
        //Data
        public readonly int Frame;
        public readonly float Time;
        public readonly float DeltaTime;
        
        internal SceneData(
            int frame,
            float time,
            float deltaTime)
        {
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
            other.Frame == Frame &&
            other.Time == Time &&
            other.DeltaTime == DeltaTime;

        public override int GetHashCode() =>
            Frame.GetHashCode() ^
            Time.GetHashCode() ^
            DeltaTime.GetHashCode();

        public override string ToString() => 
$@"(
    Frame: {Frame},
    Time: {Time},
    DeltaTime: {DeltaTime}
)";
    }
}