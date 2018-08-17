#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_fragutils.glsl"

//Uniforms
layout(binding = 0) uniform CameraData cameraData;
layout(binding = 1) uniform SceneData sceneData;

//Input
layout(location = 0) in vec4 inColor;
layout(location = 1) in vec3 inWorldNormal;

//Output
layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outNormal;

void main() 
{
    outColor.rgb = inColor.rgb; //Color
    outColor.a = 0.0; //Specularity
    outNormal.xyz = inWorldNormal; //World normal
    outNormal.a = 1.0; //Emmisiveness
}