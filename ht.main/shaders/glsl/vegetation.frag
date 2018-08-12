#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_math.glsl"
#include "include_fragutils.glsl"

//Textures
layout(binding = 1) uniform sampler2D colorSampler;
layout(binding = 2) uniform sampler2D normalSampler;
layout(binding = 3) uniform sampler2D terrainSampler;

//Input
layout(location = 0) in vec2 inColorUv;
layout(location = 1) in vec4 inColorTint;
layout(location = 2) in vec3 inWorldPosition;
layout(location = 3) in vec3 inWorldNormal;

//Output
layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outNormal;


void main()
{
    outColor = texture(colorSampler, inColorUv) * inColorTint;
    if (outColor.a < 0.01)
    {
        discard;
    }
    outNormal.xyz = applyNormalTex(normalSampler, inWorldNormal, inWorldPosition, inColorUv);
}