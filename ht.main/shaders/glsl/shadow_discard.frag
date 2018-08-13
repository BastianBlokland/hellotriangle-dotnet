#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_fragutils.glsl"

//Textures
layout(binding = 2) uniform sampler2D texSampler;

//Input
layout(location = 0) in vec2 inUv;

void main()
{
    if (texture(texSampler, inUv).a < discardAlpha)
    {
        discard;
    }
}