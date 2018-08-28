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
    const float inputGain = 1.5;

    vec4 sceneColor = texture(sceneColorSampler, inUv);
    //Bloom amount is stored in the alpha chnnel, 
    //multiply the color by bloom amount to get the bloom color
    outColor = vec4(sceneColor.rgb * sceneColor.a * inputGain, 1.0);
}