#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_fragutils.glsl"
#include "include_sceneinput.glsl"
#include "include_math.glsl"

//Specialization
layout(constant_id = 0) const int sampleKernelSize = 32;
layout(constant_id = 1) const float sampleRadius = 0.5;
layout(constant_id = 2) const float sampleBias = -0.001;
layout(constant_id = 3) const float occlusionMultiplier = 1.0;

//Uniforms
layout(binding = 0) uniform CameraData cameraData;
layout(binding = 1) uniform sampler2D sceneDepthSampler;
layout(binding = 2) uniform sampler2D sceneNormalSampler;
layout(binding = 3) uniform SampleKernel
{
    vec4 points[sampleKernelSize];
} sampleKernel;
layout(binding = 4) uniform sampler2D rotationNoiseSampler;

//Push constants
layout(push_constant) uniform PushConstants
{
    ivec2 targetSize;
} pushconstants;

//Input
layout(location = 0) in vec2 inUv;

//Output
layout(location = 0) out vec4 outColor;

void main()
{
    //Get world position and normal from the gbuffer pass
    float depth = texture(sceneDepthSampler, inUv).r;
    float linearDepth = linearizeDepth(depth, cameraData.nearClipDistance, cameraData.farClipDistance);
    vec3 worldPos = worldPosFromDepth(depth, inUv, cameraData.inverseViewProjectionMatrix);
    vec3 worldNormal = texture(sceneNormalSampler, inUv).xyz;

    //Calculate a matrix that aligns the sample hemisphere with the normal of the surface
    //Z-axis will point in the normal of the surface and x / y axis will be rotated by a random amount
    //around the z-axis. This random rotation will effectivly increase our kernel sample size as we
    //sample more 'different' points. To hide the artficates that the random causes we blur the
    //different samples together at the end.
    vec2 noiseScale = pushconstants.targetSize / vec2(textureSize(rotationNoiseSampler, 0));
    vec3 randRotation = texture(rotationNoiseSampler, inUv * noiseScale).xyz;
    vec3 tangent = normalize(randRotation - worldNormal * dot(randRotation, worldNormal));
    vec3 bitangent = cross(worldNormal, tangent);
    mat3 kernelRotation = mat3(tangent, bitangent, worldNormal);

    float occlusion = 0;
    for (int i = 0; i < sampleKernelSize; i++)
    {
        //Calculate world-space position for the kernel point
        vec3 sampleDir = kernelRotation * sampleKernel.points[i].xyz;
        vec3 samplePos = worldPos + sampleDir * sampleRadius;

        //Caculate clipspace position for that point
        vec4 sampleClipPos = cameraData.viewProjectionMatrix * vec4(samplePos, 1.0);
        sampleClipPos.xyz /= sampleClipPos.w; //Perspective divide
        vec2 sampleCoord = sampleClipPos.xy * 0.5 + 0.5; //To texture space
        //Calculate the target linear depth of the sample
        float linearTargetDepth = linearizeDepth(sampleClipPos.z, 
           cameraData.nearClipDistance, cameraData.farClipDistance) + sampleBias;

        //Sample the depth-texture at that coord
        float actualSampleDepth = texture(sceneDepthSampler, sampleCoord).r;
        float actualSampleLinearDepth = linearizeDepth(actualSampleDepth, 
           cameraData.nearClipDistance, cameraData.farClipDistance);

        //Fade out the effect based on how far away from the origin the sample is
        float radiusFade = smoothstep(0.0, 1.0, sampleRadius / abs(linearDepth - actualSampleLinearDepth));
        //If the actual depth is less then the target depth then this sample was occluded
        occlusion += float(actualSampleLinearDepth <= linearTargetDepth) * radiusFade;
    }

    outColor = vec4(1.0 - occlusion / sampleKernelSize * occlusionMultiplier);
}