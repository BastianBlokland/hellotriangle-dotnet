#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_fragutils.glsl"
#include "include_sceneinput.glsl"
#include "include_math.glsl"

//Uniforms
layout(binding = 0) uniform sampler2D sceneColorSampler;

//Input
layout(location = 0) in vec2 inUv;

//Output
layout(location = 0) out vec4 outColor;

void main()
{
    vec4 sceneColor = texture(sceneColorSampler, inUv);
    outColor = sceneColor;
}