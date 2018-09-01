#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_fragutils.glsl"
#include "include_sceneinput.glsl"
#include "include_math.glsl"

//Uniforms
layout(binding = 0) uniform sampler2D sceneDepthSampler;
layout(binding = 1) uniform sampler2D sceneNormalSampler;
layout(binding = 2) uniform sampler2D rotationNoiseSampler;

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
    vec2 noiseScale = pushconstants.targetSize / textureSize(rotationNoiseSampler, 0);
    vec3 randRotation = texture(rotationNoiseSampler, inUv * noiseScale).xyz;
    float sceneDepth = texture(sceneDepthSampler, inUv).r;
    
    //outColor = vec4(sceneDepth, sceneDepth, sceneDepth, 1.0);
    outColor = vec4(randRotation, 1.0);
}