#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_fragutils.glsl"

//Uniforms
layout(binding = 0) uniform CameraData cameraData;
layout(binding = 1) uniform SceneData sceneData;
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
layout(location = 2) out vec4 outAttributes;

void main() 
{
    #define hotExhaustColor vec3(0.9, 0.9, 2.0)
    #define coldExhaustColor vec3(0.2, 0.2, 2.0)

    vec4 colorAndSpec = texture(colorSampler, inUv);
    vec3 exhaustColor = mix(coldExhaustColor, hotExhaustColor, inExhaustIntensity) * inExhaustMask;

    outColor.rgb = colorAndSpec.rgb + exhaustColor;
    outNormal.xyz = applyNormalTex(normalSampler, inWorldNormal, inWorldPosition, inUv);
    outAttributes.x = colorAndSpec.a; //Specular
    outAttributes.y = inExhaustMask; //Emissiveness
    outAttributes.z = 0.0; //Shadow receive amount
    outAttributes.a = clamp(colorAndSpec.a * 2.0, 0.0, 1.0);
}