#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_fragutils.glsl"
#include "include_sceneinput.glsl"
#include "include_math.glsl"

//Specialization
layout(constant_id = 0) const bool isHorizontal = true;
layout(constant_id = 1) const float scale = 1.0;

//Uniforms
layout(binding = 0) uniform sampler2D inputSampler;

//Input
layout(location = 0) in vec2 inUv;

//Output
layout(location = 0) out vec4 outColor;

void main()
{
    //Gaussian blur kernel weights
    const float weights[] = float[]
    (
        0.227027,
        0.1945946,
        0.1216216,
        0.054054,
        0.016216
    );

    vec2 offset = 1.0 / textureSize(inputSampler, 0) * scale; //Size of single texel
    vec3 result = texture(inputSampler, inUv).rgb * weights[0]; //Current fragment's contribution
    for(int i = 1; i < 5; ++i)
    {
        if (isHorizontal)
        {
            result += texture(inputSampler, inUv + vec2(offset.x * i, 0.0)).rgb * weights[i];
            result += texture(inputSampler, inUv - vec2(offset.x * i, 0.0)).rgb * weights[i];
        }
        else //Vertical
        {
            result += texture(inputSampler, inUv + vec2(0.0, offset.y * i)).rgb * weights[i];
            result += texture(inputSampler, inUv - vec2(0.0, offset.y * i)).rgb * weights[i];
        }
    }
    outColor = vec4(result, 1.0);
}