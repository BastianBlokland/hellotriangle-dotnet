#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_math.glsl"
#include "include_sceneinput.glsl"

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
layout(binding = 2) uniform samplerCube skyboxTexture;

//Input
layout(location = 0) in vec3 inSkyboxDirection;
layout(location = 1) in vec3 inWorldNormal;

//Output
layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outNormal;
layout(location = 2) out vec4 outAttributes;

void main() 
{
    outColor.rgb = texture(skyboxTexture, inSkyboxDirection).rgb;
    outColor.a = luma(outColor.rgb) * 0.1; //Bloom factor
    outNormal.xyz = normalize(inWorldNormal);
    outAttributes.x = 0.0; //Specularity
    outAttributes.y = 1.0; //Emissiveness (1 because skybox is prelit)
    outAttributes.z = 0.0; //Shadow receive amount
    outAttributes.a = 0.0; //Reflectivity
}