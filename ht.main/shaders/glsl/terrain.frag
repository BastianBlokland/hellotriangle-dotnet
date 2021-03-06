#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_fragutils.glsl"
#include "include_math.glsl"

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
layout(binding = 2) uniform sampler2D terrainHeightSampler;
layout(binding = 3) uniform sampler2D terrainColorSampler;
layout(binding = 4) uniform sampler2D detail1ColorSampler;
layout(binding = 5) uniform sampler2D detail1NormalSampler;
layout(binding = 6) uniform sampler2D detail2ColorSampler;
layout(binding = 7) uniform sampler2D detail2NormalSampler;

//Input
layout(location = 0) in vec2 inWorldUv;
layout(location = 1) in vec3 inWorldPosition;
layout(location = 2) in vec3 inWorldNormal;

//Output
layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outNormal;
layout(location = 2) out vec4 outAttributes;

void main() 
{
    #define detail1Uv inWorldUv * 10
    #define detail2Uv inWorldUv * 125

    vec4 colorAndSpec =
        texture(terrainColorSampler, inWorldUv) *
        texture(detail1ColorSampler, detail1Uv) *
        texture(detail2ColorSampler, detail2Uv);

    vec3 detail1TangentNormal = texture(detail1NormalSampler, detail1Uv).xyz * 2.0 - 1.0;
    vec3 detail2TangentNormal = texture(detail2NormalSampler, detail2Uv).xyz * 2.0 - 1.0;
    vec3 tangentNormal = blendNormals(detail1TangentNormal, detail2TangentNormal);
    vec3 worldNormal = perturbNormal(tangentNormal, inWorldNormal, inWorldPosition, inWorldUv);

    outColor.rgb = colorAndSpec.rgb;
    outColor.a = 0.0; //Bloom factor
    outNormal.xyz = worldNormal;
    outAttributes.x = colorAndSpec.a; //Specularity
    outAttributes.y = 0.0; //Emissiveness
    outAttributes.z = 1.0; //Shadow receive amount
    outAttributes.a = 0.0; //Reflectivity
}