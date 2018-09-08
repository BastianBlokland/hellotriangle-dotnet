#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_fragutils.glsl"

//Specialization
layout(constant_id = 0) const int swapchainCount = 1;
layout(constant_id = 1) const bool isShadowPass = false;

//PushData
layout(push_constant) uniform PushData
{
    int swapchainIndex;
} pushdata;

//Uniforms
layout(binding = 0) uniform SceneData sceneData;
layout(binding = 1) uniform CameraData cameraData;

//Input
layout(location = 0) in vec4 inColor;
layout(location = 1) in vec3 inWorldNormal;

//Output
layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outNormal;
layout(location = 2) out vec4 outAttributes;

void main() 
{
    outColor.rgb = inColor.rgb; //Color
    outColor.a = 0.0; //Bloom factor
    outNormal.xyz = inWorldNormal; //Normal
    outAttributes.x = 0.0; //Specularity
    outAttributes.y = 1.0; //Emisiveness
    outAttributes.z = 0.0; //Shadow receive amount
    outAttributes.a = 0.0; //Reflectivity
}