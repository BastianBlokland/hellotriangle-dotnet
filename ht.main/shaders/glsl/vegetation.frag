#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_math.glsl"
#include "include_fragutils.glsl"

//Specialization
layout(constant_id = 0) const int swapchainCount = 1;
layout(constant_id = 1) const bool isShadowPass = false;

//Uniforms
layout(binding = 0) uniform SceneData sceneData;
layout(binding = 1) uniform CameraData cameraData;
layout(binding = 2) uniform sampler2D colorSampler;
layout(binding = 3) uniform sampler2D normalSampler;
layout(binding = 4) uniform sampler2D terrainSampler;

//Input
layout(location = 0) in vec2 inUv;
layout(location = 1) in vec3 inColorTint;
layout(location = 2) in vec3 inWorldPosition;
layout(location = 3) in vec3 inWorldNormal;

//Output
layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outNormal;
layout(location = 2) out vec4 outAttributes;

void main()
{
    vec4 colorSample = texture(colorSampler, inUv);
    if (colorSample.a < discardAlpha)
    {
        discard;
    }
    
    vec4 normalSample = texture(normalSampler, inUv);
    float specularIntensity = normalSample.a; //Store spec intensity in the normalmap alpha
    vec3 tangentNormal = normalSample.xyz * 2.0 - 1.0;

    outColor.rgb = colorSample.rgb * inColorTint;
    outColor.a = 0.0; //Bloom factor
    outNormal.xyz = perturbNormal(tangentNormal, inWorldNormal, inWorldPosition, inUv);
    outAttributes.x = specularIntensity; //Specularity
    outAttributes.y = 0.0; //Emmisiveness
    outAttributes.z = 1.0; //Shadow receive amount
    outAttributes.a = specularIntensity * 0.25; //Reflectivity
}