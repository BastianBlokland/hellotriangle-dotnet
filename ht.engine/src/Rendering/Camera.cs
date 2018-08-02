using HT.Engine.Math;

namespace HT.Engine.Rendering
{
    public sealed class Camera
    {
        internal const float NEAR_CLIP_DISTANCE = .1f;
        internal const float FAR_CLIP_DISTANCE = 1000f;

        //Data
        public Float4x4 Transformation { get; set; } = Float4x4.Identity;
        public float VerticalFov { get; set; } = 60f * FloatUtils.DEG_TO_RAD;

        internal Frustum GetFrustum(float aspect)
            => Frustum.CreateFromVerticalAngleAndAspect(
                VerticalFov,
                aspect,
                NEAR_CLIP_DISTANCE,
                FAR_CLIP_DISTANCE);

        internal Float4x4 GetProjection(float aspect)
            => Float4x4.CreatePerspectiveProjection(GetFrustum(aspect));
    }
}