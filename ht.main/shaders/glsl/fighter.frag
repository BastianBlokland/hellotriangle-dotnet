#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_fragutils.glsl"

//Textures
layout(binding = 2) uniform sampler2D colorSampler;
layout(binding = 3) uniform sampler2D normalSampler;

//Input
layout(location = 0) in vec2 inUv;
layout(location = 1) in float inExhaustIntensity;
layout(location = 2) in float inExhaustMask;
layout(location = 3) in vec3 inWorldPosition;
layout(location = 4) in vec3 inWorldNormal;

//Output
layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outNormal;

void main() 
{
    #define hotExhaustColor vec4(0.9, 0.9, 2.0, 1.0)
    #define coldExhaustColor vec4(0.2, 0.2, 2.0, 1.0)

    outColor = 
        texture(colorSampler, inUv) + 
        mix(coldExhaustColor, hotExhaustColor, inExhaustIntensity) * inExhaustMask;

    outNormal.xyz = applyNormalTex(normalSampler, inWorldNormal, inWorldPosition, inUv);
    outNormal.a = inExhaustMask; //Store emissiveness
}