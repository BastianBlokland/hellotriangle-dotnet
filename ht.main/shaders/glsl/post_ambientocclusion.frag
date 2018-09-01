#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_fragutils.glsl"
#include "include_sceneinput.glsl"
#include "include_math.glsl"

//Specialization
layout(constant_id = 0) const int sampleKernelSize = 32;

//Uniforms
layout(binding = 0) uniform sampler2D sceneDepthSampler;
layout(binding = 1) uniform sampler2D sceneNormalSampler;
layout(binding = 2) uniform SampleKernel
{
    vec4 points[sampleKernelSize];
} sampleKernel;
layout(binding = 3) uniform sampler2D rotationNoiseSampler;

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
    vec2 noiseScale = pushconstants.targetSize / vec2(textureSize(rotationNoiseSampler, 0));
    vec3 randRotation = texture(rotationNoiseSampler, inUv * noiseScale).xyz;
    float sceneDepth = texture(sceneDepthSampler, inUv).r;
    
    // float val = 0;
    // for (int x = 0; x < sampleKernelSize; x++)
    // {
    //     vec3 pos = sampleKernel.points[i].xyz;
    //     val += pos.x;
    // }


    //outColor = vec4(sceneDepth, sceneDepth, sceneDepth, 1.0);
    outColor = vec4(randRotation, 1.0);
}