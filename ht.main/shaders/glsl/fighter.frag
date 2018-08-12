#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_fragutils.glsl"

//Texture input
layout(binding = 1) uniform sampler2D colorTexSampler;
layout(binding = 2) uniform sampler2D normalTexSampler;

//Vert to frag input
layout(location = 0) in vec2 colorUv;
layout(location = 1) in float exhaustIntensity;
layout(location = 2) in float exhaustMask;
layout(location = 3) in vec3 worldPosition;
layout(location = 4) in vec3 worldNormal;

//Output
layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outNormal;

void main() 
{
    const vec4 hotExhaustColor = vec4(0.9, 0.9, 2.0, 1.0);
    const vec4 coldExhaustColor = vec4(0.2, 0.2, 2.0, 1.0);

    outColor = 
        texture(colorTexSampler, colorUv) + 
        mix(coldExhaustColor, hotExhaustColor, exhaustIntensity) * exhaustMask;

    outNormal.xyz = applyNormalTex(normalTexSampler, worldNormal, worldPosition, colorUv);
    outNormal.a = exhaustMask; //Store emissiveness
}