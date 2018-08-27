#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_fragutils.glsl"
#include "include_math.glsl"

//Uniforms
layout(binding = 0) uniform SceneData sceneData;
layout(binding = 1) uniform CameraData cameraData;
layout(binding = 2) uniform sampler2D terrainSampler;
layout(binding = 3) uniform sampler2D detail1ColorSampler;
layout(binding = 4) uniform sampler2D detail1NormalSampler;
layout(binding = 5) uniform sampler2D detail2ColorSampler;
layout(binding = 6) uniform sampler2D detail2NormalSampler;

//Input
layout(location = 0) in vec4 inBaseColor;
layout(location = 1) in vec2 inWorldUv;
layout(location = 2) in vec3 inWorldPosition;
layout(location = 3) in vec3 inWorldNormal;

//Output
layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outNormal;
layout(location = 2) out vec4 outAttributes;

void main() 
{
    #define detail1Uv inWorldUv * 5
    #define detail2Uv inWorldUv * 100

    vec4 colorAndSpec = vec4(inBaseColor.rgb, 1.0) * 
        texture(detail1ColorSampler, detail1Uv) *
        texture(detail2ColorSampler, detail2Uv);
    vec3 detail1Normal = applyNormalTex(detail1NormalSampler, inWorldNormal, inWorldPosition, detail1Uv);
    vec3 detail2Normal = applyNormalTex(detail2NormalSampler, inWorldNormal, inWorldPosition, detail2Uv);

    outColor.rgb = colorAndSpec.rgb;
    outColor.a = 0.0; //Bloom factor
    outNormal.xyz = blendNormals(detail1Normal, detail2Normal);
    outAttributes.x = colorAndSpec.a; //Specularity
    outAttributes.y = 0.0; //Emissiveness
    outAttributes.z = 1.0; //Shadow receive amount
    outAttributes.a = 0.0; //Reflectivity
}