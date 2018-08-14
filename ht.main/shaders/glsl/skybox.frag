#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"

//Uniforms
layout(binding = 0) uniform CameraData cameraData;
layout(binding = 1) uniform SceneData sceneData;
layout(binding = 2) uniform samplerCube skyboxTexture;

//Input
layout(location = 0) in vec3 inSkyboxDirection;
layout(location = 1) in vec3 inWorldNormal;

//Output
layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outNormal;

void main() 
{
    outColor = texture(skyboxTexture, inSkyboxDirection);

    outNormal.xyz = normalize(inWorldNormal);
    outNormal.a = 1.0; //Store emissiveness. (1 because skybox is prelit)
}