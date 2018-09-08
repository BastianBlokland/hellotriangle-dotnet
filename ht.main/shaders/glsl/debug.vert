#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_vertexinput.glsl"
#include "include_instanceinput.glsl"

//Specialization
layout(constant_id = 0) const int swapchainCount = 3;
layout(constant_id = 1) const bool isShadowPass = false;

//PushData
layout(push_constant) uniform PushData
{
    int swapchainIndex;
} pushdata;

//Uniforms
layout(binding = 0) uniform SceneDataBlock { SceneData sceneData[swapchainCount]; };
layout(binding = 1) uniform CameraDataBlock { CameraData cameraData[swapchainCount]; };

//Output
out gl_PerVertex
{
    vec4 gl_Position;
};
layout(location = 0) out vec4 outColor;
layout(location = 1) out vec3 outWorldNormal;

void main()
{
    outColor = vertColor;
    outWorldNormal = mat3(instanceModelMatrix) * vertNormal;
    gl_Position = 
        cameraData[pushdata.swapchainIndex].viewProjectionMatrix * 
        instanceModelMatrix *
        vec4(vertPosition, 1.0);
}