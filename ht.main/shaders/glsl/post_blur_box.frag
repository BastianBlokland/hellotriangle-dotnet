#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_fragutils.glsl"
#include "include_sceneinput.glsl"
#include "include_math.glsl"

//Specialization
layout(constant_id = 0) const int sampleRange = 2;
layout(constant_id = 1) const float sampleScale = 1.0;

//Uniforms
layout(binding = 0) uniform sampler2D inputSampler;

//Input
layout(location = 0) in vec2 inUv;

//Output
layout(location = 0) out vec4 outColor;

void main()
{
    vec2 sampleSize = 1.0 / textureSize(inputSampler, 0) * sampleScale; //Size of single texel
    vec4 result = vec4(0.0);
    for (int x = -sampleRange; x < sampleRange; x++) 
	for (int y = -sampleRange; y < sampleRange; y++) 
    {
        result += texture(inputSampler, inUv + vec2(float(x), float(y)) * sampleSize);
    }
    int sampleCount = (sampleRange * 2) * (sampleRange * 2);
    outColor = result / sampleCount;
}