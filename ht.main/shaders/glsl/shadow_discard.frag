#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_fragutils.glsl"
#include "include_sceneinput.glsl"

//Uniforms
layout(binding = 0) uniform CameraData cameraData;
layout(binding = 1) uniform SceneData sceneData;
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