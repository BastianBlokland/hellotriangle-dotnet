#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_math.glsl"

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
layout(binding = 2) uniform samplerCube skyboxSampler;

//Output
out gl_PerVertex
{
    vec4 gl_Position;
};
layout(location = 0) out vec3 outSkyboxDirection;
layout(location = 1) out vec3 outWorldNormal;

void main()
{
    //Can be used to tweak the rotation of the skybox
    #define offsetAngle 2.1

    //Fullscreen triangle, more info: https://www.saschawillems.de/?page_id=2122
    gl_Position = vec4((gl_VertexIndex << 1 & 2) * 2.0 - 1, (gl_VertexIndex & 2) * 2.0 - 1, 1, 1);

    //Calculate where this vertex is in viewspace (so relative to the 'camera'). These form a giant
    //triangle on the far clip plane of the camera.
    //We use a inverse of the projection matrix to go from clip-space into view-space (unroll the 
    //projection)
    vec3 viewSpaceVertPos = (inverse(cameraData.projectionMatrix) * gl_Position).xyz;

    //Calculate where this vertex is in worldspace. 
    vec3 worldSpaceVertPos = mat3(cameraData.cameraMatrix) * viewSpaceVertPos;

    //Apply offset to be able to rotate the skybox
    outSkyboxDirection = yRotMatrix(offsetAngle) * worldSpaceVertPos;

    //Normal of the skybox is the inverse of the view direction (as the skybox 'looks' back at us)
    //Note: This is NOT normalized yet
    outWorldNormal = worldSpaceVertPos * -1;
}